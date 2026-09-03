namespace MigradorSqlServerOracle.Models.Oracle;

public class ComprobantesOracle
{
    public string codi { get; set; } = string.Empty;

    public string descrip { get; set; } = string.Empty;

    public int? clase { get; set; }

    public int? grupoauto { get; set; } 
}