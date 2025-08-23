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

namespace AppFunctions.controllers.configuration
{
    public class SubserieController
    {
        private const string _partitionKey = "Subserie";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        [Function("getSubserie")]
        public static async Task<HttpResponseData> GetSubserie(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "subseries/{rowKey}")] HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("SubserieController");
            logger.LogInformation("GET -> getting the subserie with RowKey prefix {RowKey}", rowKey);

            try
            {
                // Simular StartsWith sobre RowKey
                var last = rowKey[^1];
                var next = (char)((int)last + 1);
                var nextPrefix = rowKey[..^1] + next;

                var list = await _table.QueryToListAsync<SubserieEntity>(
                    x => x.PartitionKey == _partitionKey &&
                         x.RowKey.CompareTo(rowKey) >= 0 &&
                         x.RowKey.CompareTo(nextPrefix) < 0);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync<List<SubserieEntity>>(list);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting subseries by prefix {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving subseries for prefix {rowKey}: {ex}");
                return response;
            }
        }

        [Function("getSubseries")]
        public static async Task<HttpResponseData> GetSubseries(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "subseries")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("SubserieController");
            logger.LogInformation("GET ALL -> getting all subseries");

            try
            {
                var list = await _table.QueryToListAsync<SubserieEntity>(x => x.PartitionKey == _partitionKey);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync<List<SubserieEntity>>(list);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error getting subseries");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving subseries: {ex}");
                return response;
            }
        }

        [Function("postSubserie")]
        public static async Task<HttpResponseData> PostSubserie(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "subseries")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("SubserieController");
            logger.LogInformation("POST -> inserting subserie");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Subserie>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var idSubserie = Guid.NewGuid().ToString();
                var rowKey = $"{record.idOrganizationalUnit}|{record.idSerie}|-{idSubserie}";

                var entity = new SubserieEntity(_partitionKey, rowKey)
                {
                    IdSubserie = idSubserie,
                    IdOrganizationalUnit = record.idOrganizationalUnit,
                    IdSerie = record.idSerie,
                    Name = record.name,
                    Description = record.description,
                    IdsDisposicionFinal = string.Join("|", record.idsDisposicionFinal ?? Array.Empty<string>()),
                    IdsFileEvaluation = string.Join("|", record.idsFileEvaluation ?? Array.Empty<string>()),
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting subserie");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error inserting subserie: {ex}");
                return response;
            }
        }

        [Function("putSubserie")]
        public static async Task<HttpResponseData> PutSubserie(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "subseries/{rowKey}")] HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("SubserieController");
            logger.LogInformation("PUT -> updating subserie {RowKey}", rowKey);

            try
            {
                var current = await _table.QueryFirstOrDefaultAsync<SubserieEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (current is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<Subserie>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var orgChanged = !string.Equals(updated.idOrganizationalUnit, current.IdOrganizationalUnit, StringComparison.Ordinal);
                var serieChanged = !string.Equals(updated.idSerie, current.IdSerie, StringComparison.Ordinal);

                if (orgChanged || serieChanged)
                {
                    // Mover a nuevo RowKey
                    var newRowKey = $"{updated.idOrganizationalUnit}|{updated.idSerie}|-{current.IdSubserie}";
                    var replacement = new SubserieEntity(_partitionKey, newRowKey)
                    {
                        IdSubserie = current.IdSubserie,
                        IdOrganizationalUnit = updated.idOrganizationalUnit,
                        IdSerie = updated.idSerie,
                        Name = updated.name,
                        Description = updated.description,
                        IdsDisposicionFinal = string.Join("|", updated.idsDisposicionFinal ?? Array.Empty<string>()),
                        IdsFileEvaluation = string.Join("|", updated.idsFileEvaluation ?? Array.Empty<string>()),
                        Active = updated.active
                    };

                    await _table.AddEntityAsync(replacement);

                    try
                    {
                        await _table.DeleteEntityAsync(current.PartitionKey, current.RowKey, current.ETag);
                    }
                    catch (Exception delEx)
                    {
                        // rollback del insert
                        await _table.DeleteEntityAsync(replacement.PartitionKey, replacement.RowKey, ETag.All);
                        throw new InvalidOperationException($"Failed to delete old entity after insert: {delEx.Message}", delEx);
                    }

                    var responseMoved = req.CreateResponse(HttpStatusCode.OK);
                    await responseMoved.WriteAsJsonAsync(replacement);
                    return responseMoved;
                }
                else
                {
                    // Update in-place (merge)
                    current.Name = updated.name;
                    current.Description = updated.description;
                    current.IdsDisposicionFinal = string.Join("|", updated.idsDisposicionFinal ?? Array.Empty<string>());
                    current.IdsFileEvaluation = string.Join("|", updated.idsFileEvaluation ?? Array.Empty<string>());
                    current.Active = updated.active;

                    await _table.UpdateEntityAsync(current, current.ETag, TableUpdateMode.Merge);

                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(current);
                    return response;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating subserie {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error updating subserie {rowKey}: {ex}");
                return response;
            }
        }

        [Function("deleteSubserie")]
        public static async Task<HttpResponseData> DeleteSubserie(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "subseries/{rowKey}")] HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("SubserieController");
            logger.LogInformation("DELETE -> removing subserie {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<SubserieEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (entity is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                await _table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DELETE -> error removing subserie {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error removing subserie {rowKey}: {ex}");
                return response;
            }
        }
    }
}
