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
using System.Net;

namespace AppFunctions.controllers.configuration
{
    public class RoleController
    {
        // Partición de trabajo
        private const string _partitionKey = "Role";

        // Cliente de tabla (helper propio)
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableSecurity);

        // POST /roles
        [Function("postRole")]
        public static async Task<HttpResponseData> PostRole(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "roles")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("RoleController");
            log.LogInformation("POST -> inserting roles.");

            var rowKey = Guid.NewGuid().ToString();

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Role>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var entity = new RoleEntity(_partitionKey, rowKey)
                {
                    Name = record.name,
                    Description = record.description,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var created = req.CreateResponse(HttpStatusCode.Created);
                await created.WriteAsJsonAsync(entity);
                return created;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "POST -> error during the process of inserting the roles");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while inserting the record of the role list: {ex}");
                return bad;
            }
        }

        // PUT /roles/{rowKey}
        [Function("putRole")]
        public static async Task<HttpResponseData> PutRole(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "roles/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("RoleController");
            log.LogInformation("PUT -> updating role with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<RoleEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<Role>(body);

                if (updated is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                entity.Name = updated.name;
                entity.Description = updated.description;
                entity.Active = updated.active;

                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "PUT -> error during the process of updating the role {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while updating the role {rowKey}: {ex}");
                return bad;
            }
        }

        // DELETE /roles/{rowKey}
        [Function("deleteRole")]
        public static async Task<HttpResponseData> DeleteRole(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "roles/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("RoleController");
            log.LogInformation("DELETE -> removing the role with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<RoleEntity>(
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
                log.LogError(ex, "DELETE -> error during the process of removing the role {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while removing the record from the role list: {ex}");
                return bad;
            }
        }

        // GET /roles
        [Function("getRoles")]
        public static async Task<HttpResponseData> GetRoles(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "roles")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("RoleController");
            log.LogInformation("GET ALL -> getting all the roles.");

            try
            {
                var list = await _table.QueryToListAsync<RoleEntity>(x => x.PartitionKey == _partitionKey);
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET ALL -> error during the process of getting roles");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the records from the role list: {ex}");
                return bad;
            }
        }

        // GET /roles/{rowKey}
        [Function("getRole")]
        public static async Task<HttpResponseData> GetRole(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "roles/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("RoleController");
            log.LogInformation("GET -> getting the role with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<RoleEntity>(
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
                log.LogError(ex, "GET -> error to get the role {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the record the role {rowKey}: {ex}");
                return bad;
            }
        }
    }
}
