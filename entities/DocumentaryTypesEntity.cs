using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class DocumentaryTypesEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public DocumentaryTypesEntity() { }
        public DocumentaryTypesEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }
        
        public string Name { get; set; }
        public string Description { get; set; }
        public string Keycode { get; set; }
        public bool Active { get; set; }
    }
}