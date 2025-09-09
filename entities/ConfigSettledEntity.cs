using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    /// <summary>
    /// Clase que representa la entidad tipos de radicados
    /// </summary>
    public class ConfigSettledEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        #region constructor
        public ConfigSettledEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public ConfigSettledEntity()
        {
        }
        #endregion

        #region fields
        public int maxSettled { get; set; }
        public int initialSettled { get; set; }
        public int autoIncrement { get; set; }
        public string dateRestart { get; set; }
        public string formatDate { get; set; }
        public string idWorkflow { get; set; }
        public bool active { get; set; }
        #endregion

    }
}
