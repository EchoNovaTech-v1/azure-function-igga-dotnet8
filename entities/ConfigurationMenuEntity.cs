using Azure;
using Azure.Data.Tables;

namespace AppFunctions.entities
{
    public class ConfigurationMenuEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
        public ConfigurationMenuEntity(string partitionKey, string rowKey) 
        {
        this.PartitionKey = partitionKey;
        this.RowKey = rowKey;
        }

        public ConfigurationMenuEntity() { }


        public string Name { get; set; }
        public string IdMenu { get; set; }
        public string IdParent { get; set; }
        public string Route { get; set; }
        public string Type { get; set; }
        public string Icon { get; set; }
        public bool Defect { get; set; }
        public int Level { get; set; }
        public bool DragAndDrog  { get; set; }
        public bool Active { get; set; }
        public int NumOrder { get; set; }

    }
}