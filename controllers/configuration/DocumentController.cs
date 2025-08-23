using System.Net;
using System.Web;
using Azure;
using Azure.Data.Tables;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using AppFunctions.Common;
using AppFunctions.Common.Table;
using AppFunctions.entities;
using Extensions.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;

namespace AppFunctions.controllers.configuration
{
    public class DocumentController
    {
        private static readonly TableClient _docTable = TableQuery.GetTable(Constants.tableDocument);
        private static readonly TableClient _confidentialTable = TableQuery.GetTable(Constants.tableConfidentialDocument);
        private static readonly TableClient _casefileTable = TableQuery.GetTable(Constants.tableCasefile);

        // Helpers ---------------------------------------------------------------

        private static (string lower, string upper) GetStartsWithRange(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return (string.Empty, char.MaxValue.ToString());
            var last = prefix[^1];
            var next = (char)(last + 1);
            var upper = prefix[..^1] + next;
            return (prefix, upper);
        }

        private static BlobContainerClient GetAttachmentsContainer()
        {
            // Usa AzureWebJobsStorage; si tienes otra clave, cámbiala aquí.
            var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var svc = new BlobServiceClient(conn);
            return svc.GetBlobContainerClient("attachments");
        }

        private static string BuildBlobSasUrl(BlobClient blob, TimeSpan ttl)
        {
            if (blob.CanGenerateSasUri)
            {
                var sas = blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(ttl));
                return sas.ToString();
            }

            // Fallback: construir SAS desde connection string
            var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? string.Empty;
            // Extraer nombre y key del connection string
            // (muy común en AzureWebJobsStorage)
            string? accountName = null, accountKey = null;
            foreach (var kv in conn.Split(';'))
            {
                var p = kv.Split('=', 2);
                if (p.Length != 2) continue;
                if (p[0].Equals("AccountName", StringComparison.OrdinalIgnoreCase)) accountName = p[1];
                if (p[0].Equals("AccountKey", StringComparison.OrdinalIgnoreCase)) accountKey = p[1];
            }
            if (accountName is null || accountKey is null)
                throw new InvalidOperationException("No SharedKey credentials found to generate SAS.");

            var cred = new StorageSharedKeyCredential(accountName, accountKey);
            var b = new BlobSasBuilder
            {
                BlobContainerName = blob.BlobContainerName,
                BlobName = blob.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(ttl)
            };
            b.SetPermissions(BlobSasPermissions.Read);

            var uri = new UriBuilder(blob.Uri)
            {
                Query = b.ToSasQueryParameters(cred).ToString()
            };
            return uri.Uri.ToString();
        }

