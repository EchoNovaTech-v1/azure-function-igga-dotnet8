using System.Net;
using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using Azure;
using Azure.Data.Tables;
using Extensions.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AppFunctions.controllers.security
{
    public class ActionController
    {
        // Partición de trabajo
        private const string _partitionKey = "Action";

        // Cliente de tabla (usa tu helper)
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableSecurity);

        // GET /actions/{rowKey}
        [Function("getAction")]
        public static async Task<HttpResponseData> GetAction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "actions/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var log = context.GetLogger("ActionController");
            log.LogInformation("GET -> getting the action with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<ActionEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<ActionEntity>(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET -> error to get the action {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the record the action {rowKey}: {ex}");
                return bad;
            }
        }

        // GET /actions
        [Function("getActions")]
        public static async Task<HttpResponseData> GetActions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "actions")]
            HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger("ActionController");
            log.LogInformation("GET ALL -> getting all the actions.");

            try
            {
                var list = await _table.QueryToListAsync<ActionEntity>(x => x.PartitionKey == _partitionKey);
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<List<ActionEntity>>(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET ALL -> error during the process of getting actions");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the records from the actions list: {ex}");
                return bad;
            }
        }

        // POST /actions
        [Function("postAction")]
        public static async Task<HttpResponseData> PostAction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "actions")]
            HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger("ActionController");
            log.LogInformation("POST -> inserting action.");

            var rowKey = Guid.NewGuid().ToString();

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<AppFunctions.models.Action>(body);

                if (record is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                var entity = new ActionEntity(_partitionKey, rowKey)
                {
                    Name = record.name,
                    Description = record.description,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var created = req.CreateResponse(HttpStatusCode.Created);
                await created.WriteAsJsonAsync<ActionEntity>(entity);
                return created;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "POST -> error during the process of inserting the action");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while inserting the record of the action list: {ex}");
                return bad;
            }
        }

        // PUT /actions/{rowKey}
        [Function("putAction")]
        public static async Task<HttpResponseData> PutAction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "actions/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var log = context.GetLogger("ActionController");
            log.LogInformation("PUT -> updating action with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<ActionEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<AppFunctions.models.Action>(body);

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
                await ok.WriteAsJsonAsync<ActionEntity>(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "PUT -> error during the process of updating the action {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while updating the action {rowKey}: {ex}");
                return bad;
            }
        }

        // DELETE /actions/{rowKey}
        [Function("deleteAction")]
        public static async Task<HttpResponseData> DeleteAction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "actions/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var log = context.GetLogger("ActionController");
            log.LogInformation("DELETE -> removing the action with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<ActionEntity>(
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
                log.LogError(ex, "DELETE -> error during the process of removing the action {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while removing the record from the action list: {ex}");
                return bad;
            }
        }
    }
}
