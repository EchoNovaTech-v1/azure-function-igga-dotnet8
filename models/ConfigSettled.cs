namespace AppFunctions.models
{
    /// <summary>
    /// Clase que representa el modelo de acceso de los tipos de radicados
    /// </summary>
    public class ConfigSettled
    {
        public int maxSettled { get; set; }
        public int? initialSettled { get; set; }
        public int? autoIncrement { get; set; }
        public string dateRestart { get; set; }
        public string formatDate { get; set; }
        public string idWorkflow { get; set; }
        public bool active { get; set; }

    }
}