        // GET /api/documents/{rowKey}  (prefijo "starts with")
        [Function("getDocument")]
        public static async Task<HttpResponseData> GetDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "documents/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DocumentController");
            log.LogInformation("GET -> getting documents with RowKey starts-with {RowKey}", rowKey);

            try
            {
                var (lower, upper) = GetStartsWithRange(rowKey);
                var filter = $"RowKey ge '{lower}' and RowKey lt '{upper}'";

                var list = new List<DocumentEntity>();
                await foreach (var e in _docTable.QueryAsync<DocumentEntity>(filter))
                    list.Add(e);

                var res = req.CreateResponse(HttpStatusCode.OK);
                await res.WriteAsJsonAsync(list);
                return res;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET -> error to get documents {RowKey}", rowKey);
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"Error obtaining documents {rowKey}: {ex}");
                return res;
            }
        }

        // GET /api/documents
        [Function("getDocuments")]
        public static async Task<HttpResponseData> GetDocuments(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "documents")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DocumentController");
            log.LogInformation("GET ALL -> getting all documents");

            try
            {
                var list = new List<DocumentEntity>();
                await foreach (var e in _docTable.QueryAsync<DocumentEntity>())
                    list.Add(e);

                var res = req.CreateResponse(HttpStatusCode.OK);
                await res.WriteAsJsonAsync(list);
                return res;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GET ALL -> error getting documents");
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"Error obtaining documents: {ex}");
                return res;
            }
        }

        // GET /api/addRadicadeToDocuments?RowKey=...&Radicado=...
        [Function("AddRadicadeToDocuments")]
        public static async Task<HttpResponseData> AddRadicadeToDocuments(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "addRadicadeToDocuments")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DocumentController");
            log.LogInformation("GET -> add radicado to documents");

            try
            {
                var qs = HttpUtility.ParseQueryString(req.Url.Query);
                var pRowKey = qs.Get("RowKey");
                var pRadicado = qs.Get("Radicado");

                if (string.IsNullOrWhiteSpace(pRowKey) || string.IsNullOrWhiteSpace(pRadicado))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Missing query params: RowKey and Radicado are required.");
                    return bad;
                }

                var container = GetAttachmentsContainer();
                var prefix = $"{pRowKey.TrimEnd('/')}/";

                var tempDir = Path.Combine(Path.GetTempPath(), pRowKey);
                Directory.CreateDirectory(tempDir);

                await foreach (var item in container.GetBlobsByHierarchyAsync(prefix: prefix))
                {
                    if (!item.IsBlob) continue;

                    var blob = container.GetBlobClient(item.Blob.Name);
                    var fileName = Path.GetFileName(item.Blob.Name);
                    var local = Path.Combine(tempDir, fileName);

                    await blob.DownloadToAsync(local);

                    var ext = Path.GetExtension(local).TrimStart('.').ToLowerInvariant();
                    switch (ext)
                    {
                        case "doc":
                        case "docx":
                            Helper.ManagementDocs.InsertRadicadeInDoc(local, pRadicado);
                            break;
                        case "xlx":
                        case "xlsx":
                            var portada = Helper.ManagementDocs.CreatePortadaExcel(tempDir, pRadicado);
                            Helper.ManagementDocs.MergeDocumentExcel(portada, local);
                            break;
                        case "pdf":
                            Helper.ManagementDocs.InsertRadicadeInPdf(local, pRadicado);
                            break;
                        default:
                            break;
                    }

                    // Re-subir el archivo (sobrescribiendo)
                    using var fs = File.OpenRead(local);
                    await blob.UploadAsync(fs, overwrite: true);
                }

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"Error processing documents: {ex}");
                return res;
            }
        }

        // GET /api/addLabelsToDocument?RowKey=...&Radicado=...&DocumentType=...&Fecha=...
        [Function("AddLabelsToDocument")]
        public static async Task<HttpResponseData> AddLabelsToDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "addLabelsToDocument")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DocumentController");
            log.LogInformation("GET -> add labels (radicado/fecha) to documents");

            try
            {
                var qs = HttpUtility.ParseQueryString(req.Url.Query);
                var pRowKey = qs.Get("RowKey");
                var pRadicado = qs.Get("Radicado");
                var pDocument = qs.Get("DocumentType");
                var pFecha = qs.Get("Fecha");

                var validDocs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Carta", "Memorando", "Circulares" };

                if (string.IsNullOrWhiteSpace(pRowKey) || string.IsNullOrWhiteSpace(pFecha) || string.IsNullOrWhiteSpace(pRadicado))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Missing query params: RowKey, Radicado and Fecha are required.");
                    return bad;
                }

                var container = GetAttachmentsContainer();
                var prefix = $"{pRowKey.TrimEnd('/')}/";

                var tempDir = Path.Combine(Path.GetTempPath(), $"Date-{pRowKey}");
                Directory.CreateDirectory(tempDir);

                await foreach (var item in container.GetBlobsByHierarchyAsync(prefix: prefix))
                {
                    if (!item.IsBlob) continue;

                    var blob = container.GetBlobClient(item.Blob.Name);
                    var fileName = Path.GetFileName(item.Blob.Name);
                    var local = Path.Combine(tempDir, fileName);

                    await blob.DownloadToAsync(local);

                    var ext = Path.GetExtension(local).TrimStart('.').ToLowerInvariant();
                    switch (ext)
                    {
                        case "pdf":
                            if (pRadicado.Length > 8)
                                Helper.ManagementDocs.InsertRadicadeInPdf(local, pRadicado);
                            if (validDocs.Contains(pDocument ?? string.Empty))
                                Helper.ManagementDocs.InsertDateInPdf(local, pFecha!);
                            break;

                        case "doc":
                        case "docx":
                            if (pRadicado.Length > 8)
                                Helper.ManagementDocs.InsertRadicadeInDoc(local, pRadicado);
                            if (validDocs.Contains(pDocument ?? string.Empty))
                                Helper.ManagementDocs.InsertDateInDoc(local, pFecha!);
                            break;

                        case "xlx":
                        case "xlsx":
                            var portada = Helper.ManagementDocs.CreatePortadaExcel(tempDir, pFecha!);
                            Helper.ManagementDocs.MergeDocumentExcel(portada, local);
                            break;

                        default:
                            break;
                    }

                    using var fs = File.OpenRead(local);
                    await blob.UploadAsync(fs, overwrite: true);
                }

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"Error processing documents: {ex}");
                return res;
            }
        }

        // GET /api/validateSettledIsAssigned?Settled=...
        [Function("ValidateSettledIsAssigned")]
        public static async Task<HttpResponseData> ValidateSettledIsAssigned(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "validateSettledIsAssigned")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DocumentController");
            log.LogInformation("GET -> validate Settled is assigned");

            try
            {
                var qs = HttpUtility.ParseQueryString(req.Url.Query);
                var settled = qs.Get("Settled");

                if (string.IsNullOrWhiteSpace(settled))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Missing query param: Settled");
                    return bad;
                }

                var filter = $"Settled eq '{settled.Replace("'", "''")}'";
                var any = await _docTable.QueryAnyAsync<DocumentEntity>(filter);

                var res = req.CreateResponse(HttpStatusCode.OK);
                await res.WriteAsJsonAsync(new { settledAssigned = any });
                return res;
            }
            catch (Exception ex)
            {
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"Error validating settled: {ex}");
                return res;
            }
        }

        // GET /api/getAssignedDocuments?Mail=...&Company=...
        [Function("GetAssignedDocuments")]
        public static async Task<HttpResponseData> GetAssignedDocuments(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "getAssignedDocuments")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DocumentController");
            log.LogInformation("GET -> get assigned documents");

            try
            {
                var qs = HttpUtility.ParseQueryString(req.Url.Query);
                var pMail = qs.Get("Mail");
                var pCompany = qs.Get("Company");

                if (string.IsNullOrWhiteSpace(pMail) || string.IsNullOrWhiteSpace(pCompany))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Missing query params: Mail and Company are required.");
                    return bad;
                }

                var filter = $"Company eq '{pCompany.Replace("'", "''")}'";

                var results = new List<DocumentEntity>();

                // Documentos no confidenciales
                await foreach (var d in _docTable.QueryAsync<DocumentEntity>(filter))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(d.Receptors) && d.Receptors.Contains(pMail, StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(d);
                        }
                    }
                    catch
                    {
                        // saltar nulos
                    }
                }

                // Documentos confidenciales
                await foreach (var d in _confidentialTable.QueryAsync<DocumentEntity>(filter))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(d.Receptors) && d.Receptors.Contains(pMail, StringComparison.OrdinalIgnoreCase))
                        {
                            d.IsConfidential = true;
                            results.Add(d);
                        }
                    }
                    catch
                    {
                        // saltar nulos
                    }
                }

                var res = req.CreateResponse(HttpStatusCode.OK);
                await res.WriteAsJsonAsync(results);
                return res;
            }
            catch (Exception ex)
            {
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"Error obtaining assigned documents: {ex}");
                return res;
            }
        }

        // GET /api/getDocumentsByCasefile?casefiles=a,b,c&Company=...
        [Function("GetDocumentsByCasefile")]
        public static async Task<HttpResponseData> GetDocumentsByCasefile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "getDocumentsByCasefile")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DocumentController");
            log.LogInformation("GET -> get documents by casefile");

            try
            {
                var qs = HttpUtility.ParseQueryString(req.Url.Query);
                var pCasefiles = qs.Get("casefiles");
                var pCompany = qs.Get("Company");

                if (string.IsNullOrWhiteSpace(pCasefiles) || string.IsNullOrWhiteSpace(pCompany))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Missing query params: casefiles and Company are required.");
                    return bad;
                }

                var filter = $"Company eq '{pCompany.Replace("'", "''")}'";
                var all = new List<DocumentEntity>();
                await foreach (var d in _docTable.QueryAsync<DocumentEntity>(filter))
                    all.Add(d);

                var set = new HashSet<string>((pCasefiles ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                var selected = all.Where(d => !string.IsNullOrEmpty(d.IdCasefile) && set.Any(s => d.IdCasefile.Contains(s, StringComparison.OrdinalIgnoreCase)))
                                  .GroupBy(d => d.RowKey)
                                  .Select(g => g.First())
                                  .ToList();

                var res = req.CreateResponse(HttpStatusCode.OK);
                await res.WriteAsJsonAsync(selected);
                return res;
            }
            catch (Exception ex)
            {
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"Error obtaining documents by casefile: {ex}");
                return res;
            }
        }

        // DELETE /api/documents/{rowKey}
        [Function("deleteDocument")]
        public static async Task<HttpResponseData> DeleteDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "documents/{rowKey}")]
            HttpRequestData req,
            string rowKey,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DocumentController");
            log.LogInformation("DELETE -> removing document {RowKey}", rowKey);

            try
            {
                var entity = await _docTable.QueryFirstOrDefaultAsync<DocumentEntity>(x => x.RowKey == rowKey);
                if (entity is null)
                {
                    var nf = req.CreateResponse(HttpStatusCode.NotFound);
                    await nf.WriteAsJsonAsync(new { message = $"no record found with RowKey {rowKey}" });
                    return nf;
                }

                await _docTable.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);
                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"Error removing document {rowKey}: {ex}");
                return res;
            }
        }

        // GET /api/getDocumentUrl?RowKey=...&FileName=...
        [Function("GetDocumentUrl")]
        public static async Task<HttpResponseData> GetDocumentUrl(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "getDocumentUrl")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("DocumentController");
            log.LogInformation("GET -> get document SAS url");

            try
            {
                var qs = HttpUtility.ParseQueryString(req.Url.Query);
                var pRowKey = qs.Get("RowKey");
                var fileName = qs.Get("FileName");

                if (string.IsNullOrWhiteSpace(pRowKey) || string.IsNullOrWhiteSpace(fileName))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Missing query params: RowKey and FileName are required.");
                    return bad;
                }

                var container = GetAttachmentsContainer();
                var blob = container.GetBlobClient($"{pRowKey.TrimEnd('/')}/{fileName}");

                var linkUrl = BuildBlobSasUrl(blob, TimeSpan.FromDays(365));

                var res = req.CreateResponse(HttpStatusCode.OK);
                await res.WriteAsJsonAsync(new { linkUrl });
                return res;
            }
            catch (Exception ex)
            {
                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync($"Error generating SAS URL: {ex}");
                return res;
            }
        }

        // Utilidad opcional si aún la necesitas (actualiza el número de documento de un expediente)
        private static async Task<int> GetOrderAsync(string idCasefile)
        {
            var cf = await _casefileTable.QueryFirstOrDefaultAsync<CasefileEntity>(x => x.RowKey == idCasefile);
            return cf is null ? 0 : (cf.DocumentNumber + 1);
        }
    }
}
