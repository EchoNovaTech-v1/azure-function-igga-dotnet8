using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using AppFunctions.models;
using Azure.Data.Tables;
using Extensions.Cryptography;
using Extensions.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace AppFunctions.controllers.configuration
{
    public class CasefileController
    {
        private static readonly TableClient _table = TableQuery.GetTable(Constants.tableCasefile);

        [Function("getCasefile")]
        public static async Task<HttpResponseData> getCasefile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "casefiles/{rowKey}")] HttpRequestData req,
            string rowKey,
            FunctionContext context)
        {
            var logger = context.GetLogger("CasefileController");
            logger.LogInformation($"GET -> getting the casefile with RowKey {rowKey}");

            try
            {
                var result = await _table.QueryFirstOrDefaultAsync<CasefileEntity>(x => x.RowKey == rowKey);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(result ?? new object());
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"GET -> error to get the casefile {rowKey}: {ex}");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving the casefile {rowKey}: {ex}");
                return response;
            }
        }

        [Function("getCasefiles")]
        public static async Task<HttpResponseData> getCasefiles(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "casefiles")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("CasefileController");
            logger.LogInformation("GET ALL -> getting all the casefiles");

            try
            {
                var results = new List<CasefileEntity>();
                await foreach (var item in _table.QueryAsync<CasefileEntity>())
                {
                    results.Add(item);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(results);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"GET ALL -> error during the process of getting casefiles: {ex}");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error retrieving casefiles: {ex}");
                return response;
            }
        }

        [Function("postCasefile")]
        public static async Task<HttpResponseData> postCasefile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "casefiles")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("CasefileController");
            logger.LogInformation("POST -> inserting casefile");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Casefile>(body);

                string partitionKey = $"{record.idOrganizationalUnit}|{record.idSerie}|-{record.idSubserie}";
                string rowKey = Guid.NewGuid().ToString();

                var casefile = new CasefileEntity(partitionKey, rowKey)
                {
                    IdOrganizationalUnit = record.idOrganizationalUnit,
                    IdStorageUnit = record.idStorageUnit,
                    IdSerie = record.idSerie,
                    IdSubserie = record.idSubserie,
                    IdStatusCasefile = record.idStatusCasefile,
                    IdArchive = record.idArchive,
                    Title = record.title,
                    DocumentNumber = record.documentNumber,
                    CreatedDate = record.createdDate,
                    ModifiedDateArchive = record.createdDate,
                    ClosedDate = record.closedDate,
                    Owner = record.owner,
                    Responsable = record.responsable,
                    Method = record.method,
                    FolderNumber = record.folderNumber,
                    Keywords = string.Join("|", record.keywords),
                    URL = record.url,
                    Sign = record.method.CreateMD5(),
                    Active = true,
                    UserClosed = ""
                };

                await _table.AddEntityAsync(casefile);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(casefile);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"POST -> error inserting casefile: {ex}");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"Error inserting casefile: {ex}");
                return response;
            }
        }

        
    }
}
