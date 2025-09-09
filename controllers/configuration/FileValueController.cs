using System.Net;
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
    /// <summary>
    /// CRUD de FileValue (tabla de configuraciones).
    /// </summary>
    public class FileValueController
    {
        private const string _partitionKey = "FileValue";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        [Function("getFileValue")]
        public static async Task<HttpResponseData> GetFileValue(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "fileValues/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("FileValueController");
            logger.LogInformation("GET -> getting FileValue {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<FileValueEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                var response = req.CreateResponse(entity is null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
                if (entity is null)
                {
                    await response.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                }
                else
                {
                    await response.WriteAsJsonAsync<FileValueEntity>(entity);
                }
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting FileValue {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving FileValue {rowKey}: {ex}");
                return response;
            }
        }

        [Function("getFileValues")]
        public static async Task<HttpResponseData> GetFileValues(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "fileValues")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("FileValueController");
            logger.LogInformation("GET ALL -> getting all FileValues");

            try
            {
                var list = await _table.QueryToListAsync<FileValueEntity>(x => x.PartitionKey == _partitionKey);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync<IList<FileValueEntity>>(list);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error getting FileValues");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving FileValues: {ex}");
                return response;
            }
        }

        [Function("postFileValue")]
        public static async Task<HttpResponseData> PostFileValue(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "fileValues")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("FileValueController");
            logger.LogInformation("POST -> inserting FileValue");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<FileValue>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var rowKey = Guid.NewGuid().ToString();

                var entity = new FileValueEntity(_partitionKey, rowKey)
                {
                    Name = record.name,
                    Description = record.description,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync<FileValueEntity>(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting FileValue");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error inserting FileValue: {ex}");
                return response;
            }
        }

        [Function("putFileValue")]
        public static async Task<HttpResponseData> PutFileValue(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "fileValues/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("FileValueController");
            logger.LogInformation("PUT -> updating FileValue {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<FileValueEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<FileValue>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                entity.Name = updated.name;
                entity.Description = updated.description;
                entity.Active = updated.active;

                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync<FileValueEntity>(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating FileValue {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error updating FileValue {rowKey}: {ex}");
                return response;
            }
        }

        [Function("deleteFileValue")]
        public static async Task<HttpResponseData> DeleteFileValue(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "fileValues/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("FileValueController");
            logger.LogInformation("DELETE -> removing FileValue {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<FileValueEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                await _table.DeleteEntityAsync(_partitionKey, rowKey, entity.ETag);

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DELETE -> error removing FileValue {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error removing FileValue {rowKey}: {ex}");
                return response;
            }
        }
    }
}
