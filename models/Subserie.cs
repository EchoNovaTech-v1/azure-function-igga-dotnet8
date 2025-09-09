namespace AppFunctions.models
{
    public class Subserie
    {
      public string idOrganizationalUnit { get; set; }
      public string idSerie { get; set; }
      public string name { get; set; }
      public string description { get; set; }
      public string[] idsDisposicionFinal { get; set; }
      public string[] idsFileEvaluation { get; set; }
      public bool active { get; set; }
  }
}
