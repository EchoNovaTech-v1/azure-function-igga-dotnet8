using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class ConfidentialDocumentEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public ConfidentialDocumentEntity(string partitionKey, string rowKey)
    {
      this.PartitionKey = partitionKey;
      this.RowKey = rowKey;
    }

    public ConfidentialDocumentEntity() { }

    public string IdConfidencialDocument { get; set; }
    public string IdStorageUnit { get; set; }
    public string Settled { get; set; }
    public DateTime SettledDate { get; set; }
    public string UserCreation { get; set; }
    public string UserModification { get; set; }
    public DateTime CreatedDate { get; set; }
    public string UserCancellation { get; set; }
    public DateTime? CanceledDate { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string IdSender { get; set; }
    public string IdReceptor { get; set; }
    public string IdSenderCorporate { get; set; }
    public string IdReceptorCorporate { get; set; }
    public string IdSenders { get; set; }
    public string IdReceptors { get; set; }
    public string IdSendersCorporate { get; set; }
    public string IdReceptorsCorporate { get; set; }
    public bool Canceled { get; set; }
  }
}
