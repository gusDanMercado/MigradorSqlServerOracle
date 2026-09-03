namespace MigradorSqlServerOracle.Models.Oracle;

public class CategoriaIvaOracle
{
    public string Codi { get; set; } = string.Empty;

    public string Descrip { get; set; } = string.Empty;

    public bool Discrimina { get; set; }

    public bool LlevaCuit { get; set; }
}