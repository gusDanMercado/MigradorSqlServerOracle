namespace MigradorSqlServerOracle.Models.Oracle;

public class ProductosOracle
{
    public int UNIDADESMEDIDAID { get; set; }  // UNIDADESMEDIDAID (FK)

    public int SubGruposProducId { get; set; } // SubGruposProducId (FK)

   public int? PLANCUENTASID { get; set; }  // PLANCUENTASID (FK)

    public string PRODUCTOSCODIGO { get; set; } = string.Empty;

    public string PRODUCTOSDESCRIPCORTA { get; set; } = string.Empty;

    public string PRODUCTOSDESCRIPLARGA { get; set; } = string.Empty;

    public string PRODUCTOSTIPO { get; set; } = string.Empty;  
}