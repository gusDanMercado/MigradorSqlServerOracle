namespace MigradorSqlServerOracle.Models.Oracle;

public class SubGruposProducOracle
{
    public int GRUPOSPRODUCTOID { get; set; }  // GRUPOSPRODUCTOID (FK)

    public string SUBGRUPOSPRODUCCODIGO { get; set; } = string.Empty;

    public string SUBGRUPOSPRODUCDESCRIP { get; set; } = string.Empty;
}