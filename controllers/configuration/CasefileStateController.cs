using System.Net;
using Azure;
using Azure.Data.Tables;
using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;   // CasefileStateEntity : ITableEntity
using AppFunctions.models;    // CasefileState (DTO: name, description, active)
using Extensions.Query;       // QueryAsync / QueryFirstOrDefaultAsync
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AppFunctions.controllers.configuration
{
    public class CasefileStateController
    {
        private const string PartitionKey = "CasefileState";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        // GET /casefileStates/{rowKey}
        [Function("getCasefileState")]
        public static async Task<HttpResponseData> getCasefileState(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "casefileStates/{rowKey}")] HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("CasefileStateController");
            logger.LogInformation($"GET -> getting casefile state with RowKey {rowKey}");

            try
            {
                var result = await _table.QueryFirstOrDefaultAsync<CasefileStateEntity>(
                    x => x.PartitionKey == PartitionKey && x.RowKey == rowKey);

                var response = req.CreateResponse(result is null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
                await response.WriteAsJsonAsync<object>(result is null ? new { message = $"No record found with RowKey {rowKey}" } : (object)result);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"GET -> error to get casefile state {rowKey}: {ex}");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving casefile state {rowKey}: {ex}");
                return response;
            }
        }

        // GET /casefileStates
        [Function("getCasefileStates")]
        public static async Task<HttpResponseData> getCasefileStates(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "casefileStates")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("CasefileStateController");
            logger.LogInformation("GET ALL -> getting all casefile states");

            try
            {
                var results = new List<CasefileStateEntity>();
                await foreach (var item in _table.QueryAsync<CasefileStateEntity>(x => x.PartitionKey == PartitionKey))
                {
                    results.Add(item);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(results);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"GET ALL -> error getting casefile states: {ex}");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving casefile states: {ex}");
                return response;
            }
        }

        // POST /casefileStates
        [Function("postCasefileState")]
        public static async Task<HttpResponseData> postCasefileState(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "casefileStates")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("CasefileStateController");
            logger.LogInformation("POST -> inserting casefile state");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<CasefileState>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid payload.");
                    return bad;
                }

                var rowKey = Guid.NewGuid().ToString();

                var entity = new CasefileStateEntity(PartitionKey, rowKey)
                {
                    Name = record.name,
                    Description = record.description,
                    Active = record.active is bool b ? b : true
                };

                await _table.AddEntityAsync(entity);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"POST -> error inserting casefile state: {ex}");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error inserting casefile state: {ex}");
                return response;
            }
        }

        // PUT /casefileStates/{rowKey}
        [Function("putCasefileState")]
        public static async Task<HttpResponseData> putCasefileState(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "casefileStates/{rowKey}")] HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("CasefileStateController");
            logger.LogInformation($"PUT -> updating casefile state with RowKey {rowKey}");

            try
            {
                // Obtener existente (para ETag)
                var existing = await _table.QueryFirstOrDefaultAsync<CasefileStateEntity>(
                    x => x.PartitionKey == PartitionKey && x.RowKey == rowKey);

                if (existing is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteStringAsync($"No record found with RowKey {rowKey}");
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<CasefileState>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid payload.");
                    return bad;
                }

                existing.Name = updated.name;
                existing.Description = updated.description;
                existing.Active = updated.active;

                // Usa Merge para actualizar solo campos cambiados, respetando ETag
                await _table.UpdateEntityAsync(existing, existing.ETag, TableUpdateMode.Merge);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(existing);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"PUT -> error updating casefile state {rowKey}: {ex}");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error updating casefile state {rowKey}: {ex}");
                return response;
            }
        }

        // DELETE /casefileStates/{rowKey}
        [Function("deleteCasefileState")]
        public static async Task<HttpResponseData> deleteCasefileState(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "casefileStates/{rowKey}")] HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("CasefileStateController");
            logger.LogInformation($"DELETE -> removing casefile state with RowKey {rowKey}");

            try
            {
                try
                {
                    await _table.DeleteEntityAsync(PartitionKey, rowKey, ETag.All);
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteStringAsync($"No record found with RowKey {rowKey}");
                    return nf;
                }

                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                logger.LogError($"DELETE -> error removing casefile state {rowKey}: {ex}");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error removing casefile state {rowKey}: {ex}");
                return response;
            }
        }
    }
}
