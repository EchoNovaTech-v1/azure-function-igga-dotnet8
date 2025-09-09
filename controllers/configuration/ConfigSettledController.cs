using System.Net;
using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using Extensions.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Data.Tables;

namespace AppFunctions.controllers.configuration
{
    /// <summary>
    /// CRUD de ConfigSettled (tabla de configuraciones).
    /// </summary>
    public class ConfigSettledController
    {
        private const string _partitionKey = "ConfigSettled";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        [Function("getConfigSettled")]
        public static async Task<HttpResponseData> GetConfigSettled(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "configSettleds/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigSettledController");
            logger.LogInformation("GET -> getting ConfigSettled {RowKey}", rowKey);

            try
            {
                // Puedes usar GetEntityIfExistsAsync si no usas las extensiones:
                // var (ok, entity) = await _table.GetEntityIfExistsAsync<ConfigSettledEntity>(_partitionKey, rowKey);
                var entity = await _table.QueryFirstOrDefaultAsync<ConfigSettledEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                var response = req.CreateResponse(entity is null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
                if (entity is null)
                    await response.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                else
                    await response.WriteAsJsonAsync(entity);

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error to get ConfigSettled {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving ConfigSettled {rowKey}: {ex}");
                return response;
            }
        }

        [Function("getConfigSettleds")]
        public static async Task<HttpResponseData> GetConfigSettleds(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "configSettleds")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigSettledController");
            logger.LogInformation("GET ALL -> getting all ConfigSettleds");

            try
            {
                var list = await _table.QueryToListAsync<ConfigSettledEntity>(x => x.PartitionKey == _partitionKey);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(list);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error getting ConfigSettleds");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving ConfigSettleds: {ex}");
                return response;
            }
        }

        [Function("postConfigSettled")]
        public static async Task<HttpResponseData> PostConfigSettled(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "configSettleds")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigSettledController");
            logger.LogInformation("POST -> inserting ConfigSettled");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<AppFunctions.models.ConfigSettled>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var rowKey = Guid.NewGuid().ToString();

                var entity = new ConfigSettledEntity(_partitionKey, rowKey)
                {
                    active = true,
                    dateRestart = record.dateRestart,
                    maxSettled = record.maxSettled,
                    idWorkflow = record.idWorkflow,
                    formatDate = string.IsNullOrWhiteSpace(record.formatDate) ? "YYYYMMDD" : record.formatDate,
                    initialSettled = record.initialSettled ?? 0,
                    autoIncrement = record.autoIncrement ?? 1
                };

                await _table.AddEntityAsync(entity);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting ConfigSettled");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error inserting ConfigSettled: {ex}");
                return response;
            }
        }

        [Function("putConfigSettled")]
        public static async Task<HttpResponseData> PutConfigSettled(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "configSettleds/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigSettledController");
            logger.LogInformation("PUT -> updating ConfigSettled {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<ConfigSettledEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<AppFunctions.models.ConfigSettled>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                // Mapear campos (manteniendo defaults si vinieran null)
                entity.maxSettled = updated.maxSettled;
                entity.initialSettled = updated.initialSettled ?? entity.initialSettled;
                entity.autoIncrement = updated.autoIncrement ?? entity.autoIncrement;
                entity.dateRestart = updated.dateRestart;
                entity.active = updated.active;
                entity.idWorkflow = updated.idWorkflow;
                entity.formatDate = string.IsNullOrWhiteSpace(updated?.formatDate) ? (entity.formatDate ?? "YYYYMMDD") : updated!.formatDate;

                // Merge conserva propiedades no enviadas
                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating ConfigSettled {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error updating ConfigSettled {rowKey}: {ex}");
                return response;
            }
        }

        [Function("deleteConfigSettled")]
        public static async Task<HttpResponseData> DeleteConfigSettled(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "configSettleds/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigSettledController");
            logger.LogInformation("DELETE -> removing ConfigSettled {RowKey}", rowKey);

            try
            {
                // Si quieres verificar existencia primero:
                var entity = await _table.QueryFirstOrDefaultAsync<ConfigSettledEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                await _table.DeleteEntityAsync(_partitionKey, rowKey, entity.ETag);

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DELETE -> error removing ConfigSettled {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error removing ConfigSettled {rowKey}: {ex}");
                return response;
            }
        }
    }
}
