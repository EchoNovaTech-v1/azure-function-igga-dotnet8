namespace AppFunctions.models
{
    public class Audit
    {
    public string tableName { get; set; }
    public string partitionName { get; set; }
    public string idUser { get; set; }
    public string idResource { get; set; }
    public string idAction { get; set; }
    public string idRol { get; set; }
    public string idDirectory { get; set; }
    public DateTime createdDate { get; set; }
  }
}
