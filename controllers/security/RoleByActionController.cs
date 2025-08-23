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
    public class RoleByActionController
    {
        // Partición de trabajo
        private const string _partitionKey = "ActionsByRole";

        // Cliente de tabla (helper propio)
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableSecurity);

        // POST /actionsByRole
        [Function("postActionsByRole")]
        public static async Task<HttpResponseData> PostActionsByRole(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "actionsByRole")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("RoleByActionController");
            log.LogInformation("POST -> inserting actionByRole.");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<RoleByAction>(body);

                if (record is null
                    || string.IsNullOrWhiteSpace(record.idRole)
                    || string.IsNullOrWhiteSpace(record.idAction))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body. 'idRole' and 'idAction' are required.");
                    return bad;
                }

                // RowKey: role|action
                var rowKey = $"{record.idRole}|{record.idAction}";

                var entity = new RoleByActionEntity(_partitionKey, rowKey)
                {
                    IdRole = record.idRole,
                    IdAction = record.idAction,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var created = req.CreateResponse(HttpStatusCode.Created);
                await created.WriteAsJsonAsync(entity);
                return created;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "POST -> error during the process of inserting the role by action");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while inserting the record of the role by action list: {ex}");
                return bad;
            }
        }

        // PUT /actionsByRole/{rowKey}
        [Function("putActionsByRole")]
        public static async Task<HttpResponseData> PutActionsByRole(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "actionsByRole/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("RoleByActionController");
            log.LogInformation("PUT -> updating action by role with RowKey {RowKey}.", rowKey);

            try
            {
                var current = await _table.QueryFirstOrDefaultAsync<RoleByActionEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (current is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<RoleByAction>(body);

                if (updated is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                var keysChanged =
                    !string.Equals(updated.idRole, current.IdRole, StringComparison.Ordinal) ||
                    !string.Equals(updated.idAction, current.IdAction, StringComparison.Ordinal);

                if (keysChanged)
                {
                    // Nuevo RowKey por cambio de IdRole/IdAction
                    var newRowKey = $"{updated.idRole}|{updated.idAction}";

                    var newEntity = new RoleByActionEntity(_partitionKey, newRowKey)
                    {
                        IdRole = updated.idRole,
                        IdAction = updated.idAction,
                        Active = updated.active
                    };

                    // Insertar nuevo y luego eliminar el anterior; si la eliminación falla, rollback
                    await _table.AddEntityAsync(newEntity);

                    try
                    {
                        await _table.DeleteEntityAsync(current.PartitionKey, current.RowKey, current.ETag);
                    }
                    catch
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
                    // Solo cambia Active (misma llave)
                    current.Active = updated.active;
                    await _table.UpdateEntityAsync(current, current.ETag, TableUpdateMode.Merge);

                    var ok = req.CreateResponse(HttpStatusCode.OK);
                    await ok.WriteAsJsonAsync(current);
                    return ok;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "PUT -> error during the process of updating the role by action {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while updating the role by action {rowKey}: {ex}");
                return bad;
            }
        }

        // DELETE /actionsByRole/{rowKey}
        [Function("deleteActionsByRole")]
        public static async Task<HttpResponseData> DeleteActionsByRole(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "actionsByRole/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("RoleByActionController");
            log.LogInformation("DELETE -> removing the role by action with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<RoleByActionEntity>(
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
                log.LogError(ex, "DELETE -> error during the process of removing the action by role {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while removing the record from the action by role list: {ex}");
                return bad;
            }
        }

        // GET /actionsByRole
        [Function("getActionsByRoles")]
        public static async Task<HttpResponseData> GetActionsByRoles(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "actionsByRole")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("RoleByActionController");
            log.LogInformation("GET ALL -> getting all the actions by roles.");

            try
            {
                var list = await _table.QueryToListAsync<RoleByActionEntity>(x => x.PartitionKey == _partitionKey);
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET ALL -> error during the process of getting actions by roles");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the records from the actions by roles list: {ex}");
                return bad;
            }
        }

        // GET /actionsByRole/{rowKey}
        [Function("getActionsByRole")]
        public static async Task<HttpResponseData> GetActionsByRole(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "actionsByRole/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("RoleByActionController");
            log.LogInformation("GET -> getting the action by role with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<RoleByActionEntity>(
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
                log.LogError(ex, "GET -> error to get the actions By Role {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the record the actions By Role {rowKey}: {ex}");
                return bad;
            }
        }
    }
}
