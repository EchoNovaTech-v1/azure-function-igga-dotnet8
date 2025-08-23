using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class NotificationTransactionEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public NotificationTransactionEntity(string partitionKey, string rowKey) {
      this.PartitionKey = partitionKey;
      this.RowKey = rowKey;
    }

    public NotificationTransactionEntity() { }

    public string IdNotificationTransaction { get; set; }
    public string IdStateTransaction { get; set; }
    public string To { get; set; }
    public string CC { get; set; }
    public string Subject { get; set; }
    public int EventTime { get; set; }
    public string Message { get; set; }
    public bool Active { get; set; }
  }
}
