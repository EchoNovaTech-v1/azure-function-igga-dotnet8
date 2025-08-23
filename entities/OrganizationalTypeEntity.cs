using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class OrganizationalTypeEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public OrganizationalTypeEntity(string partitionKey, string rowKey)
    {
      this.PartitionKey = partitionKey;
      this.RowKey = rowKey;
    }

    public OrganizationalTypeEntity() { }

    public string Name { get; set; }
    public string Description { get; set; }
    public string Keycode { get; set; }
    public bool Active { get; set; }
  }
}
