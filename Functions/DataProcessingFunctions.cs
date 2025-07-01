using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzureFunctionIgga.Services.Interfaces;
using AzureFunctionIgga.Models.DTOs;
using System.Net;
using System.Text.Json;

namespace AzureFunctionIgga.Functions;

public class DataProcessingFunctions
{
    private readonly IDataProcessingService _dataProcessingService;
    private readonly IProcessingJobService _processingJobService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DataProcessingFunctions> _logger;

    public DataProcessingFunctions(
        IDataProcessingService dataProcessingService,
        IProcessingJobService processingJobService,
        INotificationService notificationService,
        ILogger<DataProcessingFunctions> logger)
    {
        _dataProcessingService = dataProcessingService;
        _processingJobService = processingJobService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [Function("ProcessDataHttpTrigger")]
    public async Task<HttpResponseData> ProcessDataHttpTrigger(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "data/process")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando procesamiento de datos vía HTTP");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badRequestResponse.Headers.Add("Content-Type", "application/json");
                await badRequestResponse.WriteStringAsync(JsonSerializer.Serialize(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Datos de entrada requeridos"
                }));
                return badRequestResponse;
            }

            var processRequest = JsonSerializer.Deserialize<ProcessDataRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (processRequest == null)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badRequestResponse.Headers.Add("Content-Type", "application/json");
                await badRequestResponse.WriteStringAsync(JsonSerializer.Serialize(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Formato de datos inválido"
                }));
                return badRequestResponse;
            }

            var result = await _dataProcessingService.ProcessDataAsync(
                processRequest.DataRecordId, 
                processRequest.Parameters ?? new Dictionary<string, object>(), 
                cancellationToken);

            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en procesamiento de datos HTTP");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor"
            }));
            
            return errorResponse;
        }
    }

    [Function("ProcessDataQueueTrigger")]
    public async Task ProcessDataQueueTrigger(
        [QueueTrigger("data-processing", Connection = "AzureWebJobsStorage")] string queueItem,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Procesando datos desde queue: {QueueItem}", queueItem);

        try
        {
            var processRequest = JsonSerializer.Deserialize<ProcessDataRequest>(queueItem, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (processRequest == null)
            {
                _logger.LogError("No se pudo deserializar el mensaje de la queue");
                return;
            }

            // Crear job de procesamiento
            var createJobDto = new CreateProcessingJobDto
            {
                JobName = $"ProcessData_{processRequest.DataRecordId}",
                UserId = processRequest.UserId,
                Parameters = processRequest.Parameters
            };

            var jobResult = await _processingJobService.CreateJobAsync(createJobDto, cancellationToken);
            
            if (!jobResult.Success || jobResult.Data == null)
            {
                _logger.LogError("Error al crear job de procesamiento");
                return;
            }

            var jobId = jobResult.Data.Id;

            try
            {
                // Actualizar estado del job a "Processing"
                await _processingJobService.UpdateJobStatusAsync(jobId, "Processing", 0, null, cancellationToken);

                // Procesar datos
                var result = await _dataProcessingService.ProcessDataAsync(
                    processRequest.DataRecordId,
                    processRequest.Parameters ?? new Dictionary<string, object>(),
                    cancellationToken);

                if (result.Success && result.Data != null)
                {
                    // Completar job exitosamente
                    var resultDict = new Dictionary<string, object>
                    {
                        ["processedData"] = result.Data,
                        ["processedAt"] = DateTime.UtcNow
                    };

                    await _processingJobService.CompleteJobAsync(jobId, resultDict, cancellationToken);

                    // Enviar notificación de éxito
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        Title = "Procesamiento Completado",
                        Message = $"Los datos del registro {processRequest.DataRecordId} han sido procesados exitosamente.",
                        Type = "Success",
                        UserId = processRequest.UserId
                    }, cancellationToken);

                    _logger.LogInformation("Procesamiento completado exitosamente para DataRecord ID: {DataRecordId}", processRequest.DataRecordId);
                }
                else
                {
                    // Marcar job como fallido
                    await _processingJobService.UpdateJobStatusAsync(jobId, "Failed", 100, result.Message, cancellationToken);

                    // Enviar notificación de error
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        Title = "Error en Procesamiento",
                        Message = $"Error al procesar datos del registro {processRequest.DataRecordId}: {result.Message}",
                        Type = "Error",
                        UserId = processRequest.UserId
                    }, cancellationToken);

                    _logger.LogError("Error en procesamiento para DataRecord ID: {DataRecordId}. Error: {Error}", 
                        processRequest.DataRecordId, result.Message);
                }
            }
            catch (Exception ex)
            {
                // Marcar job como fallido
                await _processingJobService.UpdateJobStatusAsync(jobId, "Failed", 100, ex.Message, cancellationToken);

                // Enviar notificación de error
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    Title = "Error en Procesamiento",
                    Message = $"Error técnico al procesar datos del registro {processRequest.DataRecordId}",
                    Type = "Error",
                    UserId = processRequest.UserId
                }, cancellationToken);

                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en procesamiento de datos desde queue");
            throw;
        }
    }

    [Function("ProcessPendingJobsTimer")]
    public async Task ProcessPendingJobsTimer(
        [TimerTrigger("0 */5 * * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ejecutando procesamiento de jobs pendientes. Próxima ejecución: {NextRun}", timerInfo.ScheduleStatus?.Next);

        try
        {
            await _processingJobService.ProcessPendingJobsAsync(cancellationToken);
            _logger.LogInformation("Procesamiento de jobs pendientes completado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en procesamiento automático de jobs pendientes");
            throw;
        }
    }

    [Function("GenerateReport")]
    public async Task<HttpResponseData> GenerateReport(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "reports/generate")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generando reporte");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badRequestResponse.Headers.Add("Content-Type", "application/json");
                await badRequestResponse.WriteStringAsync(JsonSerializer.Serialize(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Datos de entrada requeridos"
                }));
                return badRequestResponse;
            }

            var reportRequest = JsonSerializer.Deserialize<GenerateReportRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (reportRequest == null)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badRequestResponse.Headers.Add("Content-Type", "application/json");
                await badRequestResponse.WriteStringAsync(JsonSerializer.Serialize(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Formato de datos inválido"
                }));
                return badRequestResponse;
            }

            var result = await _dataProcessingService.GenerateReportAsync(
                reportRequest.ReportType,
                reportRequest.Parameters ?? new Dictionary<string, object>(),
                cancellationToken);

            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar reporte");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor"
            }));
            
            return errorResponse;
        }
    }

    [Function("HealthCheck")]
    public async Task<HttpResponseData> HealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Verificando estado de salud del sistema");

        try
        {
            var healthStatus = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Unknown",
                Services = new
                {
                    Database = "Connected",
                    Storage = "Connected",
                    Processing = "Active"
                }
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(healthStatus));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en health check");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            }));
            
            return errorResponse;
        }
    }
}

// DTOs para las requests
public record ProcessDataRequest
{
    public int DataRecordId { get; init; }
    public int UserId { get; init; }
    public Dictionary<string, object>? Parameters { get; init; }
}

public record GenerateReportRequest
{
    public string ReportType { get; init; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; init; }
}