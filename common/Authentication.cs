using Azure.Data.Tables;
using Azure.Identity;
using Extensions.Config;

namespace AppFunctions.Common
{
    public static class Authentication
    {
        /// <summary>
        /// Reemplazo moderno de CloudStorageAccount usando TableServiceClient.
        /// Se mantiene el nombre del método por compatibilidad.
        /// </summary>
        public static TableServiceClient AuthAccountStorage()
        {
            try
            {
                string connectionString = Convert.ToBoolean(Constants.Production.Get()) ?
                    Constants.AzureWebJobsStorage.Get() :
                    Constants.AzureWebJobsStorageQA.Get();

                return new TableServiceClient(connectionString);
            }
            catch (Exception ex)
            {
                var error = $"error when trying to authenticate the storage account ({ex})";
                throw new ArgumentException(error);
            }
        }

        /// <summary>
        /// Método mantenido por compatibilidad. Devuelve el endpoint como string.
        /// </summary>
        public static string AuthContext()
        {
            try
            {
                return Constants.AzureAuthorizationEndpoint.Get();
            }
            catch (Exception ex)
            {
                var error = $"error when trying to get the Azure authorization endpoint ({ex})";
                throw new ArgumentException(error);
            }
        }

        /// <summary>
        /// Autenticación moderna con Graph API usando UsernamePasswordCredential.
        /// </summary>
        public static UsernamePasswordCredential AuthGraphApi()
        {
            try
            {
                var tenantId = Constants.TenantId.Get();
                var clientId = Constants.ClientId.Get();
                var username = Constants.UserAdmin.Get();
                var password = Constants.PassAdmin.Get();

                return new UsernamePasswordCredential(username, password, tenantId, clientId);
            }
            catch (Exception ex)
            {
                var error = $"error when trying to authenticate the Graph API ({ex})";
                throw new ArgumentException(error);
            }
        }
    }
}
