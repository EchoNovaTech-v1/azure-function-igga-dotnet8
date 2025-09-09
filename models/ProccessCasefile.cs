using AppFunctions.entities;

namespace AppFunctions.models
{
    public class ProccessCasefile
    {
    public StateTransactionEntity stateTransactionEntity { get; set; }
    public List<CasefileEntity> casefileEntities { get; set; }
   }
}
