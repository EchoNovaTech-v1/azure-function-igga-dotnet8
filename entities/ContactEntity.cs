using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class ContactEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public ContactEntity(string partitionKey, string rowKey)
    {
      this.PartitionKey = partitionKey;
      this.RowKey = rowKey;
    }

    public ContactEntity() { }

    public string IdContact { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string Municipality { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public bool Active { get; set; }
  }
}
