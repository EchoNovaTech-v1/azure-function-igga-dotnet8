using Azure;
using Azure.Data.Tables;
namespace AppFunctions.entities
{
    public class RoleEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public RoleEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public RoleEntity() { }
      
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
    }
}