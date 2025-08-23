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

namespace AppFunctions.controllers
{
    public class UserController
    {
        // Partición de trabajo
        private const string _partitionKey = "User";

        // Cliente de tabla (helper propio)
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableSecurity);

        static UserController()
        {
            try { _table.CreateIfNotExists(); } catch { /* no-op */ }
        }

        // GET /users/{rowKey}
        [Function("getUser")]
        public static async Task<HttpResponseData> GetUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "users/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("UserController");
            log.LogInformation("GET -> getting the user with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<UserEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET -> error to get the user {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the record the user {rowKey}: {ex}");
                return bad;
            }
        }

        // GET /users
        [Function("getUsers")]
        public static async Task<HttpResponseData> GetUsers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "users")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("UserController");
            log.LogInformation("GET ALL -> getting all the users.");

            try
            {
                var list = await _table.QueryToListAsync<UserEntity>(x => x.PartitionKey == _partitionKey);
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(list);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET ALL -> error during the process of getting users");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error when obtaining the records from the users list: {ex}");
                return bad;
            }
        }

        // POST /users
        [Function("postUser")]
        public static async Task<HttpResponseData> PostUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "users")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("UserController");
            log.LogInformation("POST -> inserting user.");

            var rowKey = Guid.NewGuid().ToString();

            try
            {
                var body = await req.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                User? record;
                try { record = JsonConvert.DeserializeObject<User>(body); }
                catch (JsonException)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid JSON payload.");
                    return bad;
                }

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var entity = new UserEntity(_partitionKey, rowKey)
                {
                    IdAuth = record.idAuth,
                    Name = record.name,
                    LastName = record.lastName,
                    JobTitle = record.jobTitle,
                    Email = record.email,
                    MobilePhone = record.mobilePhone,
                    AccessAccount = record.accessAccount,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var created = req.CreateResponse(HttpStatusCode.Created);
                await created.WriteAsJsonAsync(entity);
                return created;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "POST -> error during the process of inserting the user");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while inserting the record of the user list: {ex}");
                return bad;
            }
        }

        // PUT /users/{rowKey}
        [Function("putUser")]
        public static async Task<HttpResponseData> PutUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "users/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("UserController");
            log.LogInformation("PUT -> updating user with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<UserEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                User? updated;
                try { updated = JsonConvert.DeserializeObject<User>(body); }
                catch (JsonException)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid JSON payload.");
                    return bad;
                }

                if (updated is null)
                {
                    var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badBody.WriteStringAsync("Invalid body.");
                    return badBody;
                }

                entity.Name = updated.name;
                entity.LastName = updated.lastName;
                entity.JobTitle = updated.jobTitle;
                entity.Email = updated.email;
                entity.MobilePhone = updated.mobilePhone;
                entity.AccessAccount = updated.accessAccount;
                entity.Active = updated.active;

                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(entity);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "PUT -> error during the process of updating the user {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while updating the user {rowKey}: {ex}");
                return bad;
            }
        }

        // DELETE /users/{rowKey}
        [Function("deleteUser")]
        public static async Task<HttpResponseData> DeleteUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "users/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("UserController");
            log.LogInformation("DELETE -> removing the user with RowKey {RowKey}.", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<UserEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"no record found with RowKey {rowKey}" });
                    return notFound;
                }

                await _table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "DELETE -> error during the process of removing the user {RowKey}", rowKey);
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"There was the following error while removing the record from the user list: {ex}");
                return bad;
            }
        }
    }
}
