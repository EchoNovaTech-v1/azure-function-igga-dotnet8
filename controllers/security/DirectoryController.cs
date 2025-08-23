using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using Azure.Data.Tables;
using Extensions.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using Directory = AppFunctions.models.Directory;

namespace AppFunctions.controllers.security
{
    public class DirectoryController
    {
        // Partición de trabajo
        private const string _partitionKey = "Directory";

        // Cliente de tabla (usa tu helper)
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableSecurity);

        // GET /directories/{rowKey}
        [Function("getDirectory")]
        public static async Task<HttpResponseData> GetDirectory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "directories/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var log = context.GetLogger("DirectoryController");
            log.LogInformation("GET -> getting the directory with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<DirectoryEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<DirectoryEntity>(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET -> error to get the directory {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the record the directory {rowKey}: {ex}");
                return bad;
            }
        }

        // GET /directories
        [Function("getDirectories")]
        public static async Task<HttpResponseData> GetDirectories(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "directories")]
            HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger("DirectoryController");
            log.LogInformation("GET ALL -> getting all directories.");

            try
            {
                var list = await _table.QueryToListAsync<DirectoryEntity>(x => x.PartitionKey == _partitionKey);
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<List<DirectoryEntity>>(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET ALL -> error during the process of getting directories");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the records from the directories list: {ex}");
                return bad;
            }
        }

        // POST /directories
        [Function("postDirectory")]
        public static async Task<HttpResponseData> PostDirectory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "directories")]
            HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger("DirectoryController");
            log.LogInformation("POST -> inserting directory.");

            var rowKey = Guid.NewGuid().ToString();

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Directory>(body);

                if (record is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                var entity = new DirectoryEntity(_partitionKey, rowKey)
                {
                    Provider = record.provider,
                    IdApplication = record.idApplication,
                    IdObject = record.idObject,
                    Password = record.password,
                    ApiEndpoint = record.apiEndpoint,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var created = req.CreateResponse(HttpStatusCode.Created);
                await created.WriteAsJsonAsync<DirectoryEntity>(entity);
                return created;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "POST -> error during the process of inserting the directory");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while inserting the record of the directory list: {ex}");
                return bad;
            }
        }

        // PUT /directories/{rowKey}
        [Function("putDirectory")]
        public static async Task<HttpResponseData> PutDirectory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "directories/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var log = context.GetLogger("DirectoryController");
            log.LogInformation("PUT -> updating directory with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<DirectoryEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<Directory>(body);

                if (updated is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                entity.Provider = updated.provider;
                entity.IdApplication = updated.idApplication;
                entity.IdObject = updated.idObject;
                entity.Password = updated.password;
                entity.ApiEndpoint = updated.apiEndpoint;
                entity.Active = updated.active;

                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<DirectoryEntity>(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "PUT -> error during the process of updating the directory {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while updating the directory {rowKey}: {ex}");
                return bad;
            }
        }

        // DELETE /directories/{rowKey}
        [Function("deleteDirectory")]
        public static async Task<HttpResponseData> DeleteDirectory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "directories/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var log = context.GetLogger("DirectoryController");
            log.LogInformation("DELETE -> removing the directory with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<DirectoryEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                await _table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "DELETE -> error during the process of removing the directory {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while removing the record from the directories list: {ex}");
                return bad;
            }
        }
    }
}
