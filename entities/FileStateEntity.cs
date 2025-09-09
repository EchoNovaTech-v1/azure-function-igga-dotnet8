using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    class FileStateEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public FileStateEntity() { }

        public FileStateEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public bool Terminal { get; set; }
        public bool Active { get; set; }
    }
}