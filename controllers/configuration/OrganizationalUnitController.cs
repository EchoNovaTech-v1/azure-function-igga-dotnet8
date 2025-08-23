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
    public class OrganizationalUnitController
    {
        private const string _partitionKey = "OrganizationalUnit";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        // GET prefix (RowKey startsWith)
        [Function("getOrganizationalUnit")]
        public static async Task<HttpResponseData> GetOrganizationalUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "organizationalUnits/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("OrganizationalUnitController");
            logger.LogInformation("GET -> getting OrganizationalUnit prefix {RowKey}", rowKey);

            try
            {
                if (string.IsNullOrEmpty(rowKey))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("rowKey is required.");
                    return bad;
                }

                // startsWith con RowKey:  RowKey >= prefix AND RowKey < nextPrefix
                char lastChar = rowKey[^1];
                char nextLastChar = (char)(lastChar + 1);
                string nextPrefix = rowKey.Substring(0, rowKey.Length - 1) + nextLastChar;

                string filter =
                    $"PartitionKey eq '{_partitionKey}' and RowKey ge '{rowKey}' and RowKey lt '{nextPrefix}'";

                var list = new List<OrganizationalUnitEntity>();
                await foreach (var e in _table.QueryAsync<OrganizationalUnitEntity>(filter))
                    list.Add(e);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<IList<OrganizationalUnitEntity>>(list);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting OrganizationalUnit prefix {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving organizational unit(s) {rowKey}: {ex}");
                return resp;
            }
        }

        // GET all
        [Function("getOrganizationalUnits")]
        public static async Task<HttpResponseData> GetOrganizationalUnits(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "organizationalUnits")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("OrganizationalUnitController");
            logger.LogInformation("GET ALL -> getting all OrganizationalUnits");

            try
            {
                var list = await _table.QueryToListAsync<OrganizationalUnitEntity>(x => x.PartitionKey == _partitionKey);
                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<IList<OrganizationalUnitEntity>>(list);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving organizational units: {ex}");
                return resp;
            }
        }

        // POST
        [Function("postOrganizationalUnit")]
        public static async Task<HttpResponseData> PostOrganizationalUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "organizationalUnits")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("OrganizationalUnitController");
            logger.LogInformation("POST -> inserting OrganizationalUnit");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<OrganizationalUnit>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var idOrganizationalUnit = Guid.NewGuid().ToString();
                var rowKey = $"{record.idParent}|{idOrganizationalUnit}";

                var entity = new OrganizationalUnitEntity(_partitionKey, rowKey)
                {
                    IdOrganizationalUnit = idOrganizationalUnit,
                    Name = record.name,
                    Level = record.level,
                    IdOrganizationType = record.idOrganizationType,
                    IdParent = record.idParent,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var resp = req.CreateResponse(HttpStatusCode.Created);
                await resp.WriteAsJsonAsync<OrganizationalUnitEntity>(entity);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting OrganizationalUnit");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error inserting organizational unit: {ex}");
                return resp;
            }
        }

        // PUT (maneja cambio de padre -> cambia RowKey)
        [Function("putOrganizationalUnit")]
        public static async Task<HttpResponseData> PutOrganizationalUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "organizationalUnits/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("OrganizationalUnitController");
            logger.LogInformation("PUT -> updating OrganizationalUnit {RowKey}", rowKey);

            try
            {
                var current = await _table.QueryFirstOrDefaultAsync<OrganizationalUnitEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (current is null)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return nf;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<OrganizationalUnit>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                // ¿cambió el padre?
                if (!string.Equals(updated.idParent, current.IdParent, StringComparison.Ordinal))
                {
                    var idOrg = string.IsNullOrWhiteSpace(current.IdOrganizationalUnit)
                                ? updated.idOrganizationalUnit
                                : current.IdOrganizationalUnit;

                    var newRowKey = $"{updated.idParent}|{idOrg}";

                    var newEntity = new OrganizationalUnitEntity(_partitionKey, newRowKey)
                    {
                        IdOrganizationalUnit = idOrg,
                        Name = updated.name,
                        Level = updated.level,
                        IdOrganizationType = updated.idOrganizationType,
                        IdParent = updated.idParent,
                        Active = updated.active
                    };

                    await _table.AddEntityAsync(newEntity);

                    try
                    {
                        await _table.DeleteEntityAsync(current.PartitionKey, current.RowKey, current.ETag);
                    }
                    catch
                    {
                        // rollback del insert si el delete falla
                        await _table.DeleteEntityAsync(newEntity.PartitionKey, newEntity.RowKey);
                        throw;
                    }

                    var respMoved = req.CreateResponse(HttpStatusCode.OK);
                    await respMoved.WriteAsJsonAsync<OrganizationalUnitEntity>(newEntity);
                    return respMoved;
                }
                else
                {
                    // mismo padre: merge sobre el registro actual
                    current.Name = updated.name;
                    current.Level = updated.level;
                    current.IdOrganizationType = updated.idOrganizationType;
                    current.Active = updated.active;

                    await _table.UpdateEntityAsync(current, current.ETag, TableUpdateMode.Merge);

                    var resp = req.CreateResponse(HttpStatusCode.OK);
                    await resp.WriteAsJsonAsync<OrganizationalUnitEntity>(current);
                    return resp;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating OrganizationalUnit {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error updating organizational unit {rowKey}: {ex}");
                return resp;
            }
        }

        // DELETE
        [Function("deleteOrganizationalUnit")]
        public static async Task<HttpResponseData> DeleteOrganizationalUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "organizationalUnits/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("OrganizationalUnitController");
            logger.LogInformation("DELETE -> removing OrganizationalUnit {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<OrganizationalUnitEntity>(
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
                logger.LogError(ex, "DELETE -> error removing OrganizationalUnit {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error removing organizational unit {rowKey}: {ex}");
                return resp;
            }
        }
    }
}
