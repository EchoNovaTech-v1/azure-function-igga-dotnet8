namespace AppFunctions.models
{
    public class Resource
    {
        public string idResource { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public bool hereditary { get; set; }
        public string idParent { get; set; }
        public bool active { get; set; }
    }
}
