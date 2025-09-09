using Azure;
using Azure.Data.Tables;
namespace AppFunctions.entities
{
    public class UsersByGroupEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public UsersByGroupEntity(string partitionKey, string rowKey)
    {
      this.PartitionKey = partitionKey;
      this.RowKey = rowKey;
    }

    public UsersByGroupEntity() { }
      
    public string IdGroup { get; set; }
    public string IdUser { get; set; }
    public bool Active { get; set; }
  }
}
