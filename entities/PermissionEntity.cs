using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class PermissionEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public PermissionEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public PermissionEntity() { }

        public string IdResource { get; set; }
        public string IdPermission { get; set; }
        public string PartitionName { get; set; }
        public string IdRecord { get; set; }
        public string IdRole { get; set; }
        public bool Active { get; set; }
  }
}
