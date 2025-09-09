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
    public class UserByGroupController
    {
        // Partición de trabajo
        private const string _partitionKey = "UsersByGroup";

        // Cliente de tabla (helper propio)
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableSecurity);

        static UserByGroupController()
        {
            try { _table.CreateIfNotExists(); } catch { /* no-op */ }
        }

        // GET /usersByGroup/{rowKey}
        // Si recibe id de usuario en rowKey => retorna el/los grupos a los que pertenece (prefix match)
        // Si recibe id de grupo en rowKey   => retorna sus usuarios (prefix match)
        [Function("getUsersByGroup")]
        public static async Task<HttpResponseData> GetUsersByGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "usersByGroup/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("UserByGroupController");
            log.LogInformation("GET -> getting the users by group with RowKey {RowKey}.", rowKey);

            try
            {
                if (string.IsNullOrEmpty(rowKey))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("rowKey is required.");
                    return bad;
                }

                // StartsWith usando rango lexicográfico: [rowKey, nextPrefix)
                char last = rowKey[^1];
                char nextLast = (char)((int)last + 1);
                string nextPrefix = rowKey[..^1] + nextLast;

                string filter =
                    $"PartitionKey eq '{_partitionKey}' and RowKey ge '{rowKey}' and RowKey lt '{nextPrefix}'";

                var list = new List<UsersByGroupEntity>();
                await foreach (var e in _table.QueryAsync<UsersByGroupEntity>(filter))
                    list.Add(e);

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET -> error to get the users by group {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the record the users by group {rowKey}: {ex}");
                return bad;
            }
        }

        // GET /usersByGroup
        [Function("getUsersByGroups")]
        public static async Task<HttpResponseData> GetUsersByGroups(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "usersByGroup")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("UserByGroupController");
            log.LogInformation("GET ALL -> getting all the users by group.");

            try
            {
                var list = await _table.QueryToListAsync<UsersByGroupEntity>(x => x.PartitionKey == _partitionKey);
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET ALL -> error during the process of getting users by group");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the records from the users by group list: {ex}");
                return bad;
            }
        }

        // POST /usersByGroup
        [Function("postUsersByGroup")]
        public static async Task<HttpResponseData> PostUsersByGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "usersByGroup")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("UserByGroupController");
            log.LogInformation("POST -> inserting userByGroup.");

            try
            {
                var body = await req.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                UsersByGroup? record;
                try { record = JsonConvert.DeserializeObject<UsersByGroup>(body); }
                catch (JsonException)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid JSON payload.");
                    return bad;
                }

                if (record is null || string.IsNullOrWhiteSpace(record.idGroup) || string.IsNullOrWhiteSpace(record.idUser))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("idGroup and idUser are required.");
                    return bad;
                }

                string rowKey = $"{record.idGroup}|{record.idUser}";

                var entity = new UsersByGroupEntity(_partitionKey, rowKey)
                {
                    IdUser = record.idUser,
                    IdGroup = record.idGroup,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var created = req.CreateResponse(HttpStatusCode.Created);
                await created.WriteAsJsonAsync(entity);
                return created;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "POST -> error during the process of inserting the users by group");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while inserting the record of the users by group list: {ex}");
                return bad;
            }
        }

        // PUT /usersByGroup/{rowKey}
        [Function("putUsersByGroup")]
        public static async Task<HttpResponseData> PutUsersByGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "usersByGroup/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("UserByGroupController");
            log.LogInformation("PUT -> updating users by group with RowKey {RowKey}.", rowKey);

            try
            {
                var current = await _table.QueryFirstOrDefaultAsync<UsersByGroupEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (current is null)
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

                UsersByGroup? updated;
                try { updated = JsonConvert.DeserializeObject<UsersByGroup>(body); }
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

                bool keysChanged = (!string.Equals(updated.idGroup, current.IdGroup, StringComparison.Ordinal)) ||
                                   (!string.Equals(updated.idUser, current.IdUser, StringComparison.Ordinal));

                if (keysChanged)
                {
                    string newRowKey = $"{updated.idGroup}|{updated.idUser}";

                    var old = current;

                    var @new = new UsersByGroupEntity(_partitionKey, newRowKey)
                    {
                        IdGroup = updated.idGroup,
                        IdUser = updated.idUser,
                        Active = updated.active
                    };

                    await _table.AddEntityAsync(@new);

                    try
                    {
                        await _table.DeleteEntityAsync(old.PartitionKey, old.RowKey, old.ETag);
                    }
                    catch (Exception)
                    {
                        // rollback si falla el delete del viejo
                        await _table.DeleteEntityAsync(@new.PartitionKey, @new.RowKey);
                        throw;
                    }

                    var ok = req.CreateResponse(HttpStatusCode.OK);
                    await ok.WriteAsJsonAsync(@new);
                    return ok;
                }
                else
                {
                    current.Active = updated.active;
                    await _table.UpdateEntityAsync(current, current.ETag, TableUpdateMode.Merge);

                    var ok = req.CreateResponse(HttpStatusCode.OK);
                    await ok.WriteAsJsonAsync(current);
                    return ok;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "PUT -> error during the process of updating the users by group {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while updating the users by group {rowKey}: {ex}");
                return bad;
            }
        }

        // DELETE /usersByGroup/{rowKey}
        [Function("deleteUsersByGroup")]
        public static async Task<HttpResponseData> DeleteUsersByGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "usersByGroup/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("UserByGroupController");
            log.LogInformation("DELETE -> removing the users by group with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<UsersByGroupEntity>(
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
                log.LogError(ex, "DELETE -> error during the process of removing the users by group {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while removing the record from the users by group list: {ex}");
                return bad;
            }
        }
    }
}
