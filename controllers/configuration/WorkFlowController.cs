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
    /// <summary>
    /// Controlador que expone las operaciones basicas de CRUD de la entidad Workflow (migrado a .NET Isolated).
    /// </summary>
    public class WorkFlowController
    {
        private const string _partitionKey = "Workflow";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        [Function("getWorkFlow")]
        public static async Task<HttpResponseData> GetWorkFlow(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "workflows/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("WorkFlowController");
            logger.LogInformation("GET -> getting the WorkFlow with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<WorkFlowEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<WorkFlowEntity>(entity);
                return ok;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error to get the workflow {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the workflow {rowKey}: {ex}");
                return bad;
            }
        }

        [Function("getWorkFlows")]
        public static async Task<HttpResponseData> GetWorkFlows(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "workflows")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("WorkFlowController");
            logger.LogInformation("GET ALL -> getting all the workflows.");

            try
            {
                var list = await _table.QueryToListAsync<WorkFlowEntity>(x => x.PartitionKey == _partitionKey);
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<List<WorkFlowEntity>>(list);
                return ok;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error during the process of getting WorkFlows");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the records from the WorkFlows list: {ex}");
                return bad;
            }
        }

        [Function("postWorkFlow")]
        public static async Task<HttpResponseData> PostWorkFlow(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "workflows")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("WorkFlowController");
            logger.LogInformation("POST -> inserting WorkFlow.");

            var rowKey = Guid.NewGuid().ToString();

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Workflow>(body);

                if (record is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                // valida nombre duplicado
                if (await ValidateNameWorkFlowAsync(record.Name, null))
                {
                    var badDup = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badDup.WriteStringAsync($"Error inserting Workflow, name already exists: {record.Name}");
                    return badDup;
                }

                var wf = new WorkFlowEntity(_partitionKey, rowKey)
                {
                    Active = true,
                    Description = record.Description,
                    Name = record.Name,
                    Prefix = record.Prefix
                };

                await _table.AddEntityAsync(wf);

                var created = req.CreateResponse(HttpStatusCode.Created);
                await created.WriteAsJsonAsync<WorkFlowEntity>(wf);
                return created;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error during the process of inserting the WorkFlow");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while inserting the WorkFlow: {ex}");
                return bad;
            }
        }

        [Function("putWorkFlow")]
        public static async Task<HttpResponseData> PutWorkFlow(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "workflows/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("WorkFlowController");
            logger.LogInformation("PUT -> updating WorkFlow with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<WorkFlowEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<Workflow>(body);

                if (updated is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                // valida nombre duplicado (excluyendo el mismo RowKey)
                if (await ValidateNameWorkFlowAsync(updated.Name, rowKey))
                {
                    var badDup = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badDup.WriteStringAsync($"Error updating Workflow, name already exists: {updated.Name}");
                    return badDup;
                }

                entity.Active = updated.Active;
                entity.Description = updated.Description;
                entity.Name = updated.Name;
                entity.Prefix = updated.Prefix;

                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<WorkFlowEntity>(entity);
                return ok;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error during the process of updating the WorkFlow {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while updating the WorkFlow {rowKey}: {ex}");
                return bad;
            }
        }

        [Function("deleteWorkFlow")]
        public static async Task<HttpResponseData> DeleteWorkFlow(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "workflows/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("WorkFlowController");
            logger.LogInformation("DELETE -> removing the workflow with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<WorkFlowEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                await _table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);
                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DELETE -> error during the process of removing the WorkFlow {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while removing the workflow {rowKey}: {ex}");
                return bad;
            }
        }

        /// <summary>
        /// Valida que no exista otro Workflow con el mismo nombre.
        /// </summary>
        private static async Task<bool> ValidateNameWorkFlowAsync(string wfName, string? excludeRowKey)
        {
            // Trae por nombre (misma partición)
            var sameName = await _table.QueryToListAsync<WorkFlowEntity>(
                x => x.PartitionKey == _partitionKey && x.Name == wfName);

            if (string.IsNullOrEmpty(excludeRowKey))
            {
                return sameName.Any(); // existe alguno con ese nombre
            }

            // existe alguno con el mismo nombre pero distinto RowKey
            return sameName.Any(e => !string.Equals(e.RowKey, excludeRowKey, StringComparison.Ordinal));
        }
    }
}
