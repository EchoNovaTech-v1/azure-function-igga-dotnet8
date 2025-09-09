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
    public class PermissionController
    {
        private const string _partitionKey = "Permission";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableSecurity);

        // GET /permissions/{rowKey}   (StartsWith por RowKey)
        [Function("getPermission")]
        public static async Task<HttpResponseData> GetPermission(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "permissions/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("PermissionController");
            log.LogInformation("GET -> getting the permission(s) with RowKey prefix {RowKey}.", rowKey);

            try
            {
                // StartsWith usando rango: RowKey >= prefix && RowKey < nextPrefix
                var nextPrefix = NextPrefix(rowKey);
                var filter = TableClient.CreateQueryFilter($"PartitionKey eq {_partitionKey} and RowKey ge {rowKey} and RowKey lt {nextPrefix}");

                var list = new List<PermissionEntity>();
                await foreach (var e in _table.QueryAsync<PermissionEntity>(filter: filter))
                {
                    list.Add(e);
                }

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET -> error to get the permission(s) {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the record(s) the permission {rowKey}: {ex}");
                return bad;
            }
        }

        // GET /permissions
        [Function("getPermissions")]
        public static async Task<HttpResponseData> GetPermissions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "permissions")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("PermissionController");
            log.LogInformation("GET ALL -> getting all the permissions.");

            try
            {
                var list = await _table.QueryToListAsync<PermissionEntity>(x => x.PartitionKey == _partitionKey);
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET ALL -> error during the process of getting permissions");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the records from the permissions list: {ex}");
                return bad;
            }
        }

        // POST /permissions
        [Function("postPermission")]
        public static async Task<HttpResponseData> PostPermission(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "permissions")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("PermissionController");
            log.LogInformation("POST -> inserting permission.");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Permission>(body);

                if (record is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                var guidPermission = Guid.NewGuid().ToString();
                var rowKey = $"{record.idResource}|{record.partitionName}|-{record.idRecord}|+|{record.idRole}|*|{guidPermission}";

                var entity = new PermissionEntity(_partitionKey, rowKey)
                {
                    IdPermission = guidPermission,
                    IdResource = record.idResource,
                    PartitionName = record.partitionName,
                    IdRecord = record.idRecord,
                    IdRole = record.idRole,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var created = req.CreateResponse(HttpStatusCode.Created);
                await created.WriteAsJsonAsync(entity);
                return created;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "POST -> error during the process of inserting the permission");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while inserting the record of the permission list: {ex}");
                return bad;
            }
        }

        // PUT /permissions/{rowKey}
        [Function("putPermission")]
        public static async Task<HttpResponseData> PutPermission(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "permissions/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("PermissionController");
            log.LogInformation("PUT -> updating permission with RowKey {RowKey}.", rowKey);

            try
            {
                var current = await _table.QueryFirstOrDefaultAsync<PermissionEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (current is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<Permission>(body);

                if (updated is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                // Detectar cambios que requieren cambio de RowKey
                var needsKeyChange =
                    !string.Equals(current.IdResource, updated.idResource, StringComparison.Ordinal) ||
                    !string.Equals(current.PartitionName, updated.partitionName, StringComparison.Ordinal) ||
                    !string.Equals(current.IdRecord, updated.idRecord, StringComparison.Ordinal) ||
                    !string.Equals(current.IdRole, updated.idRole, StringComparison.Ordinal);

                if (needsKeyChange)
                {
                    var newRowKey = $"{updated.idResource}|{updated.partitionName}|-{updated.idRecord}|+|{updated.idRole}|*|{updated.idPermission ?? current.IdPermission}";

                    var newEntity = new PermissionEntity(_partitionKey, newRowKey)
                    {
                        IdPermission = updated.idPermission ?? current.IdPermission,
                        IdResource = updated.idResource,
                        PartitionName = updated.partitionName,
                        IdRecord = updated.idRecord,
                        IdRole = updated.idRole,
                        Active = updated.active
                    };

                    // Insertar nuevo y luego eliminar el viejo (rollback si algo falla)
                    await _table.AddEntityAsync(newEntity);

                    try
                    {
                        await _table.DeleteEntityAsync(current.PartitionKey, current.RowKey, current.ETag);
                    }
                    catch (Exception)
                    {
                        // rollback de la inserción
                        await _table.DeleteEntityAsync(newEntity.PartitionKey, newEntity.RowKey);
                        throw;
                    }

                    var okNew = req.CreateResponse(HttpStatusCode.OK);
                    await okNew.WriteAsJsonAsync(newEntity);
                    return okNew;
                }
                else
                {
                    // Solo actualizar banderas/campos sin cambiar RowKey
                    current.Active = updated.active;
                    // (si quieres permitir actualizar otros campos cuando no cambia la clave)
                    current.IdResource = updated.idResource;
                    current.PartitionName = updated.partitionName;
                    current.IdRecord = updated.idRecord;
                    current.IdRole = updated.idRole;

                    await _table.UpdateEntityAsync(current, current.ETag, TableUpdateMode.Merge);

                    var ok = req.CreateResponse(HttpStatusCode.OK);
                    await ok.WriteAsJsonAsync(current);
                    return ok;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "PUT -> error during the process of updating the permission {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while updating the permission {rowKey}: {ex}");
                return bad;
            }
        }

        // DELETE /permissions/{rowKey}
        [Function("deletePermission")]
        public static async Task<HttpResponseData> DeletePermission(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "permissions/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("PermissionController");
            log.LogInformation("DELETE -> removing the permission with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<PermissionEntity>(
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
                log.LogError(ex, "DELETE -> error during the process of removing the permission {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while removing the record from the permissions list: {ex}");
                return bad;
            }
        }

        // Helpers
        private static string NextPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return "\uFFFF"; // seguridad
            var lastChar = prefix[^1];
            var nextLastChar = (char)(lastChar + 1);
            return prefix.Substring(0, prefix.Length - 1) + nextLastChar;
        }
    }
}
