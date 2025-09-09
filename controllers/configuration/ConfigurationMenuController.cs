using System.Net;
using Azure;
using Azure.Data.Tables;
using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using AppFunctions.models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace AppFunctions.controllers.configuration
{
    /// <summary>
    /// CRUD de configuración de Menú (partición "Menu") en .NET Isolated + Azure.Data.Tables.
    /// </summary>
    public class ConfigurationMenuController
    {
        private const string _partitionKey = "Menu";
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfigurations);

        // GET /api/configMenu/{rowKey}
        // Devuelve todas las filas cuyo RowKey empieza con el prefijo {rowKey}.
        [Function("getConfigurationMenu")]
        public static async Task<HttpResponseData> GetConfigurationMenu(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "configMenu/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigurationMenuController");
            logger.LogInformation("GET -> getting Configuration of Menu by RowKey prefix {RowKey}", rowKey);

            try
            {
                if (string.IsNullOrEmpty(rowKey))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("rowKey prefix is required.");
                    return bad;
                }

                var nextPrefix = NextPrefix(rowKey);
                var list = new List<ConfigurationMenuEntity>();

                // Filtro tipo "startswith": PartitionKey eq 'Menu' and RowKey ge 'X' and RowKey lt 'X_next'
                var filter = $"PartitionKey eq '{_partitionKey}' and RowKey ge '{rowKey}' and RowKey lt '{nextPrefix}'";
                await foreach (var item in _table.QueryAsync<ConfigurationMenuEntity>(filter: filter))
                {
                    list.Add(item);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(list);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET -> error to get the Configuration of Menu {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving the Configuration of Menu {rowKey}: {ex}");
                return response;
            }
        }

        // GET /api/configMenu
        [Function("getConfigurationMenus")]
        public static async Task<HttpResponseData> GetConfigurationMenus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "configMenu")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigurationMenuController");
            logger.LogInformation("GET ALL -> getting all the Configuration of Menus.");

            try
            {
                var list = new List<ConfigurationMenuEntity>();
                await foreach (var item in _table.QueryAsync<ConfigurationMenuEntity>(e => e.PartitionKey == _partitionKey))
                {
                    list.Add(item);
                }

                // Orden igual que en la versión original
                var ordered = list
                    .OrderBy(x => x.Level)
                    .ThenBy(x => x.NumOrder)
                    .ToList();

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(ordered);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET ALL -> error during the process of getting Configuration of Menu");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"There was an error when obtaining the records: {ex}");
                return response;
            }
        }

        // POST /api/configMenu
        [Function("postConfigurationMenu")]
        public static async Task<HttpResponseData> PostConfigurationMenu(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "configMenu")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigurationMenuController");
            logger.LogInformation("POST -> inserting Configuration of Menu.");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<ConfigurationMenu>(body);

                if (record is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var guid = Guid.NewGuid().ToString();
                string rowKey;

                if (string.IsNullOrEmpty(record.idParent))
                {
                    // Nodo raíz: RowKey = {guid}|{guid}
                    rowKey = $"{guid}|{guid}";
                }
                else
                {
                    // Nodo con padre: RowKey = {idParent}|{guid}
                    rowKey = $"{record.idParent}|{guid}";
                }

                var entity = new ConfigurationMenuEntity(_partitionKey, rowKey)
                {
                    IdMenu = guid,
                    IdParent = record.idParent,
                    Name = record.name,
                    Route = record.route,
                    Type = record.type,
                    Icon = record.icon,
                    Defect = record.defect,
                    Level = record.level,
                    DragAndDrog = record.dragAndDrog,
                    Active = true,
                    NumOrder = record.numOrder
                };

                await _table.AddEntityAsync(entity);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST -> error during the process of inserting the Configuration of Menu");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error inserting Configuration of Menu: {ex}");
                return response;
            }
        }

        // PUT /api/configMenu/{rowKey}
        // Si cambia el padre, se "mueve" la fila: inserta nueva con nueva RowKey y luego elimina la anterior.
        [Function("putConfigurationMenu")]
        public static async Task<HttpResponseData> PutConfigurationMenu(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "configMenu/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigurationMenuController");
            logger.LogInformation("PUT -> updating Configuration of Menu {RowKey}", rowKey);

            try
            {
                var current = await _table.GetEntityIfExistsAsync<ConfigurationMenuEntity>(_partitionKey, rowKey);
                if (!current.HasValue)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new { message = $"No record found with RowKey {rowKey}" });
                    return notFound;
                }

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<ConfigurationMenu>(body);

                if (updated is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid body.");
                    return bad;
                }

                var oldEntity = current.Value;

                // ¿Cambió el padre?
                var parentChanged = (updated.idParent ?? string.Empty) != (oldEntity.IdParent ?? string.Empty);

                if (parentChanged)
                {
                    // Nueva RowKey en función del nuevo padre
                    var idMenu = oldEntity.IdMenu;
                    var newRowKey = string.IsNullOrEmpty(updated.idParent)
                        ? $"{idMenu}|{idMenu}"
                        : $"{updated.idParent}|{idMenu}";

                    var newEntity = new ConfigurationMenuEntity(_partitionKey, newRowKey)
                    {
                        IdMenu = idMenu,
                        IdParent = updated.idParent,
                        Name = updated.name,
                        Route = updated.route,
                        Type = updated.type,
                        Icon = updated.icon,
                        Defect = updated.defect,
                        Level = updated.level,
                        DragAndDrog = updated.dragAndDrog,
                        NumOrder = updated.numOrder,
                        Active = updated.active
                    };

                    // Inserta la nueva
                    await _table.AddEntityAsync(newEntity);

                    try
                    {
                        // Elimina la vieja (controlando ETag)
                        await _table.DeleteEntityAsync(_partitionKey, rowKey, oldEntity.ETag);
                    }
                    catch (RequestFailedException delEx)
                    {
                        // Si falla la eliminación, revertimos la inserción para no dejar duplicado
                        try { await _table.DeleteEntityAsync(_partitionKey, newRowKey); } catch { /* best-effort */ }
                        logger.LogError(delEx, "PUT -> error deleting old entity after insert of new entity");
                        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                        await bad.WriteStringAsync($"Error moving menu node (parent change): {delEx}");
                        return bad;
                    }

                    var ok = req.CreateResponse(HttpStatusCode.OK);
                    await ok.WriteAsJsonAsync(newEntity);
                    return ok;
                }
                else
                {
                    // Mismo padre -> solo Merge
                    oldEntity.IdParent = updated.idParent;
                    oldEntity.Name = updated.name;
                    oldEntity.Route = updated.route;
                    oldEntity.Type = updated.type;
                    oldEntity.Icon = updated.icon;
                    oldEntity.Defect = updated.defect;
                    oldEntity.Level = updated.level;
                    oldEntity.DragAndDrog = updated.dragAndDrog;
                    oldEntity.Active = updated.active;
                    oldEntity.NumOrder = updated.numOrder;

                    try
                    {
                        await _table.UpdateEntityAsync(oldEntity, oldEntity.ETag, TableUpdateMode.Merge);
                    }
                    catch (RequestFailedException rfe) when (rfe.Status == 412)
                    {
                        var pre = req.CreateResponse(HttpStatusCode.PreconditionFailed);
                        await pre.WriteStringAsync("Concurrency conflict. Reload and retry.");
                        return pre;
                    }

                    var ok = req.CreateResponse(HttpStatusCode.OK);
                    await ok.WriteAsJsonAsync(oldEntity);
                    return ok;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT -> error during the process of updating the Configuration of Menu {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error updating Configuration of Menu {rowKey}: {ex}");
                return response;
            }
        }

        // DELETE /api/configMenu/{rowKey}
        [Function("deleteConfigurationMenu")]
        public static async Task<HttpResponseData> DeleteConfigurationMenu(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "configMenu/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfigurationMenuController");
            logger.LogInformation("DELETE -> removing the Configuration of Menu {RowKey}", rowKey);

            try
            {
                var res = await _table.GetEntityIfExistsAsync<ConfigurationMenuEntity>(_partitionKey, rowKey);
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
                logger.LogError(ex, "DELETE -> error during the process of removing the Configuration of Menu {RowKey}", rowKey);
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error removing Configuration of Menu {rowKey}: {ex}");
                return response;
            }
        }

        /// <summary>
        /// Calcula el siguiente prefijo lexicográfico para emular "startswith" con RowKey (ge/lt).
        /// </summary>
        private static string NextPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return "~"; // mayor que z / 0x7E
            var chars = prefix.ToCharArray();
            var last = chars[^1];
            chars[^1] = (char)(last + 1);
            return new string(chars);
        }
    }
}
