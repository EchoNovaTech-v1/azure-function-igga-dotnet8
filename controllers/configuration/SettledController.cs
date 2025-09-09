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
    /// CRUD de la entidad Settled (radicados).
    /// </summary>
    public class SettledController
    {
        private const string _partitionKey = "Settled";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        // GET by RowKey
        [Function("getSettled")]
        public static async Task<HttpResponseData> GetSettled(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "settleds/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("SettledController");
            logger.LogInformation("GET -> getting Settled {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<SettledEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                var resp = req.CreateResponse(entity is null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
                if (entity is null)
                    await resp.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                else
                    await resp.WriteAsJsonAsync<SettledEntity>(entity);

                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting Settled {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving Settled {rowKey}: {ex}");
                return resp;
            }
        }

        // GET all
        [Function("getSettleds")]
        public static async Task<HttpResponseData> GetSettleds(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "settleds")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("SettledController");
            logger.LogInformation("GET ALL -> getting all Settleds");

            try
            {
                var list = await _table.QueryToListAsync<SettledEntity>(x => x.PartitionKey == _partitionKey);
                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<IList<SettledEntity>>(list);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error getting Settleds");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving Settleds: {ex}");
                return resp;
            }
        }

        // POST
        [Function("postSettled")]
        public static async Task<HttpResponseData> PostSettled(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "settleds")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("SettledController");
            logger.LogInformation("POST -> inserting Settled");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Settled>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var rowKey = Guid.NewGuid().ToString();

                var entity = new SettledEntity(_partitionKey, rowKey)
                {
                    consecutive = record.consecutive,
                    day = record.day,
                    idWorkflow = record.idWorkflow,
                    month = record.month,
                    year = record.year
                };

                await _table.AddEntityAsync(entity);

                var resp = req.CreateResponse(HttpStatusCode.Created);
                await resp.WriteAsJsonAsync<SettledEntity>(entity);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting Settled");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error inserting Settled: {ex}");
                return resp;
            }
        }

        // PUT
        [Function("putSettled")]
        public static async Task<HttpResponseData> PutSettled(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "settleds/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("SettledController");
            logger.LogInformation("PUT -> updating Settled {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<SettledEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return nf;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<Settled>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                entity.consecutive = updated.consecutive;
                entity.day = updated.day;
                entity.idWorkflow = updated.idWorkflow;
                entity.month = updated.month;
                entity.year = updated.year;

                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<SettledEntity>(entity);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating Settled {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error updating Settled {rowKey}: {ex}");
                return resp;
            }
        }

        // DELETE
        [Function("deleteSettled")]
        public static async Task<HttpResponseData> DeleteSettled(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "settleds/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("SettledController");
            logger.LogInformation("DELETE -> removing Settled {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<SettledEntity>(
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
                logger.LogError(ex, "DELETE -> error removing Settled {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error removing Settled {rowKey}: {ex}");
                return resp;
            }
        }
    }
}
