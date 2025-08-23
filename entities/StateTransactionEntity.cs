using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class StateTransactionEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public StateTransactionEntity(string partitionkey, string rowkey)
    {
      this.PartitionKey = partitionkey;
      this.RowKey = rowkey;
    }

    public StateTransactionEntity() { }


    public string IdStateTransaction { get; set; }
    public string IdInitialState { get; set; }
    public string IdFinalState { get; set; }
    public string IdOrganizationalUnit {get; set;}
    public string IdSerie { get; set; }
    public string IdSubserie { get; set; }
    public int Time { get; set; }
    public bool Automatic { get; set; }
    public bool Active { get; set; }
  }
}
