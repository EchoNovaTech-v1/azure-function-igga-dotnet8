namespace AppFunctions.models
{
    public class ConfigurationMenu
    {
        public string name { get; set; }
        public string idMenu { get; set; }
        public string idParent { get; set; }
        public string route { get; set; }
        public string type { get; set; }
        public string icon { get; set; }
        public bool defect { get; set; }
        public int level { get; set; }
        public bool dragAndDrog { get; set; }
        public bool active { get; set; }
        public int numOrder { get; set; }
    }
    
}