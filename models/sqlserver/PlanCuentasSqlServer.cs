namespace MigradorSqlServerOracle.Models.SqlServer;

public class PlanCuentasSqlServer
{
    public string cuenta { get; set; } = string.Empty;

    public string nombre { get; set; } = string.Empty;

    public string nombre_redu { get; set; } = string.Empty;

    public string tipo { get; set; } = string.Empty;

    public int mone_marca { get; set; }

    public DateTime alta_fecha { get; set; }

    public int ajus_marca { get; set; }

    public string? ajus_cuenta { get; set; }

    public string? agru_1 { get; set; }

    public string? agru_2 { get; set; } 

    public string? agru_3 { get; set; } 
 
    public string? agru_4 { get; set; } 

    public string? presu_finan { get; set; }
}