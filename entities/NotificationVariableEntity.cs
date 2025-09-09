using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class NotificationVariableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public NotificationVariableEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public NotificationVariableEntity()
        {

        }

        public string Name { get; set; }
        public string OpenChar { get; set; }
        public string CloseChar { get; set; }
        public string TableName { get; set; }
        public string PartitionName { get; set; }
        public string FieldName { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
    }
}
