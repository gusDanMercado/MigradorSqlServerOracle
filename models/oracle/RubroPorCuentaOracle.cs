namespace MigradorSqlServerOracle.Models.SqlServer;

public class RubroPorCuentaOracle
{
    public int PresuEstrucInfoId { get; set; }   // PresuEstrucInfoId (FK)

    public int PlanCuentasId { get; set; }   // PlanCuentasId (FK)

    public string RubroPorCuentaDescrip { get; set; } = string.Empty;
}