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

namespace AppFunctions.controllers.configuration
{
    public class StorageUnitController
    {
        // Partición de trabajo
        private const string _partitionKey = "StorageUnit";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        // GET -> startsWith(RowKey) (mismo comportamiento que antes)
        [Function("getStorageUnit")]
        public static async Task<HttpResponseData> GetStorageUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "storageUnits/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("StorageUnitController");
            logger.LogInformation("GET -> getting storage units by RowKey prefix {RowKey}", rowKey);

            try
            {
                if (string.IsNullOrEmpty(rowKey))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("RowKey prefix is required.");
                    return bad;
                }

                // startsWith con filtros OData: RowKey ge '{prefix}' and RowKey lt '{nextPrefix}'
                var last = rowKey[^1];
                var nextLast = (char)((int)last + 1);
                var nextPrefix = rowKey[..^1] + nextLast;

                var filter =
                    $"PartitionKey eq '{_partitionKey}' and RowKey ge '{rowKey}' and RowKey lt '{nextPrefix}'";

                var list = await _table.QueryToListAsync<StorageUnitEntity>(filter);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<IList<StorageUnitEntity>>(list);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting storage units by prefix {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving storage units: {ex}");
                return resp;
            }
        }

        // GET ALL
        [Function("getStorageUnits")]
        public static async Task<HttpResponseData> GetStorageUnits(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "storageUnits")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("StorageUnitController");
            logger.LogInformation("GET ALL -> getting all storage units");

            try
            {
                var list = await _table.QueryToListAsync<StorageUnitEntity>(x => x.PartitionKey == _partitionKey);
                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<IList<StorageUnitEntity>>(list);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error getting storage units");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving storage units: {ex}");
                return resp;
            }
        }

        // POST
        [Function("postStorageUnit")]
        public static async Task<HttpResponseData> PostStorageUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "storageUnits")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("StorageUnitController");
            logger.LogInformation("POST -> inserting storage unit");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<StorageUnit>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var idStorageUnit = Guid.NewGuid().ToString();
                var rowKey = $"{record.idParent}|{idStorageUnit}";

                var entity = new StorageUnitEntity(_partitionKey, rowKey)
                {
                    IdStorageUnit = idStorageUnit,
                    Name = record.name,
                    Location = record.location,
                    Dimensions = record.dimensions,
                    Level = record.level,
                    IdStorageType = record.idStorageType,
                    IdParent = record.idParent,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var resp = req.CreateResponse(HttpStatusCode.Created);
                await resp.WriteAsJsonAsync<StorageUnitEntity>(entity);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting storage unit");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error inserting storage unit: {ex}");
                return resp;
            }
        }

        // PUT
        [Function("putStorageUnit")]
        public static async Task<HttpResponseData> PutStorageUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "storageUnits/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("StorageUnitController");
            logger.LogInformation("PUT -> updating storage unit {RowKey}", rowKey);

            try
            {
                var current = await _table.QueryFirstOrDefaultAsync<StorageUnitEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (current is null)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return nf;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<StorageUnit>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                // Si cambia el padre, cambia la RowKey (prefijo)
                var idStorageUnit = current.IdStorageUnit; // fuente de la verdad
                if (!string.Equals(updated.idParent, current.IdParent, StringComparison.Ordinal))
                {
                    var newRowKey = $"{updated.idParent}|{idStorageUnit}";

                    var replacement = new StorageUnitEntity(_partitionKey, newRowKey)
                    {
                        IdStorageUnit = idStorageUnit,
                        Name = updated.name,
                        Location = updated.location,
                        Dimensions = updated.dimensions,
                        Level = updated.level,
                        IdStorageType = updated.idStorageType,
                        IdParent = updated.idParent,
                        Active = updated.active
                    };

                    // Inserta nuevo y borra el anterior (transición simple)
                    await _table.AddEntityAsync(replacement);

                    try
                    {
                        await _table.DeleteEntityAsync(current.PartitionKey, current.RowKey, current.ETag);
                    }
                    catch (Exception ex)
                    {
                        // rollback del insert si el delete falló
                        await _table.DeleteEntityAsync(replacement.PartitionKey, replacement.RowKey);
                        throw new InvalidOperationException("Failed moving entity to new RowKey", ex);
                    }

                    var respMoved = req.CreateResponse(HttpStatusCode.OK);
                    await respMoved.WriteAsJsonAsync<StorageUnitEntity>(replacement);
                    return respMoved;
                }
                else
                {
                    // Actualización en la misma RowKey
                    current.Name = updated.name;
                    current.Location = updated.location;
                    current.Dimensions = updated.dimensions;
                    current.Level = updated.level;
                    current.IdStorageType = updated.idStorageType;
                    current.Active = updated.active;

                    await _table.UpdateEntityAsync(current, current.ETag, TableUpdateMode.Merge);

                    var resp = req.CreateResponse(HttpStatusCode.OK);
                    await resp.WriteAsJsonAsync<StorageUnitEntity>(current);
                    return resp;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating storage unit {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error updating storage unit {rowKey}: {ex}");
                return resp;
            }
        }

        // DELETE
        [Function("deleteStorageUnit")]
        public static async Task<HttpResponseData> DeleteStorageUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "storageUnits/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("StorageUnitController");
            logger.LogInformation("DELETE -> removing storage unit {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<StorageUnitEntity>(
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
                logger.LogError(ex, "DELETE -> error removing storage unit {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error removing storage unit {rowKey}: {ex}");
                return resp;
            }
        }
    }
}
