using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class StorageUnitEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public StorageUnitEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public StorageUnitEntity() { }

        public string IdStorageUnit { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Dimensions { get; set; }
        public string Level { get; set; }
        public string IdStorageType { get; set; }
        public string IdParent { get; set; }
        public bool Active { get; set; }
    }
}
