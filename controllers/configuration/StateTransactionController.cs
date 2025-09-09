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
    public class StateTransactionController
    {
        private const string _partitionKey = "StateTransaction";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        // Utilidad para prefijo (RowKey starts with)
        private static string NextPrefix(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var last = value[^1];
            var next = (char)(last + 1);
            return value[..^1] + next;
        }

        [Function("getTransactionState")]
        public static async Task<HttpResponseData> GetTransactionState(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "statesTransaction/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("StateTransactionController");
            logger.LogInformation("GET -> getting StateTransaction by prefix {RowKey}", rowKey);

            try
            {
                var next = NextPrefix(rowKey);
                var filter =
                    $"PartitionKey eq '{_partitionKey}' and RowKey ge '{rowKey}' and RowKey lt '{next}'";

                var list = await _table.QueryToListAsync<StateTransactionEntity>(filter);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<IList<StateTransactionEntity>>(list);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting StateTransaction by prefix {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving state transactions: {ex}");
                return resp;
            }
        }

        [Function("getStateTransactions")]
        public static async Task<HttpResponseData> GetStateTransactions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "statesTransaction")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("StateTransactionController");
            logger.LogInformation("GET ALL -> getting all StateTransactions");

            try
            {
                var list = await _table.QueryToListAsync<StateTransactionEntity>(x => x.PartitionKey == _partitionKey);
                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync<IList<StateTransactionEntity>>(list);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error getting StateTransactions");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error retrieving state transactions: {ex}");
                return resp;
            }
        }

        [Function("postStateTransaction")]
        public static async Task<HttpResponseData> PostStateTransaction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "statesTransaction")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("StateTransactionController");
            logger.LogInformation("POST -> inserting StateTransaction");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<StateTransaction>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var id = Guid.NewGuid().ToString();
                var rowKey = $"{record.idInitialState}|{record.idFinalState}|-{id}";

                var entity = new StateTransactionEntity(_partitionKey, rowKey)
                {
                    IdStateTransaction = id,
                    IdInitialState = record.idInitialState,
                    IdFinalState = record.idFinalState,
                    IdOrganizationalUnit = record.idOrganizationalUnit,
                    IdSerie = record.idSerie,
                    IdSubserie = record.idSubserie,
                    Time = record.time,
                    Automatic = record.automatic,
                    Active = true
                };

                await _table.AddEntityAsync(entity);

                var resp = req.CreateResponse(HttpStatusCode.Created);
                await resp.WriteAsJsonAsync<StateTransactionEntity>(entity);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error inserting StateTransaction");
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error inserting state transaction: {ex}");
                return resp;
            }
        }

        [Function("putStateTransaction")]
        public static async Task<HttpResponseData> PutStateTransaction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "statesTransaction/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("StateTransactionController");
            logger.LogInformation("PUT -> updating StateTransaction {RowKey}", rowKey);

            try
            {
                var current = await _table.QueryFirstOrDefaultAsync<StateTransactionEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (current is null)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return nf;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<StateTransaction>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var mustMoveKey =
                    !string.Equals(updated.idInitialState, current.IdInitialState, StringComparison.Ordinal) ||
                    !string.Equals(updated.idFinalState, current.IdFinalState, StringComparison.Ordinal);

                if (mustMoveKey)
                {
                    // Nueva RowKey por cambio de estado inicial/final
                    var newRowKey = $"{updated.idInitialState}|{updated.idFinalState}|-{current.IdStateTransaction}";

                    var replacement = new StateTransactionEntity(_partitionKey, newRowKey)
                    {
                        IdStateTransaction = current.IdStateTransaction,
                        IdInitialState = updated.idInitialState,
                        IdFinalState = updated.idFinalState,
                        IdOrganizationalUnit = updated.idOrganizationalUnit,
                        IdSerie = updated.idSerie,
                        IdSubserie = updated.idSubserie,
                        Time = updated.time,
                        Automatic = updated.automatic,
                        Active = updated.active
                    };

                    await _table.AddEntityAsync(replacement);
                    await _table.DeleteEntityAsync(current.PartitionKey, current.RowKey, current.ETag);

                    var respMoved = req.CreateResponse(HttpStatusCode.OK);
                    await respMoved.WriteAsJsonAsync<StateTransactionEntity>(replacement);
                    return respMoved;
                }
                else
                {
                    // Solo actualizar campos
                    current.IdOrganizationalUnit = updated.idOrganizationalUnit;
                    current.IdSerie = updated.idSerie;
                    current.IdSubserie = updated.idSubserie;
                    current.Time = updated.time;
                    current.Automatic = updated.automatic;
                    current.Active = updated.active;

                    await _table.UpdateEntityAsync(current, current.ETag, TableUpdateMode.Merge);

                    var resp = req.CreateResponse(HttpStatusCode.OK);
                    await resp.WriteAsJsonAsync<StateTransactionEntity>(current);
                    return resp;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating StateTransaction {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error updating state transaction {rowKey}: {ex}");
                return resp;
            }
        }

        [Function("deleteStateTransaction")]
        public static async Task<HttpResponseData> DeleteStateTransaction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "statesTransaction/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("StateTransactionController");
            logger.LogInformation("DELETE -> removing StateTransaction {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<StateTransactionEntity>(
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
                logger.LogError(ex, "DELETE -> error removing StateTransaction {RowKey}", rowKey);
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync($"Error removing state transaction {rowKey}: {ex}");
                return resp;
            }
        }
    }
}
