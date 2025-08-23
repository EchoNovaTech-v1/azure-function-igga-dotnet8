namespace AppFunctions.models
{
    public class ConfigNotifications
    {
        public string idConfigurationNotification { get; set; }
        public string idTemplate { get; set; }
        public string domainEmail { get; set; }
        public string emailSender { get; set; }
        public string passwordSender { get; set; }
        public int frecuencyNotification { get; set; }
        public bool active { get; set; }
    }
}
