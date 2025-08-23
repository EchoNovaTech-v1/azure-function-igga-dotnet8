using System.Net;
using Azure;
using Azure.Data.Tables;
using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using AppFunctions.models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AppFunctions.controllers.configuration
{
    /// <summary>
    /// CRUD de ConfigurationApp (partición ListConfigurations) en .NET Isolated + Azure.Data.Tables.
    /// </summary>
    public class ConfigurationAppController
    {
        private const string _partitionKey = "ListConfigurations";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        // GET /api/configurationsHome/{rowKey}
        [Function("getConfigurationApp")]
        public static async Task<HttpResponseData> GetConfigurationApp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "configurationsHome/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigurationAppController");
            logger.LogInformation("GET -> getting configuration {RowKey}", rowKey);

            try
            {
                var res = await _table.GetEntityIfExistsAsync<ConfigurationAppEntity>(_partitionKey, rowKey);
                var response = req.CreateResponse(res.HasValue ? HttpStatusCode.OK : HttpStatusCode.NotFound);

                if (!res.HasValue)
                {
                    await response.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                    return response;
                }

                await response.WriteAsJsonAsync(res.Value);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting configuration {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving configuration {rowKey}: {ex}");
                return response;
            }
        }

        // GET /api/configurationsHome
        [Function("getConfigurationApps")]
        public static async Task<HttpResponseData> GetConfigurationApps(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "configurationsHome")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigurationAppController");
            logger.LogInformation("GET ALL -> getting all configurations");

            try
            {
                var list = new List<ConfigurationAppEntity>();
                await foreach (var item in _table.QueryAsync<ConfigurationAppEntity>(e => e.PartitionKey == _partitionKey))
                {
                    list.Add(item);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(list);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error getting configurations");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving configurations: {ex}");
                return response;
            }
        }

        // POST /api/configurationsHome
        [Function("postConfigurationApp")]
        public static async Task<HttpResponseData> PostConfigurationApp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "configurationsHome")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigurationAppController");
            logger.LogInformation("POST -> inserting configuration");

            try
            {
                // En esta partición normalmente debería existir SOLO UN registro
                if (await PartitionHasAnyAsync())
                {
                    var conflict = req.CreateResponse(HttpStatusCode.Conflict);
                    await conflict.WriteAsJsonAsync(new { message = "A configuration record already exists (single-row constraint)." });
                    return conflict;
                }

                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<ConfigurationApp>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var rowKey = Guid.NewGuid().ToString();

                var entity = new ConfigurationAppEntity(_partitionKey, rowKey)
                {
                    Name = record.name,
                    Language = record.language,
                    IdDirectory = record.idDirectory,
                    ImageHome = record.imageHome,
                    UrlFacebook = record.urlFacebook,
                    UrlTwitter = record.urlTwitter,
                    UrilOfficialPage = record.urilOfficialPage,
                    Phone = record.phone
                };

                await _table.AddEntityAsync(entity);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting configuration");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error inserting configuration: {ex}");
                return response;
            }
        }

        // PUT /api/configurationsHome/{rowKey}
        [Function("putConfigurationApp")]
        public static async Task<HttpResponseData> PutConfigurationApp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "configurationsHome/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigurationAppController");
            logger.LogInformation("PUT -> updating configuration {RowKey}", rowKey);

            try
            {
                var current = await _table.GetEntityIfExistsAsync<ConfigurationAppEntity>(_partitionKey, rowKey);
                if (!current.HasValue)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                var entity = current.Value;

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<ConfigurationApp>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                // Mapear campos
                entity.Name = updated.name;
                entity.Language = updated.language;
                entity.IdDirectory = updated.idDirectory;
                entity.ImageHome = updated.imageHome;
                entity.UrlFacebook = updated.urlFacebook;
                entity.UrlTwitter = updated.urlTwitter;
                entity.UrilOfficialPage = updated.urilOfficialPage;
                entity.Phone = updated.phone;

                try
                {
                    await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);
                }
                catch (RequestFailedException rfe) when (rfe.Status == 412)
                {
                    var pre = req.CreateResponse(HttpStatusCode.PreconditionFailed);
                    await pre.WriteStringAsync("Concurrency conflict. Reload and retry.");
                    return pre;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating configuration {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error updating configuration {rowKey}: {ex}");
                return response;
            }
        }

        // DELETE /api/configurationsHome/{rowKey}
        [Function("deleteConfigurationApp")]
        public static async Task<HttpResponseData> DeleteConfigurationApp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "configurationsHome/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigurationAppController");
            logger.LogInformation("DELETE -> removing configuration {RowKey}", rowKey);

            try
            {
                var res = await _table.GetEntityIfExistsAsync<ConfigurationAppEntity>(_partitionKey, rowKey);
                if (!res.HasValue)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                await _table.DeleteEntityAsync(_partitionKey, rowKey, res.Value.ETag);
                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DELETE -> error removing configuration {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error removing configuration {rowKey}: {ex}");
                return response;
            }
        }

        /// <summary>
        /// Verifica si ya existe al menos un registro en la partición (single-row constraint).
        /// </summary>
        private static async Task<bool> PartitionHasAnyAsync()
        {
            await foreach (var _ in _table.QueryAsync<ConfigurationAppEntity>(e => e.PartitionKey == _partitionKey, maxPerPage: 1))
            {
                return true;
            }
            return false;
        }
    }
}
