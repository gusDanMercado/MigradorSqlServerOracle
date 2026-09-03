namespace MigradorSqlServerOracle.Models.SqlServer;

public class PlanCuentasRelaOracle
{
    public int PresupuestosId { get; set; }   // PRESUPUESTOSID (FK)

    public int STPCuentasCuentasRelaId1 { get; set; }   // PLANCUENTASID (FK)

    public int STPCuentasCuentasRelaId2 { get; set; }  // PLANCUENTASID (FK)
}