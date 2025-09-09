namespace AppFunctions.models
{
    public class OrganizationalUnit
    {
    public string idOrganizationalUnit { get; set; }
      public string name { get; set; }
      public string level { get; set; }
      public string idOrganizationType { get; set; }
      public string idParent { get; set; }
      public bool active { get; set; }
  }
}
