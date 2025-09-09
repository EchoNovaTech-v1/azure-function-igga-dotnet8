using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class SubserieEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public SubserieEntity(string partitionKey, string rowKey) {
      this.PartitionKey = partitionKey;
      this.RowKey = rowKey;
    }

    public SubserieEntity() { }

    public string IdSubserie { get; set; }
    public string IdOrganizationalUnit { get; set; }
    public string IdSerie { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string IdsDisposicionFinal { get; set; }
    public string IdsFileEvaluation { get; set; }
    public bool Active { get; set; }
  }
}
