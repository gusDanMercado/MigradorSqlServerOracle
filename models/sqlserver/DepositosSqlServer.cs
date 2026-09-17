namespace MigradorSqlServerOracle.Models.SqlServer;

public class DepositosSqlServer
{
    public string descrip { get; set; } = string.Empty;

    public string depo { get; set; } = string.Empty;

    public string? loca { get; set; }

    public string? tele { get; set; }
}