namespace AppFunctions.models
{
    public class Document
    {
    public string idDocument { get; set; }
    public string settled { get; set; }
    public DateTime settledDate { get; set; }
    public string documentName { get; set; }
    public string documentDescription { get; set; }
    public DateTime createdDate { get; set; }
    public DateTime dateAddedCaseFile { get; set; }
    public bool canceled { get; set; }
    public string footprintValue { get; set; }
    public string summaryFunction { get; set; }
    public int documentOrder { get; set; }
    public int startPage { get; set; }
    public int endPage { get; set; }
    public string format { get; set; }
    public string size { get; set; }
    public string origin { get; set; }
    public string idCasefile { get; set; }
    public string idDocumentalType { get; set; }
    public string url { get; set; }
    public string[] senders { get; set; }
    public string[] receptors { get; set; }
    public bool hasReturn { get; set; }
    public bool itReturn { get; set; }
    public string filedCorresponds { get; set; }
    public string documentFlow { get; set; }
    public string userCreation { get; set; }
    public string userModification { get; set; }
    public string userCancellation { get; set; }
    public DateTime? canceledDate { get; set; }
    public string idSender { get; set; }
    public string idReceptor { get; set; }
    public string idSenderCorporate { get; set; }
    public string idReceptorCorporate { get; set; }
    public string[] idSenders { get; set; }
    public string[] idReceptors { get; set; }
    public string[] idSendersCorporate { get; set; }
    public string[] IdReceptorsCorporate { get; set; }
  }
}
