namespace MigradorSqlServerOracle.Models.Oracle;

public class PlanCuentasOracle
{
    public int PlanCuentasId { get; set; }
    
    public string PlanCuentasDescrip { get; set; } = string.Empty;

    public string PlanCuentasDescripRedu { get; set; } = string.Empty;

    public string PlanCuentasTipo { get; set; } = string.Empty;

    public bool PlanCuentasMoneMarca { get; set; }

    public DateTime PlanCuentasFchAlta { get; set; }

    public bool PlanCuentasAjusMarca { get; set; }

    public int? PlanCuentasAjusCuenta { get; set; }

    public string? PlanCuentasAgru1 { get; set; }

    public string? PlanCuentasAgru2 { get; set; }

    public string? PlanCuentasAgru3 { get; set; }

    public string? PlanCuentasAgru4 { get; set; }

    public bool? PlanCuentasPresuFinan { get; set; }
}