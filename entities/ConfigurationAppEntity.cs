using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class ConfigurationAppEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public ConfigurationAppEntity(string partitionKey, string rowKey) {
      this.PartitionKey = partitionKey;
      this.RowKey = rowKey;
    }

    public ConfigurationAppEntity() { }

    public string Name { get; set; }
    public string Language { get; set; }
    public string IdDirectory { get; set; }
    public string ImageHome { get; set; }
    public string UrlFacebook { get; set; }
    public string UrlTwitter { get; set; }
    public string UrilOfficialPage { get; set; }
    public string Phone { get; set; }
  }
}
