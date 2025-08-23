using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using AppFunctions.models;
using Azure.Data.Tables;
using Extensions.Http;
using Extensions.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AppFunctions.controllers.configuration
{
    public class ConfidentialDocumentController
    {
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableConfidentialDocument);

        // Función para obtener un documento confidencial por su rowKey
        [Function("getConfidentialDocument")]
        public static async Task<HttpResponseData> getConfidentialDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "confidentialDocuments/{rowKey}")] HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfidentialDocumentController");
            logger.LogInformation($"GET -> getting the confidential document with RowKey {rowKey}");

            try
            {
                var document = await _table.QueryFirstOrDefaultAsync<ConfidentialDocumentEntity>(x => x.RowKey == rowKey);
                var response = document ?? new object();
                return await req.OkAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                logger.LogError($"GET -> error getting the confidential document {rowKey}: {ex}");
                return await req.BadRequestAsync($"There was an error retrieving the confidential document: {ex.Message}");
            }
        }

        // Función para obtener toda la lista de documentos confidenciales
        [Function("getConfidentialDocuments")]
        public static async Task<HttpResponseData> getConfidentialDocuments(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "confidentialDocuments")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfidentialDocumentController");
            logger.LogInformation("GET ALL -> getting all the confidential documents");

            try
            {
                var documents = await _table.QueryToListAsync<ConfidentialDocumentEntity>();
                return await req.OkAsJsonAsync(documents);
            }
            catch (Exception ex)
            {
                logger.LogError($"GET ALL -> error retrieving confidential documents: {ex}");
                return await req.BadRequestAsync($"Error retrieving confidential documents: {ex.Message}");
            }
        }

        // Función para insertar un nuevo documento confidencial
        [Function("postConfidentialDocument")]
        public static async Task<HttpResponseData> postConfidentialDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "confidentialDocuments")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfidentialDocumentController");
            logger.LogInformation("POST -> inserting confidential document");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<ConfidentialDocument>(body);

                var rowKey = Guid.NewGuid().ToString();
                var entity = new ConfidentialDocumentEntity(record.idStorageUnit, rowKey)
                {
                    IdConfidencialDocument = rowKey,
                    IdStorageUnit = record.idStorageUnit,
                    Settled = record.settled,
                    SettledDate = record.settledDate,
                    UserCreation = record.userCreation,
                    UserModification = record.userCreation,
                    CreatedDate = record.createdDate,
                    Name = record.name,
                    Description = record.description,
                    IdSender = record.idSender,
                    IdReceptor = record.idReceptor,
                    IdSenderCorporate = record.idSenderCorporate,
                    IdReceptorCorporate = record.idReceptorCorporate,
                    IdSenders = string.Join("|", record.idSenders),
                    IdReceptors = string.Join("|", record.idReceptors),
                    IdSendersCorporate = string.Join("|", record.idSendersCorporate),
                    IdReceptorsCorporate = string.Join("|", record.idReceptorsCorporate),
                    Canceled = false
                };

                await _table.AddEntityAsync(entity);
                return await req.CreatedAsJsonAsync(entity);
            }
            catch (Exception ex)
            {
                logger.LogError($"POST -> error inserting confidential document: {ex}");
                return await req.BadRequestAsync($"Error inserting confidential document: {ex.Message}");
            }
        }

        // Función para actualizar un documento confidencial
        [Function("putConfidentialDocument")]
        public static async Task<HttpResponseData> putConfidentialDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "PUT", Route = "confidentialDocuments/{rowKey}")] HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfidentialDocumentController");
            logger.LogInformation($"PUT -> updating confidential document with RowKey {rowKey}");

            try
            {
                var existing = await _table.QueryFirstOrDefaultAsync<ConfidentialDocumentEntity>(x => x.RowKey == rowKey);
                if (existing == null)
                    return await req.NotFoundAsync($"No record found with RowKey {rowKey}");

                var body = await req.ReadAsStringAsync();
                var updated = JsonConvert.DeserializeObject<ConfidentialDocument>(body);

                if (existing.IdStorageUnit != updated.idStorageUnit)
                {
                    var newEntity = new ConfidentialDocumentEntity(updated.idStorageUnit, existing.RowKey)
                    {
                        IdStorageUnit = updated.idStorageUnit,
                        Name = updated.name,
                        Description = updated.description,
                        UserModification = updated.userModification,
                        UserCancellation = updated.userCancellation,
                        CanceledDate = updated.canceledDate,
                        IdSender = updated.idSender,
                        IdReceptor = updated.idReceptor,
                        IdSenderCorporate = updated.idSenderCorporate,
                        IdReceptorCorporate = updated.idReceptorCorporate,
                        IdSenders = string.Join("|", updated.idSenders),
                        IdReceptors = string.Join("|", updated.idReceptors),
                        IdSendersCorporate = string.Join("|", updated.idSendersCorporate),
                        IdReceptorsCorporate = string.Join("|", updated.idReceptorsCorporate),
                        Canceled = updated.canceled
                    };

                    await _table.AddEntityAsync(newEntity);
                    await _table.DeleteEntityAsync(existing.PartitionKey, existing.RowKey);
                    existing = newEntity;
                }
                else
                {
                    existing.Name = updated.name;
                    existing.Description = updated.description;
                    existing.UserModification = updated.userModification;
                    existing.UserCancellation = updated.userCancellation;
                    existing.CanceledDate = updated.canceledDate;
                    existing.IdSender = updated.idSender;
                    existing.IdReceptor = updated.idReceptor;
                    existing.IdSenderCorporate = updated.idSenderCorporate;
                    existing.IdReceptorCorporate = updated.idReceptorCorporate;
                    existing.IdSenders = string.Join("|", updated.idSenders);
                    existing.IdReceptors = string.Join("|", updated.idReceptors);
                    existing.IdSendersCorporate = string.Join("|", updated.idSendersCorporate);
                    existing.IdReceptorsCorporate = string.Join("|", updated.idReceptorsCorporate);
                    existing.Canceled = updated.canceled;

                    await _table.UpdateEntityAsync(existing, existing.ETag, TableUpdateMode.Replace);
                }

                return await req.OkAsJsonAsync(existing);
            }
            catch (Exception ex)
            {
                logger.LogError($"PUT -> error updating confidential document {rowKey}: {ex}");
                return await req.BadRequestAsync($"Error updating confidential document: {ex.Message}");
            }
        }

        // Función para eliminar un documento confidencial
        [Function("deleteConfidentialDocument")]
        public static async Task<HttpResponseData> deleteConfidentialDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "confidentialDocuments/{rowKey}")] HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("ConfidentialDocumentController");
            logger.LogInformation($"DELETE -> removing confidential document with RowKey {rowKey}");

            try
            {
                var existing = await _table.QueryFirstOrDefaultAsync<ConfidentialDocumentEntity>(x => x.RowKey == rowKey);
                if (existing == null)
                    return await req.NotFoundAsync($"No record found with RowKey {rowKey}");

                await _table.DeleteEntityAsync(existing.PartitionKey, existing.RowKey);
                return await req.NoContentAsync();
            }
            catch (Exception ex)
            {
                logger.LogError($"DELETE -> error removing confidential document {rowKey}: {ex}");
                return await req.ErrorAsync($"Error removing confidential document: {ex.Message}");
            }
        }
    }
}
