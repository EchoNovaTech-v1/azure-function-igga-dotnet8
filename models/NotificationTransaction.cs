namespace AppFunctions.models
{
    public class NotificationTransaction
  {
    public string idNotificationTransaction { get;set;}
    public string idStateTransaction { get;set;}
    public string to { get;set;}
    public string cc { get;set;}
    public string subject { get;set;}
    public int eventTime { get;set;}
    public string message { get;set;}
    public bool active { get;set;}
  }
}
