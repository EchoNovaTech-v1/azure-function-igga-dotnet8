using System.Collections.Generic;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using AppFunctions.models;
using AppFunctions.services;
using Azure.Data.Tables;
using Extensions.Config;
using Extensions.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AppFunctions.controllers.configuration
{
    /// <summary>
    /// CRUD y lógica de notificaciones por transacción.
    /// </summary>
    public class NotificationTransactionController
    {
        private const string _partitionKey = "NotificationTransaction";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        [Function("getNotificationTransaction")]
        public static async Task<HttpResponseData> GetNotificationTransaction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "notificationsTransaction/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("NotificationTransactionController");
            logger.LogInformation("GET -> getting notification transactions by RowKey prefix {RowKey}", rowKey);

            try
            {
                if (string.IsNullOrWhiteSpace(rowKey))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("rowKey is required.");
                    return bad;
                }

                // StartsWith emulado: RowKey >= rowKey AND RowKey < nextPrefix
                var next = NextPrefix(rowKey);
                var filter =
                    $"PartitionKey eq '{_partitionKey}' and RowKey ge '{Escape(rowKey)}' and RowKey lt '{Escape(next)}'";

                var list = await _table.QueryToListAsync<NotificationTransactionEntity>(filter);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync<IList<NotificationTransactionEntity>>(list);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error getting notifications by prefix {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving notifications: {ex}");
                return response;
            }
        }

        [Function("getNotificationTransactions")]
        public static async Task<HttpResponseData> GetNotificationTransactions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "notificationsTransaction")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("NotificationTransactionController");
            logger.LogInformation("GET ALL -> getting all NotificationTransactions");

            try
            {
                var list = await _table.QueryToListAsync<NotificationTransactionEntity>(
                    x => x.PartitionKey == _partitionKey);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync<IList<NotificationTransactionEntity>>(list);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving notifications: {ex}");
                return response;
            }
        }

        [Function("postNotificationTransaction")]
        public static async Task<HttpResponseData> PostNotificationTransaction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "notificationsTransaction")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("NotificationTransactionController");
            logger.LogInformation("POST -> inserting NotificationTransaction");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<NotificationTransaction>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var idNotificationTransaction = Guid.NewGuid().ToString();
                var rowKey = $"{record.idStateTransaction}|{idNotificationTransaction}";

                // Inactivar existentes para el mismo estado
                await ChangeNotificationInactive(record.idStateTransaction);

                var entity = new NotificationTransactionEntity(_partitionKey, rowKey)
                {
                    IdNotificationTransaction = idNotificationTransaction,
                    IdStateTransaction = record.idStateTransaction,
                    To = record.to,
                    CC = record.cc,
                    Subject = record.subject,
                    EventTime = record.eventTime,
                    Message = record.message,
                    Active = true
                };

                // Sincroniza con módulo de seguridad
                await ConfigModuleNotifications(entity);

                await _table.AddEntityAsync(entity);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync<NotificationTransactionEntity>(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error inserting notification transaction: {ex}");
                return response;
            }
        }

        [Function("putNotificationTransaction")]
        public static async Task<HttpResponseData> PutNotificationTransaction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "notificationsTransaction/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("NotificationTransactionController");
            logger.LogInformation("PUT -> updating NotificationTransaction {RowKey}", rowKey);

            try
            {
                var existing = await _table.QueryFirstOrDefaultAsync<NotificationTransactionEntity>(
                    x => x.PartitionKey == _partitionKey && x.RowKey == rowKey);

                if (existing is null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync<object>(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<NotificationTransaction>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var stateChanged = !string.Equals(updated.idStateTransaction, existing.IdStateTransaction, StringComparison.Ordinal);

                if (stateChanged)
                {
                    // Nueva RowKey al cambiar el estado
                    var newRowKey = $"{updated.idStateTransaction}|{existing.IdNotificationTransaction}";

                    await ChangeNotificationInactive(updated.idStateTransaction);

                    var replacement = new NotificationTransactionEntity(_partitionKey, newRowKey)
                    {
                        IdNotificationTransaction = existing.IdNotificationTransaction,
                        IdStateTransaction = updated.idStateTransaction,
                        To = updated.to,
                        CC = updated.cc,
                        Subject = updated.subject,
                        EventTime = updated.eventTime,
                        Message = updated.message,
                        Active = updated.active
                    };

                    // Insertar nuevo y eliminar viejo (move)
                    await _table.AddEntityAsync(replacement);

                    try
                    {
                        await _table.DeleteEntityAsync(existing.PartitionKey, existing.RowKey, existing.ETag);
                    }
                    catch
                    {
                        // rollback si falla la eliminación
                        await _table.DeleteEntityAsync(replacement.PartitionKey, replacement.RowKey);
                        throw;
                    }

                    var ok = req.CreateResponse(HttpStatusCode.OK);
                    await ok.WriteAsJsonAsync<NotificationTransactionEntity>(replacement);
                    return ok;
                }
                else
                {
                    // Solo actualizar campos
                    existing.To = updated.to;
                    existing.CC = updated.cc;
                    existing.Subject = updated.subject;
                    existing.EventTime = updated.eventTime;
                    existing.Message = updated.message;
                    existing.Active = updated.active;

                    await _table.UpdateEntityAsync(existing, existing.ETag, TableUpdateMode.Merge);

                    var ok = req.CreateResponse(HttpStatusCode.OK);
                    await ok.WriteAsJsonAsync<NotificationTransactionEntity>(existing);
                    return ok;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error updating NotificationTransaction {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error updating notification transaction {rowKey}: {ex}");
                return response;
            }
        }

        [Function("deleteNotificationTransaction")]
        public static async Task<HttpResponseData> DeleteNotificationTransaction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "notificationsTransaction/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("NotificationTransactionController");
            logger.LogInformation("DELETE -> removing NotificationTransaction {RowKey}", rowKey);

            try
            {
                var entity = await _table.QueryFirstOrDefaultAsync<NotificationTransactionEntity>(
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
                logger.LogError(ex, "DELETE -> error removing {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error removing notification transaction {rowKey}: {ex}");
                return response;
            }
        }

        // ===== Helpers de negocio / integración =====

        // Inactivar todas las notificaciones activas para un estado dado
        private static async Task ChangeNotificationInactive(string idStateTransaction)
        {
            var filter =
                $"PartitionKey eq '{_partitionKey}' and IdStateTransaction eq '{Escape(idStateTransaction)}' and Active eq true";
            var list = await _table.QueryToListAsync<NotificationTransactionEntity>(filter);

            foreach (var nt in list)
            {
                nt.Active = false;
                await _table.UpdateEntityAsync(nt, nt.ETag, TableUpdateMode.Merge);
            }
        }

        // Orquestación con el módulo de seguridad
        private static async Task ConfigModuleNotifications(NotificationTransactionEntity notification)
        {
            await CreateTemplate(notification);
            await CreateConfigNotifications(notification);
            await CreateTemplateMaps(notification);
        }

        private static async Task CreateTemplate(NotificationTransactionEntity notification)
        {
            var template = new Template
            {
                idTemplate = notification.IdNotificationTransaction,
                subject = notification.Subject,
                htmlBody = notification.Message
            };
            await GeneralService.CreateTemplateAsync(template);
        }

        private static async Task CreateConfigNotifications(NotificationTransactionEntity notification)
        {
            var configNotifications = new ConfigNotifications
            {
                idTemplate = notification.IdNotificationTransaction,
                domainEmail = Constants.DOMAIN_SENDER.Get(),
                emailSender = Constants.EMAIL_SENDER.Get(),
                passwordSender = Constants.PASS_SENDER.Get(),
                frecuencyNotification = 1
            };
            await GeneralService.CreateConfigNotificationsAsync(configNotifications);
        }

        private static async Task CreateTemplateMaps(NotificationTransactionEntity notification)
        {
            var input = (notification.Message ?? string.Empty) + (notification.Subject ?? string.Empty);

            foreach (Match match in Regex.Matches(input, @"\{(.*?)\}|\[(.*?)\]|\((.*?)\)"))
            {
                var name = match.Value.Substring(1, match.Value.Length - 2);
                var openChar = match.Value.Substring(0, 1);
                var closeChar = match.Value.Substring(match.Value.Length - 1, 1);

                var variable = await _table.QueryFirstOrDefaultAsync<NotificationVariableEntity>(
                    x => x.PartitionKey == "NotificationVariable"
                      && x.Name == name
                      && x.OpenChar == openChar
                      && x.CloseChar == closeChar
                      && x.Active == true);

                if (variable is not null)
                {
                    var templateMap = new TemplateMap
                    {
                        idTemplate = notification.IdNotificationTransaction,
                        origin = name,
                        destination = name
                    };
                    await GeneralService.CreateTemplateMapAsync(templateMap);
                }
            }
        }

        // ===== Utilidades =====

        private static string NextPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return "\uFFFF";
            var last = prefix[^1];
            var next = (char)(last + 1);
            return prefix[..^1] + next;
        }

        private static string Escape(string s) => (s ?? string.Empty).Replace("'", "''");
    }
}
