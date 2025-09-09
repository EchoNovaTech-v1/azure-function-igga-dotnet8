using System.Net;
using Azure;
using Azure.Data.Tables;
using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using AppFunctions.models;
using Extensions.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AppFunctions.controllers.configuration
{
    public class DocumentaryTypesController
    {
        private const string _partitionKey = "DocumentaryType";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        // POST /api/documentaryTypes
        [Function("postDocumentaryTypes")]
        public static async Task<HttpResponseData> PostDocumentaryTypes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "documentaryTypes")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("DocumentaryTypesController");
            logger.LogInformation("POST -> inserting documentaryTypes");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<DocumentaryTypes>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var rowKey = Guid.NewGuid().ToString();

                var entity = new DocumentaryTypesEntity(_partitionKey, rowKey)
                {
                    Name = record.name,
                    Description = record.description,
                    Keycode = record.keycode,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting documentaryTypes");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"There was an error while inserting the documentary type: {ex}");
                return response;
            }
        }

        // PUT /api/documentaryTypes/{rowKey}
        [Function("putDocumentaryTypes")]
        public static async Task<HttpResponseData> PutDocumentaryTypes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "documentaryTypes/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("DocumentaryTypesController");
            logger.LogInformation("PUT -> updating documentaryTypes {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<DocumentaryTypesEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<DocumentaryTypes>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                entity.Name = updated.name;
                entity.Description = updated.description;
                entity.Keycode = updated.keycode;
                entity.Active = updated.active;

                try
                {
                    await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);
                }
                catch (RequestFailedException rfe) when (rfe.Status == 412)
                {
                    var pre = req.CreateResponse(HttpStatusCode.PreconditionFailed);
                    await pre.WriteStringAsync("Concurrency conflict. Reload and retry.");
                    return pre;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating documentaryTypes {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"There was an error while updating the documentary type {rowKey}: {ex}");
                return response;
            }
        }

        // DELETE /api/documentaryTypes/{rowKey}
        [Function("deleteDocumentaryTypes")]
        public static async Task<HttpResponseData> DeleteDocumentaryTypes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "documentaryTypes/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("DocumentaryTypesController");
            logger.LogInformation("DELETE -> removing documentaryTypes {RowKey}", rowKey);

            try
            {
                var res = await _table.GetEntityIfExistsAsync<DocumentaryTypesEntity>(_partitionKey, rowKey);
                if (!res.HasValue)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                await _table.DeleteEntityAsync(_partitionKey, rowKey, res.Value.ETag);
                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DELETE -> error removing documentaryTypes {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"There was an error while removing the documentary type {rowKey}: {ex}");
                return response;
            }
        }

        // GET /api/documentaryTypes
        [Function("getDocumentaryTypes")]
        public static async Task<HttpResponseData> GetDocumentaryTypes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "documentaryTypes")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("DocumentaryTypesController");
            logger.LogInformation("GET ALL -> getting all documentaryTypes");

            try
            {
                var list = await _table.QueryToListAsync<DocumentaryTypesEntity>(x => x.PartitionKey == _partitionKey);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(list);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error getting documentaryTypes");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"There was an error when obtaining documentary types: {ex}");
                return response;
            }
        }

        // GET /api/documentaryTypes/{rowKey}
        [Function("getDocumentaryType")]
        public static async Task<HttpResponseData> GetDocumentaryType(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "documentaryTypes/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("DocumentaryTypesController");
            logger.LogInformation("GET Id -> getting documentaryType {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<DocumentaryTypesEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                var response = req.CreateResponse(entity is null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
                if (entity is null)
                    await response.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                else
                    await response.WriteAsJsonAsync(entity);

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET Id -> error getting documentaryType {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"There was an error when obtaining the documentary type {rowKey}: {ex}");
                return response;
            }
        }
    }
}
