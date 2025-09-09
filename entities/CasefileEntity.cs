using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class CasefileEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public CasefileEntity(string partitionKey, string rowKey)
    {
      this.PartitionKey = partitionKey;
      this.RowKey = rowKey;
    }

    public CasefileEntity() { }

    public string IdOrganizationalUnit { get; set; }
    public string IdStorageUnit { get; set; }
    public string IdSerie { get; set; }
    public string IdSubserie { get; set; }
    public string IdStatusCasefile { get; set; }
    public string IdArchive { get; set; }
    public string Title { get; set; }
    public int DocumentNumber { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ClosedDate { get; set; }
    public DateTime ModifiedDateArchive { get; set; }
    public string Owner { get; set; }
    public string Responsable { get; set; }
    public string Sign { get; set; }
    public string Method { get; set; }
    public int FolderNumber { get; set; }
    public string Keywords { get; set; }
    public string URL { get; set; }
    public bool Active { get; set; }
    public string UserClosed { get; set; }
  }
}
