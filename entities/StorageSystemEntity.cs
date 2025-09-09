using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class StorageSystemEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public StorageSystemEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public StorageSystemEntity() { }

        public string Provider { get; set; }
        public string IdApplication { get; set; }
        public string IdObject { get; set; }
        public string Password { get; set; }
        public string ApiEndpoint { get; set; }
        public bool Active { get; set; }
    }
}
