using System.Net;
using Azure;
using Azure.Data.Tables;
using AppFunctions.Common;
using AppFunctions.entities;
using AppFunctions.models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AppFunctions.controllers
{
    public static class AddressController
    {
        // Partición
        private const string _partitionKey = "Address";

        // Obtiene un TableClient para la tabla de configuraciones
        private static TableClient GetTable()
        {
            var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            return new TableClient(conn, Constants.tableConfigurations);
        }

        // GET /address/{rowKey}
        [Function("getAddress")]
        public static async Task<HttpResponseData> GetAddress(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "address/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("AddressController");
            log.LogInformation("GET -> getting the address with RowKey {RowKey}.", rowKey);

            try
            {
                var table = GetTable();
                try
                {
                    var response = await table.GetEntityAsync<AddressEntity>(_partitionKey, rowKey);
                    var ok = req.CreateResponse(HttpStatusCode.OK);
                    await ok.WriteAsJsonAsync<AddressEntity>(response.Value);
                    return ok;
                }
                catch (RequestFailedException rfe) when (rfe.Status == 404)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync<object>(new { message = $"no record found with RowKey {rowKey}" });
                    return nf;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET -> error to get the address {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the record the address {rowKey}: {ex}");
                return bad;
            }
        }

        // GET /address
        [Function("getAddresses")]
        public static async Task<HttpResponseData> GetAddresses(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "address")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("AddressController");
            log.LogInformation("GET ALL -> getting all the address.");

            try
            {
                var table = GetTable();
                var results = table.Query<AddressEntity>(x => x.PartitionKey == _partitionKey).ToList();

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<List<AddressEntity>>(results);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET ALL -> error during the process of getting addresses");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the records from the address list: {ex}");
                return bad;
            }
        }

        // POST /address
        [Function("postAddress")]
        public static async Task<HttpResponseData> PostAddress(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "address")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("AddressController");
            log.LogInformation("POST -> inserting address.");

            var rowKey = Guid.NewGuid().ToString();

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Address>(body);

                if (record is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                var table = GetTable();
                await table.CreateIfNotExistsAsync();

                var entity = new AddressEntity(_partitionKey, rowKey)
                {
                    Code = record.code,
                    Name = record.name,
                    Active = true
                };

                await table.AddEntityAsync(entity);

                var created = req.CreateResponse(HttpStatusCode.Created);
                await created.WriteAsJsonAsync<AddressEntity>(entity);
                return created;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "POST -> error during the process of inserting the address");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while inserting the record of the address list: {ex}");
                return bad;
            }
        }

        // PUT /address/{rowKey}
        [Function("putAddress")]
        public static async Task<HttpResponseData> PutAddress(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "address/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("AddressController");
            log.LogInformation("PUT -> updating address with RowKey {RowKey}.", rowKey);

            try
            {
                var table = GetTable();

                AddressEntity entity;
                try
                {
                    entity = (await table.GetEntityAsync<AddressEntity>(_partitionKey, rowKey)).Value;
                }
                catch (RequestFailedException rfe) when (rfe.Status == 404)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync<object>(new { message = $"no record found with RowKey {rowKey}" });
                    return nf;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<Address>(body);

                if (updated is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                entity.Code = updated.code;
                entity.Name = updated.name;
                entity.Active = updated.active;

                await table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<AddressEntity>(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "PUT -> error during the process of updating the address {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while updating the address {rowKey}: {ex}");
                return bad;
            }
        }

        // DELETE /address/{rowKey}
        [Function("deleteAddress")]
        public static async Task<HttpResponseData> DeleteAddress(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "address/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("AddressController");
            log.LogInformation("DELETE -> removing the address with RowKey {RowKey}.", rowKey);

            try
            {
                var table = GetTable();

                // Verifica existencia antes de borrar para responder 404 de forma limpia
                try
                {
                    var existing = await table.GetEntityAsync<AddressEntity>(_partitionKey, rowKey);
                    await table.DeleteEntityAsync(existing.Value.PartitionKey, existing.Value.RowKey, existing.Value.ETag);
                }
                catch (RequestFailedException rfe) when (rfe.Status == 404)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync<object>(new { message = $"no record found with RowKey {rowKey}" });
                    return nf;
                }

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "DELETE -> error during the process of removing the address {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while removing the record from the address list: {ex}");
                return bad;
            }
        }
    }
}
