namespace AppFunctions.models
{
    /// <summary>
    /// clase que representa el modelo de consulta de radicados
    /// </summary>
    public class Settled
    {
        public string idWorkflow { get; set; }
        public int year { get; set; }
        public int month { get; set; }
        public int day { get; set; }
        public int consecutive { get; set; }
    }
}
