namespace AppFunctions.models
{
    public class ConfidentialDocument
  {
    public string idConfidencialDocument { get; set; }
    public string idStorageUnit { get; set; }
    public string settled { get; set; }
    public DateTime settledDate { get; set; }
    public string userCreation { get; set; }
    public string userModification { get; set; }
    public DateTime createdDate { get; set; }
    public string userCancellation { get; set; }
    public DateTime? canceledDate { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public string idSender { get; set; }
    public string idReceptor { get; set; }
    public string idSenderCorporate { get; set; }
    public string idReceptorCorporate { get; set; }
    public string[] idSenders { get; set; }
    public string[] idReceptors { get; set; }
    public string[] idSendersCorporate { get; set; }
    public string[] idReceptorsCorporate { get; set; }
    public bool canceled { get; set; }
  }
}
