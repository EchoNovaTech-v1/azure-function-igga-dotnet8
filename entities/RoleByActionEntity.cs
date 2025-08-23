using Azure;
using Azure.Data.Tables;
namespace AppFunctions.entities
{
    public class RoleByActionEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public RoleByActionEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public RoleByActionEntity() { }

        public string IdRole { get; set; }
        public string IdAction { get; set; }
        public bool Active { get; set; }
    }
}