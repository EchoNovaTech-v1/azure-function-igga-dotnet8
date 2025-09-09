namespace AppFunctions.models
{
    public class Permission
    {
        public string idPermission { get; set; }
        public string idResource { get; set; }
        public string partitionName { get; set; }
        public string idRecord { get; set; }
        public string idRole { get; set; }
        public bool active { get; set; }
  }
}
