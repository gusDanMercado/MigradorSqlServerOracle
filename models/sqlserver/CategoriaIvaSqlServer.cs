namespace MigradorSqlServerOracle.Models.SqlServer;

public class CategoriaIvaSqlServer
{
    public string Codi { get; set; } = string.Empty;

    public string Descrip { get; set; } = string.Empty;

    public bool Discrimina { get; set; }

    public string? LlevaCuit { get; set; }
}