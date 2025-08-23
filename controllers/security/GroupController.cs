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
    public class GroupController
    {
        // Partición de trabajo
        private const string _partitionKey = "Group";

        // Cliente de tabla (usa tu helper)
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableSecurity);

        // GET /groups/{rowKey}
        [Function("getGroup")]
        public static async Task<HttpResponseData> GetGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "groups/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("GroupController");
            log.LogInformation("GET -> getting the group with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<GroupEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<GroupEntity>(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET -> error to get the group {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the record the group {rowKey}: {ex}");
                return bad;
            }
        }

        // GET /groups
        [Function("getGroups")]
        public static async Task<HttpResponseData> GetGroups(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "groups")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("GroupController");
            log.LogInformation("GET ALL -> getting all the groups.");

            try
            {
                var list = await _table.QueryToListAsync<GroupEntity>(x => x.PartitionKey == _partitionKey);
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<List<GroupEntity>>(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET ALL -> error during the process of getting groups");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the records from the groups list: {ex}");
                return bad;
            }
        }

        // POST /groups
        [Function("postGroup")]
        public static async Task<HttpResponseData> PostGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "groups")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("GroupController");
            log.LogInformation("POST -> inserting group.");

            var rowKey = Guid.NewGuid().ToString();

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Group>(body);

                if (record is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                var entity = new GroupEntity(_partitionKey, rowKey)
                {
                    Name = record.name,
                    Description = record.description,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var created = req.CreateResponse(HttpStatusCode.Created);
                await created.WriteAsJsonAsync<GroupEntity>(entity);
                return created;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "POST -> error during the process of inserting the group");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while inserting the record of the group list: {ex}");
                return bad;
            }
        }

        // PUT /groups/{rowKey}
        [Function("putGroup")]
        public static async Task<HttpResponseData> PutGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "groups/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("GroupController");
            log.LogInformation("PUT -> updating group with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<GroupEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<Group>(body);

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
                await ok.WriteAsJsonAsync<GroupEntity>(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "PUT -> error during the process of updating the group {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while updating the group {rowKey}: {ex}");
                return bad;
            }
        }

        // DELETE /groups/{rowKey}
        [Function("deleteGroup")]
        public static async Task<HttpResponseData> DeleteGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "groups/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("GroupController");
            log.LogInformation("DELETE -> removing the group with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<GroupEntity>(
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
                log.LogError(ex, "DELETE -> error during the process of removing the group {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while removing the record from the group list: {ex}");
                return bad;
            }
        }
    }
}
