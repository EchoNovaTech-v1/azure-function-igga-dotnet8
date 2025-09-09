using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class AuditEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public AuditEntity(string partitionKey, string rowKey)
    {
      this.PartitionKey = partitionKey;
      this.RowKey = rowKey;
    }

    public AuditEntity() { }

    public string TableName { get; set; }
    public string PartitionName { get; set; }
    public string IdUser { get; set; }
    public string IdResource { get; set; }
    public string IdAction { get; set; }
    public string IdRol { get; set; }
    public string IdDirectory { get; set; }
    public DateTime CreatedDate { get; set; }
  }
}
