using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class SerieEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public SerieEntity(string partitionKey, string rowKey) {
      this.PartitionKey = partitionKey;
      this.RowKey = rowKey;
    }
    public SerieEntity() { }

    public string IdSerie { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Keycode { get; set; }
    public string IdOrganizationalUnit { get; set; }
    public bool Active { get; set; }
  }
}
