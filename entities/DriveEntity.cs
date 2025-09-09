using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class DriveEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public DriveEntity(string partitionKey, string rowKey)
    {
      this.PartitionKey = partitionKey;
      this.RowKey = rowKey;
    }

    public DriveEntity() { }

    public string url { get; set; }
    public string user { get; set; }
    public string password { get; set; }
    public bool active { get; set; }
  }
}
