using System.Net;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using AppFunctions.models;
using Extensions.Query;
using System.Threading.Tasks;
using System;

namespace AppFunctions.controllers.configuration
{
    /// <summary>
    /// CRUD para estados de archivo (FileState) en Table Storage.
    /// </summary>
    public class FileStateController
    {
        private const string _partitionKey = "FileState";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        [Function("getFileStates")]
        public static async Task<HttpResponseData> GetFileStates(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "fileStates")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("FileStateController");
            logger.LogInformation("GET ALL -> getting all the fileStates.");

            try
            {
                var list = await _table.QueryToListAsync<FileStateEntity>(x => x.PartitionKey == _partitionKey);
                var res = req.CreateResponse(HttpStatusCode.OK);
                await res.WriteAsJsonAsync(list);
                return res;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error during the process of getting fileStates");
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"There was an error obtaining the records from the fileStates list: {ex}");
                return res;
            }
        }

        [Function("getFileState")]
        public static async Task<HttpResponseData> GetFileState(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "fileStates/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("FileStateController");
            logger.LogInformation("GET ID -> getting the fileState with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<FileStateEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                var res = req.CreateResponse(entity is null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
                await res.WriteAsJsonAsync<object>(entity is null
                    ? new { message = $"No record found with RowKey {rowKey}" }
                    : entity);
                return res;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error to get the fileState {RowKey}", rowKey);
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"Error retrieving the fileState {rowKey}: {ex}");
                return res;
            }
        }

        [Function("postFileState")]
        public static async Task<HttpResponseData> PostFileState(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "fileStates")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("FileStateController");
            logger.LogInformation("POST -> inserting fileState.");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<FileState>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var rowKey = Guid.NewGuid().ToString();

                var entity = new FileStateEntity(_partitionKey, rowKey)
                {
                    Name = record.name,
                    Description = record.description,
                    Terminal = record.terminal,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var res = req.CreateResponse(HttpStatusCode.Created);
                await res.WriteAsJsonAsync(entity);
                return res;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error during the process of inserting the fileState");
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"There was an error while inserting the record in the fileState list: {ex}");
                return res;
            }
        }

        [Function("putFileState")]
        public static async Task<HttpResponseData> PutFileState(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "fileStates/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("FileStateController");
            logger.LogInformation("PUT -> updating fileState with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<FileStateEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<FileState>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                entity.Name = updated.name;
                entity.Description = updated.description;
                entity.Terminal = updated.terminal;
                entity.Active = updated.active;

                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

                var res = req.CreateResponse(HttpStatusCode.OK);
                await res.WriteAsJsonAsync(entity);
                return res;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error during the process of updating the fileState {RowKey}", rowKey);
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"There was an error while updating the fileState {rowKey}: {ex}");
                return res;
            }
        }

        [Function("deleteFileState")]
        public static async Task<HttpResponseData> DeleteFileState(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "fileStates/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("FileStateController");
            logger.LogInformation("DELETE -> removing the fileState with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<FileStateEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                await _table.DeleteEntityAsync(_partitionKey, rowKey, entity.ETag);

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DELETE -> error during the process of removing the fileState {RowKey}", rowKey);
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"There was an error while removing the record from the fileStates list: {ex}");
                return res;
            }
        }
    }
}
