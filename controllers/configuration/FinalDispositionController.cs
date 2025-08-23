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
    /// <summary>
    /// CRUD de FinalDisposition (tabla de configuraciones).
    /// </summary>
    public class FinalDispositionController
    {
        private const string _partitionKey = "FinalDisposition";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        [Function("getFinalDisposition")]
        public static async Task<HttpResponseData> GetFinalDisposition(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "finalDisposition/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("FinalDispositionController");
            logger.LogInformation("GET -> getting FinalDisposition {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<FinalDispositionEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                var response = req.CreateResponse(entity is null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
                if (entity is null)
                {
                    await response.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                }
                else
                {
                    await response.WriteAsJsonAsync<FinalDispositionEntity>(entity);
                }
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting FinalDisposition {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving FinalDisposition {rowKey}: {ex}");
                return response;
            }
        }

        [Function("getFinalDispositions")]
        public static async Task<HttpResponseData> GetFinalDispositions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "finalDisposition")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("FinalDispositionController");
            logger.LogInformation("GET ALL -> getting all FinalDispositions");

            try
            {
                var list = await _table.QueryToListAsync<FinalDispositionEntity>(x => x.PartitionKey == _partitionKey);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync<IList<FinalDispositionEntity>>(list);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error getting FinalDispositions");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving FinalDispositions: {ex}");
                return response;
            }
        }

        [Function("postFinalDisposition")]
        public static async Task<HttpResponseData> PostFinalDisposition(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "finalDisposition")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("FinalDispositionController");
            logger.LogInformation("POST -> inserting FinalDisposition");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<FinalDisposition>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var rowKey = Guid.NewGuid().ToString();

                var entity = new FinalDispositionEntity(_partitionKey, rowKey)
                {
                    Name = record.name,
                    Description = record.description,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync<FinalDispositionEntity>(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting FinalDisposition");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error inserting FinalDisposition: {ex}");
                return response;
            }
        }

        [Function("putFinalDisposition")]
        public static async Task<HttpResponseData> PutFinalDisposition(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "finalDisposition/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("FinalDispositionController");
            logger.LogInformation("PUT -> updating FinalDisposition {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<FinalDispositionEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<FinalDisposition>(body);

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
                await response.WriteAsJsonAsync<FinalDispositionEntity>(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating FinalDisposition {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error updating FinalDisposition {rowKey}: {ex}");
                return response;
            }
        }

        [Function("deleteFinalDisposition")]
        public static async Task<HttpResponseData> DeleteFinalDisposition(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "finalDisposition/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("FinalDispositionController");
            logger.LogInformation("DELETE -> removing FinalDisposition {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<FinalDispositionEntity>(
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
                logger.LogError(ex, "DELETE -> error removing FinalDisposition {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error removing FinalDisposition {rowKey}: {ex}");
                return response;
            }
        }
    }
}
