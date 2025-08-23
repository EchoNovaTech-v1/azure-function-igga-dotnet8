using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    /// <summary>
    /// Clase que representa la entidad Work Flows
    /// </summary>
    public class WorkFlowEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        #region constructor
        public WorkFlowEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public WorkFlowEntity()
        {
        }
        #endregion

        #region Fields

        public string Name { get; set; }

        public string Prefix { get; set; }
    
        public string Description { get; set; }

        public Boolean Active { get; set; }


        #endregion
    }
}
