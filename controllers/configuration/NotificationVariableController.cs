using System.Collections.Generic;
using System;
using System.Net;
using System.Threading.Tasks;
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

namespace AppFunctions.controllers.configuration
{
    public class NotificationVariableController
    {
        private const string _partitionKey = "NotificationVariable";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        [Function("getNotificationVariable")]
        public static async Task<HttpResponseData> GetNotificationVariable(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "notificationsVariables/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("NotificationVariableController");
            logger.LogInformation("GET -> getting NotificationVariable {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<NotificationVariableEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                var resp = req.CreateResponse(entity is null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
                if (entity is null)
                    await resp.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                else
                    await resp.WriteAsJsonAsync<NotificationVariableEntity>(entity);

                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting NotificationVariable {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving NotificationVariable {rowKey}: {ex}");
                return resp;
            }
        }

        [Function("getNotificationVariables")]
        public static async Task<HttpResponseData> GetNotificationVariables(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "notificationsVariables")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("NotificationVariableController");
            logger.LogInformation("GET ALL -> getting all NotificationVariables");

            try
            {
                var list = await _table.QueryToListAsync<NotificationVariableEntity>(x => x.PartitionKey == _partitionKey);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<IList<NotificationVariableEntity>>(list);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving NotificationVariables: {ex}");
                return resp;
            }
        }

        [Function("postNotificationVariable")]
        public static async Task<HttpResponseData> PostNotificationVariable(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "notificationsVariables")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("NotificationVariableController");
            logger.LogInformation("POST -> inserting NotificationVariable");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<NotificationVariable>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var rowKey = Guid.NewGuid().ToString();

                var entity = new NotificationVariableEntity(_partitionKey, rowKey)
                {
                    Name = record.name,
                    OpenChar = record.openChar,
                    CloseChar = record.closeChar,
                    TableName = record.tableName,
                    PartitionName = record.partitionName,
                    FieldName = record.fieldName,
                    Description = record.description,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var resp = req.CreateResponse(HttpStatusCode.Created);
                await resp.WriteAsJsonAsync<NotificationVariableEntity>(entity);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error inserting NotificationVariable: {ex}");
                return resp;
            }
        }

        [Function("putNotificationVariable")]
        public static async Task<HttpResponseData> PutNotificationVariable(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "notificationsVariables/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("NotificationVariableController");
            logger.LogInformation("PUT -> updating NotificationVariable {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<NotificationVariableEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return nf;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<NotificationVariable>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                entity.Name = updated.name;
                entity.OpenChar = updated.openChar;
                entity.CloseChar = updated.closeChar;
                entity.TableName = updated.tableName;
                entity.PartitionName = updated.partitionName;
                entity.FieldName = updated.fieldName;
                entity.Description = updated.description;
                entity.Active = updated.active;

                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<NotificationVariableEntity>(entity);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating NotificationVariable {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error updating NotificationVariable {rowKey}: {ex}");
                return resp;
            }
        }

        [Function("deleteNotificationVariable")]
        public static async Task<HttpResponseData> DeleteNotificationVariable(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "notificationsVariables/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("NotificationVariableController");
            logger.LogInformation("DELETE -> removing NotificationVariable {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<NotificationVariableEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return nf;
                }

                await _table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DELETE -> error removing NotificationVariable {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error removing NotificationVariable {rowKey}: {ex}");
                return resp;
            }
        }
    }
}
