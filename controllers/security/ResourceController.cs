using System.Collections.Generic;
using System;
using System.Net;
using System.Threading.Tasks;
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
    public class ResourceController
    {
        private const string _partitionKey = "Resource";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableSecurity);

        // GET /resources/{rowKey}  (StartsWith por RowKey)
        [Function("getResource")]
        public static async Task<HttpResponseData> GetResource(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "resources/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("ResourceController");
            log.LogInformation("GET -> getting the resource(s) with RowKey prefix {RowKey}.", rowKey);

            try
            {
                var nextPrefix = NextPrefix(rowKey);
                // PartitionKey == 'Resource' AND RowKey in [rowKey, nextPrefix)
                var filter = TableClient.CreateQueryFilter(
                    $"PartitionKey eq {_partitionKey} and RowKey ge {rowKey} and RowKey lt {nextPrefix}");

                var list = new List<ResourceEntity>();
                await foreach (var e in _table.QueryAsync<ResourceEntity>(filter: filter))
                {
                    list.Add(e);
                }

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET -> error to get the resource(s) {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the record the resource {rowKey}: {ex}");
                return bad;
            }
        }

        // GET /resources
        [Function("getResources")]
        public static async Task<HttpResponseData> GetResources(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "resources")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("ResourceController");
            log.LogInformation("GET ALL -> getting all the resources.");

            try
            {
                var list = await _table.QueryToListAsync<ResourceEntity>(x => x.PartitionKey == _partitionKey);
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET ALL -> error during the process of getting resources");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the records from the resources list: {ex}");
                return bad;
            }
        }

        // POST /resources
        [Function("postResource")]
        public static async Task<HttpResponseData> PostResource(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "resources")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("ResourceController");
            log.LogInformation("POST -> inserting resource.");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Resource>(body);

                if (record is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                var guidResource = !string.IsNullOrWhiteSpace(record.idResource)
                    ? record.idResource
                    : Guid.NewGuid().ToString();

                var rowKey = string.IsNullOrWhiteSpace(record.idParent)
                    ? $"{guidResource}|{guidResource}"
                    : $"{record.idParent}|{guidResource}";

                var entity = new ResourceEntity(_partitionKey, rowKey)
                {
                    IdResource = guidResource,
                    Name = record.name,
                    Description = record.description,
                    Type = record.type,
                    Hereditary = record.hereditary,
                    IdParent = record.idParent,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var created = req.CreateResponse(HttpStatusCode.Created);
                await created.WriteAsJsonAsync(entity);
                return created;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "POST -> error during the process of inserting the resource");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while inserting the record of the resource list: {ex}");
                return bad;
            }
        }

        // PUT /resources/{rowKey}
        [Function("putResource")]
        public static async Task<HttpResponseData> PutResource(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "resources/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("ResourceController");
            log.LogInformation("PUT -> updating resource with RowKey {RowKey}.", rowKey);

            try
            {
                var current = await _table.QueryFirstOrDefaultAsync<ResourceEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (current is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<Resource>(body);

                if (updated is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                var parentChanged = !string.Equals(updated.idParent, current.IdParent, StringComparison.Ordinal);

                if (parentChanged)
                {
                    var newRowKey = string.IsNullOrWhiteSpace(updated.idParent)
                        ? $"{current.IdResource}|{current.IdResource}"
                        : $"{updated.idParent}|{current.IdResource}";

                    var newEntity = new ResourceEntity(_partitionKey, newRowKey)
                    {
                        IdResource = current.IdResource,
                        Name = updated.name,
                        Description = updated.description,
                        Type = updated.type,
                        Hereditary = updated.hereditary,
                        IdParent = updated.idParent,
                        Active = updated.active
                    };

                    // Insertar nuevo y luego eliminar el viejo; si falla, rollback
                    await _table.AddEntityAsync(newEntity);

                    try
                    {
                        await _table.DeleteEntityAsync(current.PartitionKey, current.RowKey, current.ETag);
                    }
                    catch (Exception)
                    {
                        await _table.DeleteEntityAsync(newEntity.PartitionKey, newEntity.RowKey);
                        throw;
                    }

                    var okNew = req.CreateResponse(HttpStatusCode.OK);
                    await okNew.WriteAsJsonAsync(newEntity);
                    return okNew;
                }
                else
                {
                    // Actualización sin cambio de RowKey
                    current.Name = updated.name;
                    current.Description = updated.description;
                    current.Type = updated.type;
                    current.Hereditary = updated.hereditary;
                    current.Active = updated.active;

                    await _table.UpdateEntityAsync(current, current.ETag, TableUpdateMode.Merge);

                    var ok = req.CreateResponse(HttpStatusCode.OK);
                    await ok.WriteAsJsonAsync(current);
                    return ok;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "PUT -> error during the process of updating the resource {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while updating the resource {rowKey}: {ex}");
                return bad;
            }
        }

        // DELETE /resources/{rowKey}
        [Function("deleteResource")]
        public static async Task<HttpResponseData> DeleteResource(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "resources/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("ResourceController");
            log.LogInformation("DELETE -> removing the resource with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<ResourceEntity>(
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
                log.LogError(ex, "DELETE -> error during the process of removing the resource {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while removing the record from the resource list: {ex}");
                return bad;
            }
        }

        // Helpers
        private static string NextPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return "\uFFFF";
            var last = prefix[^1];
            var next = (char)(last + 1);
            return prefix[..^1] + next;
        }
    }
}
