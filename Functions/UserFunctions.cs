using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzureFunctionIgga.Services.Interfaces;
using AzureFunctionIgga.Models.DTOs;
using System.Net;
using System.Text.Json;

namespace AzureFunctionIgga.Functions;

public class UserFunctions
{
    private readonly IUserService _userService;
    private readonly ILogger<UserFunctions> _logger;

    public UserFunctions(IUserService userService, ILogger<UserFunctions> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [Function("GetUser")]
    public async Task<HttpResponseData> GetUser(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{id:int}")] HttpRequestData req,
        int id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Obteniendo usuario con ID: {UserId}", id);

        try
        {
            var result = await _userService.GetUserByIdAsync(id, cancellationToken);
            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound);
            
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario con ID: {UserId}", id);
            
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

    [Function("GetUsers")]
    public async Task<HttpResponseData> GetUsers(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Obteniendo lista de usuarios");

        try
        {
            var query = req.Query;
            var paginationQuery = new PaginationQuery
            {
                PageNumber = int.TryParse(query["pageNumber"], out var pageNumber) ? pageNumber : 1,
                PageSize = int.TryParse(query["pageSize"], out var pageSize) ? Math.Min(pageSize, 100) : 10,
                SearchTerm = query["searchTerm"],
                SortBy = query["sortBy"],
                SortDescending = bool.TryParse(query["sortDescending"], out var sortDesc) && sortDesc
            };

            var result = await _userService.GetUsersAsync(paginationQuery, cancellationToken);
            var response = req.CreateResponse(HttpStatusCode.OK);
            
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios");
            
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

    [Function("CreateUser")]
    public async Task<HttpResponseData> CreateUser(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "users")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creando nuevo usuario");

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

            var createUserDto = JsonSerializer.Deserialize<CreateUserDto>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (createUserDto == null)
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

            var result = await _userService.CreateUserAsync(createUserDto, cancellationToken);
            var response = req.CreateResponse(result.Success ? HttpStatusCode.Created : HttpStatusCode.BadRequest);
            
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear usuario");
            
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

    [Function("UpdateUser")]
    public async Task<HttpResponseData> UpdateUser(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "users/{id:int}")] HttpRequestData req,
        int id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Actualizando usuario con ID: {UserId}", id);

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

            var updateUserDto = JsonSerializer.Deserialize<UpdateUserDto>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (updateUserDto == null)
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

            var result = await _userService.UpdateUserAsync(id, updateUserDto, cancellationToken);
            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound);
            
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar usuario con ID: {UserId}", id);
            
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

    [Function("DeleteUser")]
    public async Task<HttpResponseData> DeleteUser(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "users/{id:int}")] HttpRequestData req,
        int id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Eliminando usuario con ID: {UserId}", id);

        try
        {
            var result = await _userService.DeleteUserAsync(id, cancellationToken);
            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound);
            
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar usuario con ID: {UserId}", id);
            
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

    [Function("ActivateUser")]
    public async Task<HttpResponseData> ActivateUser(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "users/{id:int}/activate")] HttpRequestData req,
        int id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activando usuario con ID: {UserId}", id);

        try
        {
            var result = await _userService.ActivateUserAsync(id, cancellationToken);
            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound);
            
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al activar usuario con ID: {UserId}", id);
            
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

    [Function("DeactivateUser")]
    public async Task<HttpResponseData> DeactivateUser(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "users/{id:int}/deactivate")] HttpRequestData req,
        int id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Desactivando usuario con ID: {UserId}", id);

        try
        {
            var result = await _userService.DeactivateUserAsync(id, cancellationToken);
            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound);
            
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar usuario con ID: {UserId}", id);
            
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
}