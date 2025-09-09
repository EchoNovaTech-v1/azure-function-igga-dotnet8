using System.Net;
using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using AppFunctions.models;
using Azure.Data.Tables;
using Extensions.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AppFunctions.controllers.configuration
{
    public class StorageTypeController
    {
        private const string _partitionKey = "StorageType";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        [Function("getStorageType")]
        public static async Task<HttpResponseData> GetStorageType(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "storageTypes/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("StorageTypeController");
            logger.LogInformation("GET -> getting StorageType {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<StorageTypeEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                var resp = req.CreateResponse(entity is null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
                if (entity is null)
                    await resp.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                else
                    await resp.WriteAsJsonAsync<StorageTypeEntity>(entity);

                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting StorageType {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving storage type {rowKey}: {ex}");
                return resp;
            }
        }

        [Function("getStorageTypes")]
        public static async Task<HttpResponseData> GetStorageTypes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "storageTypes")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("StorageTypeController");
            logger.LogInformation("GET ALL -> getting all StorageTypes");

            try
            {
                var list = await _table.QueryToListAsync<StorageTypeEntity>(x => x.PartitionKey == _partitionKey);
                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<IList<StorageTypeEntity>>(list);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error getting StorageTypes");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving storage types: {ex}");
                return resp;
            }
        }

        [Function("postStorageTypes")]
        public static async Task<HttpResponseData> PostStorageTypes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "storageTypes")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("StorageTypeController");
            logger.LogInformation("POST -> inserting StorageType");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<StorageType>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var rowKey = Guid.NewGuid().ToString();
                var entity = new StorageTypeEntity(_partitionKey, rowKey)
                {
                    Name = record.name,
                    Description = record.description,
                    Keycode = record.keycode,
                    StorageSize = record.storageSize,
                    IdStorageUnity = record.idStorageUnity,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var resp = req.CreateResponse(HttpStatusCode.Created);
                await resp.WriteAsJsonAsync<StorageTypeEntity>(entity);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting StorageType");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error inserting storage type: {ex}");
                return resp;
            }
        }

        [Function("putStorageType")]
        public static async Task<HttpResponseData> PutStorageType(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "storageTypes/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("StorageTypeController");
            logger.LogInformation("PUT -> updating StorageType {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<StorageTypeEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return nf;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<StorageType>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                entity.Name = updated.name;
                entity.Description = updated.description;
                entity.Keycode = updated.keycode;
                entity.StorageSize = updated.storageSize;
                entity.IdStorageUnity = updated.idStorageUnity;
                entity.Active = updated.active;

                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<StorageTypeEntity>(entity);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating StorageType {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error updating storage type {rowKey}: {ex}");
                return resp;
            }
        }

        [Function("deleteStorageType")]
        public static async Task<HttpResponseData> DeleteStorageType(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "storageTypes/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("StorageTypeController");
            logger.LogInformation("DELETE -> removing StorageType {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<StorageTypeEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return nf;
                }

                await _table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);
                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DELETE -> error removing StorageType {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error removing storage type {rowKey}: {ex}");
                return resp;
            }
        }
    }
}
