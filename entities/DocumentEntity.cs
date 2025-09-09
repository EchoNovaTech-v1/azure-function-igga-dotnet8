using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class DocumentEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public DocumentEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public DocumentEntity() { }
        public double CreatedDate { get; set; }
        public string CreationTime { get; set; }
        public string Description {get;set; }
        public string DocumentName { get; set; } 
        public string IdCasefile { get; set; }
        public string Casefile { get; set; }
        public string IdDocumentalType { get; set; }
        public string DocumentalType { get; set; }
        public string IdWorkflow { get; set; }
        public string Workflow { get; set; }
        public string DocumentDescription { get; set; }
        public string IdSender { get; set; }
        public string SenderName { get; set; }
        public string SenderAddress { get; set; }
        public string SenderState { get; set; }
        public string SenderPhone { get; set; }
        public string SenderMail { get; set; }
        public string Receptors { get; set; }
        public string ReceptorsCopy { get; set; }
        public string HashReturn { get; set; }
        public string Settled { get; set; }
        public string SettledState { get; set; }
        public bool Closed { get; set; }
        public string ClosedDate { get; set; }
        public string UserClosed { get; set; }
        public string IdUserClosed { get; set; }
        public string EmailUserClosed { get; set; }
        public bool Active { get; set; }
        public bool IsConfidential { get; set; }
        
    }
}
