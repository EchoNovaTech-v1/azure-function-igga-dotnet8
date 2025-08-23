namespace AppFunctions.models
{
    public class StateTransaction
    {
        public string idStateTransaction { get; set; }
        public string idInitialState { get; set; }
        public string idFinalState { get; set; }
        public int time { get; set; }
        public bool automatic { get; set; }
        public string idOrganizationalUnit {get; set;}
        public string idSerie { get; set; }
        public string idSubserie { get; set; }
        public bool active { get; set; }
	}
}
