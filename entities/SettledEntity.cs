using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    /// <summary>
    /// clase que representa la entidad de radicados
    /// </summary>
    public class SettledEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        #region Contructor
        public SettledEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public SettledEntity() { }
        #endregion

        #region fields
        public string idWorkflow { get; set; }
        public int year { get; set; }
        public int month { get; set; }
        public int day { get; set; }
        public int consecutive { get; set; }
        #endregion
    }
}
