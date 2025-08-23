namespace AppFunctions.models
{
    public class Notification
    {
        public string IdProyect { get; set; }
        public string IdConfigurationNotifications { get; set; }
        public string Recipients { get; set; }
        public Nullable<DateTime> DataStart { get; set; }
        public Nullable<DateTime> DateEnd { get; set; }
        public string Status { get; set; }
        public string TypeSend { get; set; }
        public string QueryREST { get; set; }
        public string DatasJSON { get; set; }
    }
}
