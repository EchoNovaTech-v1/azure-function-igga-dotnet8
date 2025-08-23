using Azure.Data.Tables;

namespace AppFunctions.Common.Table
{
    public static class TableQuery
    {
        /// <summary>
        /// Reemplazo de GetTable que retorna TableClient.
        /// </summary>
        public static TableClient GetTable(string tableName)
        {
            var serviceClient = Authentication.AuthAccountStorage(); // sigue siendo TableServiceClient
            return serviceClient.GetTableClient(tableName);
        }
    }

    
}
