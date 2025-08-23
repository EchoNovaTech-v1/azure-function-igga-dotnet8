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
    public class OrganizationalTypeController
    {
        private const string _partitionKey = "OrganizationalType";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        [Function("getOrganizationalType")]
        public static async Task<HttpResponseData> GetOrganizationalType(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "organizationalTypes/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("OrganizationalTypeController");
            logger.LogInformation("GET -> getting OrganizationalType {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<OrganizationalTypeEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                var resp = req.CreateResponse(entity is null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
                if (entity is null)
                    await resp.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                else
                    await resp.WriteAsJsonAsync<OrganizationalTypeEntity>(entity);

                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting OrganizationalType {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving OrganizationalType {rowKey}: {ex}");
                return resp;
            }
        }

        [Function("getOrganizationalTypes")]
        public static async Task<HttpResponseData> GetOrganizationalTypes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "organizationalTypes")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("OrganizationalTypeController");
            logger.LogInformation("GET ALL -> getting all OrganizationalTypes");

            try
            {
                var list = await _table.QueryToListAsync<OrganizationalTypeEntity>(x => x.PartitionKey == _partitionKey);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<IList<OrganizationalTypeEntity>>(list);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving OrganizationalTypes: {ex}");
                return resp;
            }
        }

        [Function("postOrganizationalType")]
        public static async Task<HttpResponseData> PostOrganizationalType(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "organizationalTypes")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("OrganizationalTypeController");
            logger.LogInformation("POST -> inserting OrganizationalType");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<OrganizationalType>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var rowKey = Guid.NewGuid().ToString();

                var entity = new OrganizationalTypeEntity(_partitionKey, rowKey)
                {
                    Name = record.name,
                    Description = record.description,
                    Keycode = record.keycode,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var resp = req.CreateResponse(HttpStatusCode.Created);
                await resp.WriteAsJsonAsync<OrganizationalTypeEntity>(entity);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting OrganizationalType");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error inserting OrganizationalType: {ex}");
                return resp;
            }
        }

        [Function("putOrganizationalType")]
        public static async Task<HttpResponseData> PutOrganizationalType(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "organizationalTypes/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("OrganizationalTypeController");
            logger.LogInformation("PUT -> updating OrganizationalType {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<OrganizationalTypeEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return nf;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<OrganizationalType>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                entity.Name = updated.name;
                entity.Description = updated.description;
                entity.Keycode = updated.keycode;
                entity.Active = updated.active;

                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<OrganizationalTypeEntity>(entity);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating OrganizationalType {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error updating OrganizationalType {rowKey}: {ex}");
                return resp;
            }
        }

        [Function("deleteOrganizationalType")]
        public static async Task<HttpResponseData> DeleteOrganizationalType(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "organizationalTypes/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("OrganizationalTypeController");
            logger.LogInformation("DELETE -> removing OrganizationalType {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<OrganizationalTypeEntity>(
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
                logger.LogError(ex, "DELETE -> error removing OrganizationalType {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error removing OrganizationalType {rowKey}: {ex}");
                return resp;
            }
        }
    }
}
