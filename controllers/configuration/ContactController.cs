using System.Net;
using Azure;
using Azure.Data.Tables;
using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using AppFunctions.models;
using Extensions.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AppFunctions.controllers.configuration
{
    public class ContactController
    {
        private const string _partitionKey = "Contact";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        // GET /api/contacts/{rowKey}
        [Function("getContact")]
        public static async Task<HttpResponseData> GetContact(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "contacts/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ContactController");
            logger.LogInformation("GET -> getting contact {RowKey}", rowKey);

            try
            {
                // Si usas Extensions.Query:
                var entity = await _table.QueryFirstOrDefaultAsync<ContactEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                var response = req.CreateResponse(entity is null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
                if (entity is null)
                    await response.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                else
                    await response.WriteAsJsonAsync(entity);

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting contact {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving contact {rowKey}: {ex}");
                return response;
            }
        }

        // GET /api/contacts
        [Function("getContacts")]
        public static async Task<HttpResponseData> GetContacts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "contacts")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("ContactController");
            logger.LogInformation("GET ALL -> getting all contacts");

            try
            {
                var list = new List<ContactEntity>();
                await foreach (var item in _table.QueryAsync<ContactEntity>(e => e.PartitionKey == _partitionKey))
                {
                    list.Add(item);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(list);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error getting contacts");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving contacts: {ex}");
                return response;
            }
        }

        // POST /api/contacts
        [Function("postContact")]
        public static async Task<HttpResponseData> PostContact(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "contacts")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("ContactController");
            logger.LogInformation("POST -> inserting contact");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Contact>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var rowKey = Guid.NewGuid().ToString();

                var entity = new ContactEntity(_partitionKey, rowKey)
                {
                    IdContact = rowKey,
                    Id = record.id,
                    Name = record.name,
                    Address = record.address,
                    Municipality = record.municipality,
                    Phone = record.phone,
                    Email = record.email,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting contact");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"There was an error while inserting the contact: {ex}");
                return response;
            }
        }

        // PUT /api/contacts/{rowKey}
        [Function("putContact")]
        public static async Task<HttpResponseData> PutContact(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "contacts/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ContactController");
            logger.LogInformation("PUT -> updating contact {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<ContactEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<AppFunctions.models.Contact>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                entity.Id = updated.id;
                entity.Name = updated.name;
                entity.Address = updated.address;
                entity.Municipality = updated.municipality;
                entity.Phone = updated.phone;
                entity.Email = updated.email;
                entity.Active = updated.active;

                try
                {
                    await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);
                }
                catch (RequestFailedException rfe) when (rfe.Status == 412)
                {
                    var pre = req.CreateResponse(HttpStatusCode.PreconditionFailed);
                    await pre.WriteStringAsync("Concurrency conflict. Reload and retry.");
                    return pre;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating contact {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"There was an error while updating the contact {rowKey}: {ex}");
                return response;
            }
        }

        // DELETE /api/contacts/{rowKey}
        [Function("deleteContact")]
        public static async Task<HttpResponseData> DeleteContact(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "contacts/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ContactController");
            logger.LogInformation("DELETE -> removing contact {RowKey}", rowKey);

            try
            {
                var res = await _table.GetEntityIfExistsAsync<ContactEntity>(_partitionKey, rowKey);
                if (!res.HasValue)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                await _table.DeleteEntityAsync(_partitionKey, rowKey, res.Value.ETag);

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DELETE -> error removing contact {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"There was an error while removing the contact {rowKey}: {ex}");
                return response;
            }
        }
    }
}
