using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using AppFunctions.models;
using Azure.Data.Tables;
using Extensions.Http;
using Extensions.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AppFunctions.controllers.configuration
{
    public class AuditController
    {
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableAudit);

        [Function("getAudit")]
        public static async Task<HttpResponseData> getAudit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "audits/{rowKey}")] HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("AuditController");
            logger.LogInformation($"GET -> getting the audit with RowKey {rowKey}");

            try
            {
                var result = await _table.QueryFirstOrDefaultAsync<AuditEntity>(e => e.RowKey == rowKey);
                return await req.OkAsJsonAsync(result ?? new object());
            }
            catch (Exception ex)
            {
                logger.LogError($"GET -> error getting audit {rowKey}: {ex}");
                return await req.BadRequestAsync(
                    $"There was an error retrieving the audit: {ex.Message}");
            }
        }

        [Function("getAudits")]
        public static async Task<HttpResponseData> getAudits(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "audits")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("AuditController");
            logger.LogInformation("GET ALL -> getting all the audits");

            try
            {
                var audits =  await _table.QueryToListAsync<AuditEntity>();
                return await req.OkAsJsonAsync(audits);
            }
            catch (Exception ex)
            {
                logger.LogError($"GET ALL -> error getting audits: {ex}");
                return await req.BadRequestAsync(
                    $"Error retrieving audits: {ex.Message}");
            }
        }

        [Function("postAudit")]
        public static async Task<HttpResponseData> postAudit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "audits")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("AuditController");
            logger.LogInformation("POST -> inserting audit");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Audit>(body);

                var partitionKey = record.idResource;
                var rowKey = Guid.NewGuid().ToString();

                var audit = new AuditEntity(partitionKey, rowKey)
                {
                    CreatedDate = DateTime.UtcNow,
                    IdAction = record.idAction,
                    IdDirectory = record.idDirectory,
                    IdResource = record.idResource,
                    IdRol = record.idRol,
                    IdUser = record.idUser,
                    PartitionName = record.partitionName,
                    TableName = record.tableName
                };

                await _table.AddEntityAsync(audit);

                return await req.CreatedAsJsonAsync(audit);
            }
            catch (Exception ex)
            {
                logger.LogError($"POST -> error inserting audit: {ex}");
                return await req.BadRequestAsync(
                    $"Error inserting audit: {ex.Message}");
            }
        }

        [Function("putAudit")]
        public static async Task<HttpResponseData> putAudit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "audits/{rowKey}")] HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("AuditController");
            logger.LogInformation($"PUT -> updating audit with RowKey {rowKey}");

            try
            {
                var existing = await _table.QueryFirstOrDefaultAsync<AuditEntity>(e => e.RowKey == rowKey);
                if (existing == null)
                    return await req.NotFoundAsync($"No record found with RowKey {rowKey}");

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<Audit>(body);

                if (existing.IdResource != updated.idResource)
                {
                    var newAudit = new AuditEntity(updated.idResource, existing.RowKey)
                    {
                        CreatedDate = updated.createdDate,
                        IdAction = updated.idAction,
                        IdDirectory = updated.idDirectory,
                        IdResource = updated.idResource,
                        IdRol = updated.idRol,
                        IdUser = updated.idUser,
                        PartitionName = updated.partitionName,
                        TableName = updated.tableName
                    };

                    await _table.AddEntityAsync(newAudit);
                    await _table.DeleteEntityAsync(existing.PartitionKey, existing.RowKey);
                    existing = newAudit;
                }
                else
                {
                    existing.CreatedDate = updated.createdDate;
                    existing.IdAction = updated.idAction;
                    existing.IdDirectory = updated.idDirectory;
                    existing.IdResource = updated.idResource;
                    existing.IdRol = updated.idRol;
                    existing.IdUser = updated.idUser;
                    existing.PartitionName = updated.partitionName;
                    existing.TableName = updated.tableName;

                    await _table.UpdateEntityAsync(
                        existing,
                        existing.ETag,
                        TableUpdateMode.Replace);
                }

                return await req.OkAsJsonAsync(existing);
            }
            catch (Exception ex)
            {
                logger.LogError($"PUT -> error updating audit {rowKey}: {ex}");
                return await req.BadRequestAsync(
                    $"Error updating audit: {ex.Message}");
            }
        }

        [Function("deleteAudit")]
        public static async Task<HttpResponseData> deleteAudit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "audits/{rowKey}")] HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("AuditController");
            logger.LogInformation($"DELETE -> removing the audit with RowKey {rowKey}");

            try
            {
                var existing = await _table.QueryFirstOrDefaultAsync<AuditEntity>(e => e.RowKey == rowKey);

                if (existing == null)
                    return await req.NotFoundAsync($"No record found with RowKey {rowKey}");

                await _table.DeleteEntityAsync(existing.PartitionKey, existing.RowKey);
                return await req.NoContentAsync();
            }
            catch (Exception ex)
            {
                logger.LogError($"DELETE -> error removing audit {rowKey}: {ex}");
                return await req.ErrorAsync(
                    $"Error deleting audit: {ex.Message}");
            }
        }
    }
}
