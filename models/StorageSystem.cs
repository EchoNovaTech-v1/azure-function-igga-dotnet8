namespace AppFunctions.models
{
    public class StorageSystem
  {
    public string provider{get;set;}
    public string idApplication { get; set; }
    public string idObject { get; set; }
    public string password { get; set; }
    public string apiEndpoint { get; set; }
    public bool active { get; set; }
  }
}
