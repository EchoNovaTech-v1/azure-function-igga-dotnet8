using System.Net;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using AppFunctions.Common;
using AppFunctions.entities;
using AppFunctions.models;
using Extensions.Config;
using Microsoft.Azure.Storage; // CloudStorageAccount
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;

namespace AppFunctions.controllers.security
{
    public class AuthController
    {
        // GET /auth/SAS
        [Function("getTokenSAS")]
        public static async Task<HttpResponseData> GetTokenSAS(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "auth/SAS")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("AuthController");
            log.LogInformation("processing token SAS...");

            try
            {
                // Conexión con el Azure Storage (usando tu helper actual)
                CloudStorageAccount storageAccount = Authentication.AuthAccountStorage();

                // Política de SAS de cuenta (igual a tu implementación actual)
                var policy = new SharedAccessAccountPolicy
                {
                    Permissions = SharedAccessAccountPermissions.Read
                                  | SharedAccessAccountPermissions.Write
                                  | SharedAccessAccountPermissions.List
                                  | SharedAccessAccountPermissions.Create
                                  | SharedAccessAccountPermissions.Update
                                  | SharedAccessAccountPermissions.Delete
                                  | SharedAccessAccountPermissions.ProcessMessages
                                  | SharedAccessAccountPermissions.Add,
                    Services = SharedAccessAccountServices.Blob
                               | SharedAccessAccountServices.File
                               | SharedAccessAccountServices.Table,
                    ResourceTypes = SharedAccessAccountResourceTypes.Service
                                    | SharedAccessAccountResourceTypes.Container
                                    | SharedAccessAccountResourceTypes.Object,
                    // Tu comentario decía 4 horas, pero el código tenía 1 mes; conservo 1 mes como en tu código:
                    SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(1),
                    Protocols = SharedAccessProtocol.HttpsOrHttp
                };

                var resPayload = new Token
                {
                    token = storageAccount.GetSharedAccessSignature(policy)
                };

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<Token>(resPayload);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "error when trying to get the sas token");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"error when trying to get the sas token: {ex}");
                return bad;
            }
        }

        // GET /auth/graphApi
        [Function("getTokenGraphApi")]
        public static async Task<HttpResponseData> GetTokenGraphApi(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "auth/graphApi")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("AuthController");
            log.LogInformation("processing token graph api...");

            try
            {
                // ADAL (tal como lo tienes actualmente)
                AuthenticationContext authenticationContext = Authentication.AuthContext();
                UserPasswordCredential userCreds = Authentication.AuthGraphApi();

                var authResult = await authenticationContext
                    .AcquireTokenAsync(Constants.ResourceUri.Get(), Constants.ClientId.Get(), userCreds)
                    .ConfigureAwait(false);

                var resPayload = new Token { token = authResult.AccessToken };

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<Token>(resPayload);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "error when trying to get the graph token");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"error when trying to get the graph token: {ex}");
                return bad;
            }
        }

        // GET /auth/graphApiAD
        [Function("getTokenGraphApiAD")]
        public static async Task<HttpResponseData> GetTokenGraphApiAD(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "auth/graphApiAD")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("AuthController");
            log.LogInformation("processing token graph api AD...");

            try
            {
                AuthenticationContext authenticationContext = Authentication.AuthContext();
                UserPasswordCredential userCreds = Authentication.AuthGraphApi();

                var resourceUriAd = Functions.Get("ResourceUriAD");
                var clientId = Functions.Get("ClientId");

                var authResult = await authenticationContext
                    .AcquireTokenAsync(resourceUriAd, clientId, userCreds)
                    .ConfigureAwait(false);

                var resPayload = new Token { token = authResult.AccessToken };

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<Token>(resPayload);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "error when trying to get the graph token (AD)");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"error when trying to get the graph token: {ex}");
                return bad;
            }
        }

        // POST /auth/allows
        [Function("authAllows")]
        public static async Task<HttpResponseData> AuthAllows(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "auth/allows")]
            HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("AuthController");
            log.LogInformation("processing authentication and getting allows...");

            try
            {
                var body = await req.ReadAsStringAsync();
                var record = JsonConvert.DeserializeObject<Auth>(body);

                if (record == null || string.IsNullOrWhiteSpace(record.token))
                {
                    var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badReq.WriteStringAsync("Invalid body or token.");
                    return badReq;
                }

                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(record.token);

                // Puedes devolver el objeto JwtSecurityToken directamente;
                // si prefieres proyectar, descomenta y usa el payload:
                // var payload = new {
                //     jwt.Issuer,
                //     jwt.Audiences,
                //     jwt.ValidFrom,
                //     jwt.ValidTo,
                //     Claims = jwt.Claims.Select(c => new { c.Type, c.Value })
                // };

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync<JwtSecurityToken>(jwt);
                return ok;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "error when trying to decode/return jwt");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync($"error when trying to get the graph token: {ex}");
                return bad;
            }
        }
    }
}
