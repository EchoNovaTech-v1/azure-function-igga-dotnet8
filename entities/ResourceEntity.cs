using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    class ResourceEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public ResourceEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public ResourceEntity() { }

        public string IdResource { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public bool Hereditary { get; set; }
        public string IdParent { get; set; }
        public bool Active { get; set; }

    }
}
