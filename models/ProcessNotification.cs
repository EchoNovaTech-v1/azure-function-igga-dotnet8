using AppFunctions.entities;
using Microsoft.Azure.WebJobs.Host;

namespace AppFunctions.models
{
    public class ProcessNotification
    {
    public StateTransactionEntity stateTransactionEntity { get; set; }
    public List<CasefileEntity> casefileEntities { get; set; }
    public TraceWriter log { get; set; }
    public NotificationTransactionEntity notificationTransactionEntity { get; set; }
  }
}
