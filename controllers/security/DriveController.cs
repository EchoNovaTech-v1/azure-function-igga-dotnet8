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

namespace AppFunctions.controllers.security
{
    public class DriveController
    {
        // Partición de trabajo
        private const string _partitionKey = "Drive";

        // Cliente de tabla (usa tu helper)
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableSecurity);

        // GET /drives/{rowKey}
        [Function("getDrive")]
        public static async Task<HttpResponseData> GetDrive(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "drives/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DriveController");
            log.LogInformation("GET -> getting the drive with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<DriveEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<DriveEntity>(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET -> error to get the drive {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the record the drive {rowKey}: {ex}");
                return bad;
            }
        }

        // GET /drives
        [Function("getDrives")]
        public static async Task<HttpResponseData> GetDrives(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "drives")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DriveController");
            log.LogInformation("GET ALL -> getting all drives.");

            try
            {
                var list = await _table.QueryToListAsync<DriveEntity>(x => x.PartitionKey == _partitionKey);
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<List<DriveEntity>>(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET ALL -> error during the process of getting drives");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the records from the drives list: {ex}");
                return bad;
            }
        }

        // POST /drives
        [Function("postDrive")]
        public static async Task<HttpResponseData> PostDrive(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "drives")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DriveController");
            log.LogInformation("POST -> inserting drive.");

            var rowKey = Guid.NewGuid().ToString();

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Drive>(body);

                if (record is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                var entity = new DriveEntity(_partitionKey, rowKey)
                {
                    url = record.url,
                    user = record.user,
                    password = record.password,
                    active = true
                };

                await _table.AddEntityAsync(entity);

                var created = req.CreateResponse(HttpStatusCode.Created);
                await created.WriteAsJsonAsync<DriveEntity>(entity);
                return created;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "POST -> error during the process of inserting the drive");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while inserting the record of the drives list: {ex}");
                return bad;
            }
        }

        // PUT /drives/{rowKey}
        [Function("putDrive")]
        public static async Task<HttpResponseData> PutDrive(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "drives/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DriveController");
            log.LogInformation("PUT -> updating drive with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<DriveEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<Drive>(body);

                if (updated is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                entity.user = updated.user;
                entity.password = updated.password;
                entity.url = updated.url;
                entity.active = updated.active;

                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<DriveEntity>(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "PUT -> error during the process of updating the drive {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while updating the drive {rowKey}: {ex}");
                return bad;
            }
        }

        // DELETE /drives/{rowKey}
        [Function("deleteDrive")]
        public static async Task<HttpResponseData> DeleteDrive(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "drives/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DriveController");
            log.LogInformation("DELETE -> removing the drive with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<DriveEntity>(
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
                log.LogError(ex, "DELETE -> error during the process of removing the drive {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while removing the record from the drives list: {ex}");
                return bad;
            }
        }
    }
}
