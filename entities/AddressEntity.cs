using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class AddressEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public AddressEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public AddressEntity() { }

        public string Code { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
    }
}
