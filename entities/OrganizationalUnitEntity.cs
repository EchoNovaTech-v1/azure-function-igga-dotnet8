using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class OrganizationalUnitEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public OrganizationalUnitEntity(string partitionKey, string rowKey)
      {
        this.PartitionKey = partitionKey;
        this.RowKey = rowKey;
      }

      public OrganizationalUnitEntity() { }

      public string IdOrganizationalUnit { get; set; }
      public string Name { get; set; }
      public string Level { get; set; }
      public string IdOrganizationType { get; set; }
      public string IdParent { get; set; }
      public bool Active { get; set; }
  }
}
