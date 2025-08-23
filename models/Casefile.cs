namespace AppFunctions.models
{
    public class Casefile
    {
        public string idOrganizationalUnit { get; set; }
        public string idStorageUnit { get; set; }
        public string idSerie { get; set; }
        public string idSubserie { get; set; }
        public string idStatusCasefile { get; set; }
        public string idArchive { get; set; }
        public string title { get; set; }
        public int documentNumber { get; set; }
        public DateTime createdDate { get; set; }
        public DateTime closedDate { get; set; }
        public string owner { get; set; }
        public string responsable { get; set; }
        public string sign { get; set; }
        public string method { get; set; }
        public int folderNumber { get; set; }
        public string[] keywords { get; set; }
        public string url { get; set; }
        public bool active { get; set; }
        //Campos para los post
        public string partition { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public string field { get; set; }
        public string email { get; set; }
    public string userClosed { get; set; }
  }
}
