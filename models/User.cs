namespace AppFunctions.models
{
    public class User
    {
        public string idAuth { get; set; }
        public string name { get; set; }
        public string lastName { get; set; }
        public string jobTitle { get; set; }
        public string email { get; set; }
        public string mobilePhone { get; set; }
        
        //Campo indicará con que cuenta se logueo el usuario
        // google o microsoft
        public string accessAccount { get; set; }
        public bool active { get; set; }
        
    }
}
