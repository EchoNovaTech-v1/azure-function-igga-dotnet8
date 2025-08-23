namespace AppFunctions.models
{
    class NotificationVariable
    {
        public string name { get; set; }
        public string openChar { get; set; }
        public string closeChar { get; set; }
        public string tableName { get; set; }
        public string partitionName { get; set; }
        public string fieldName { get; set; }
        public string description { get; set; }
        public bool active { get; set; }
    }
}
