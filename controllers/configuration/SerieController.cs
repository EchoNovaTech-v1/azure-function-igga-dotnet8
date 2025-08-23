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
    public class SerieController
    {
        private const string _partitionKey = "Serie";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        // GET prefix (RowKey startsWith)
        [Function("getSerie")]
        public static async Task<HttpResponseData> GetSerie(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "series/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("SerieController");
            logger.LogInformation("GET -> getting Serie prefix {RowKey}", rowKey);

            try
            {
                if (string.IsNullOrEmpty(rowKey))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("rowKey is required.");
                    return bad;
                }

                // startsWith => RowKey >= prefix AND RowKey < nextPrefix
                char lastChar = rowKey[^1];
                char nextLast = (char)(lastChar + 1);
                string nextPrefix = rowKey.Substring(0, rowKey.Length - 1) + nextLast;

                string filter =
                    $"PartitionKey eq '{_partitionKey}' and RowKey ge '{rowKey}' and RowKey lt '{nextPrefix}'";

                var list = new List<SerieEntity>();
                await foreach (var e in _table.QueryAsync<SerieEntity>(filter))
                    list.Add(e);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<IList<SerieEntity>>(list);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting Serie prefix {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving series {rowKey}: {ex}");
                return resp;
            }
        }

        // GET all
        [Function("getSeries")]
        public static async Task<HttpResponseData> GetSeries(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "series")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("SerieController");
            logger.LogInformation("GET ALL -> getting all Series");

            try
            {
                var list = await _table.QueryToListAsync<SerieEntity>(x => x.PartitionKey == _partitionKey);
                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<IList<SerieEntity>>(list);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving series: {ex}");
                return resp;
            }
        }

        // POST
        [Function("postSerie")]
        public static async Task<HttpResponseData> PostSerie(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "series")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("SerieController");
            logger.LogInformation("POST -> inserting Serie");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Serie>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var guid = Guid.NewGuid().ToString();
                var rowKey = $"{record.idOrganizationalUnit}|{guid}";

                var entity = new SerieEntity(_partitionKey, rowKey)
                {
                    IdSerie = guid,
                    Name = record.name,
                    Description = record.description,
                    Keycode = record.keycode,
                    IdOrganizationalUnit = record.idOrganizationalUnit,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var resp = req.CreateResponse(HttpStatusCode.Created);
                await resp.WriteAsJsonAsync<SerieEntity>(entity);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting Serie");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error inserting serie: {ex}");
                return resp;
            }
        }

        // PUT (si cambia la unidad organizacional -> cambia RowKey)
        [Function("putSerie")]
        public static async Task<HttpResponseData> PutSerie(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "series/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("SerieController");
            logger.LogInformation("PUT -> updating Serie {RowKey}", rowKey);

            try
            {
                var current = await _table.QueryFirstOrDefaultAsync<SerieEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (current is null)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return nf;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<Serie>(body);
                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                // ¿cambió la unidad organizacional?
                if (!string.Equals(updated.idOrganizationalUnit, current.IdOrganizationalUnit, StringComparison.Ordinal))
                {
                    var idSerie = string.IsNullOrWhiteSpace(current.IdSerie) ? updated.idSerie : current.IdSerie;
                    var newRowKey = $"{updated.idOrganizationalUnit}|{idSerie}";

                    var newEntity = new SerieEntity(_partitionKey, newRowKey)
                    {
                        IdSerie = idSerie,
                        Name = updated.name,
                        Description = updated.description,
                        Keycode = updated.keycode,
                        IdOrganizationalUnit = updated.idOrganizationalUnit,
                        Active = updated.active
                    };

                    await _table.AddEntityAsync(newEntity);

                    try
                    {
                        await _table.DeleteEntityAsync(current.PartitionKey, current.RowKey, current.ETag);
                    }
                    catch
                    {
                        // rollback si falla el delete del viejo
                        await _table.DeleteEntityAsync(newEntity.PartitionKey, newEntity.RowKey);
                        throw;
                    }

                    var respMoved = req.CreateResponse(HttpStatusCode.OK);
                    await respMoved.WriteAsJsonAsync<SerieEntity>(newEntity);
                    return respMoved;
                }
                else
                {
                    // misma unidad -> Merge
                    current.Name = updated.name;
                    current.Description = updated.description;
                    current.Keycode = updated.keycode;
                    current.Active = updated.active;

                    await _table.UpdateEntityAsync(current, current.ETag, TableUpdateMode.Merge);

                    var resp = req.CreateResponse(HttpStatusCode.OK);
                    await resp.WriteAsJsonAsync<SerieEntity>(current);
                    return resp;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating Serie {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error updating serie {rowKey}: {ex}");
                return resp;
            }
        }

        // DELETE
        [Function("deleteSerie")]
        public static async Task<HttpResponseData> DeleteSerie(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "series/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("SerieController");
            logger.LogInformation("DELETE -> removing Serie {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<SerieEntity>(
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
                logger.LogError(ex, "DELETE -> error removing Serie {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error removing serie {rowKey}: {ex}");
                return resp;
            }
        }
    }
}
