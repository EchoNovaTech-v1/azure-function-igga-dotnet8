using System.Net;
using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using AppFunctions.models;
using Azure;
using Azure.Data.Tables;
using Extensions.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AppFunctions.controllers.security
{
    public class StorageSystemController
    {
        // Partición de trabajo
        private const string _partitionKey = "Storage";

        // Cliente de tabla (helper propio)
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableSecurity);

        static StorageSystemController()
        {
            try { _table.CreateIfNotExists(); } catch { /* no-op */ }
        }

        // GET /storages/{rowKey}
        [Function("getStorageSystem")]
        public static async Task<HttpResponseData> GetStorageSystem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "storages/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("StorageSystemController");
            log.LogInformation("GET -> getting the storage system with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<StorageSystemEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET -> error to get the storage system {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the record the storage system {rowKey}: {ex}");
                return bad;
            }
        }

        // GET /storages
        [Function("getStorageSystems")]
        public static async Task<HttpResponseData> GetStorageSystems(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "storages")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("StorageSystemController");
            log.LogInformation("GET ALL -> getting all the storage systems.");

            try
            {
                var list = await _table.QueryToListAsync<StorageSystemEntity>(x => x.PartitionKey == _partitionKey);
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET ALL -> error during the process of getting storage systems");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the records from the storage system list: {ex}");
                return bad;
            }
        }

        // POST /storages
        [Function("postStorageSystem")]
        public static async Task<HttpResponseData> PostStorageSystem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "storages")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("StorageSystemController");
            log.LogInformation("POST -> inserting storage system.");

            var rowKey = Guid.NewGuid().ToString();

            try
            {
                var body = await req.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                StorageSystem? record;
                try { record = JsonConvert.DeserializeObject<StorageSystem>(body); }
                catch (JsonException)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid JSON payload.");
                    return bad;
                }

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var entity = new StorageSystemEntity(_partitionKey, rowKey)
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
                await created.WriteAsJsonAsync(entity);
                return created;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "POST -> error during the process of inserting the storage system");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while inserting the record of the storage systems list: {ex}");
                return bad;
            }
        }

        // PUT /storages/{rowKey}
        [Function("putStorageSystem")]
        public static async Task<HttpResponseData> PutStorageSystem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "storages/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("StorageSystemController");
            log.LogInformation("PUT -> updating storage system with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<StorageSystemEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                StorageSystem? updated;
                try { updated = JsonConvert.DeserializeObject<StorageSystem>(body); }
                catch (JsonException)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid JSON payload.");
                    return bad;
                }

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
                await ok.WriteAsJsonAsync(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "PUT -> error during the process of updating the storage system {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while updating the storage system {rowKey}: {ex}");
                return bad;
            }
        }

        // DELETE /storages/{rowKey}
        [Function("deleteStorageSystem")]
        public static async Task<HttpResponseData> DeleteStorageSystem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "storages/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("StorageSystemController");
            log.LogInformation("DELETE -> removing the storage system with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<StorageSystemEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                await _table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "DELETE -> error during the process of removing the storage system {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while removing the record from the storage systems list: {ex}");
                return bad;
            }
        }
    }
}
