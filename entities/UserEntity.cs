using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class UserEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public UserEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public UserEntity() { }

        public string IdAuth { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string JobTitle { get; set; }
        public string Email { get; set; }
        public string MobilePhone { get; set; }
        //Campo indicará con que cuenta se logueo el usuario
        // google o microsoft
        public string AccessAccount { get; set; }
        public bool Active { get; set; }
    }
}
