namespace MigradorSqlServerOracle.Models.SqlServer;

public class ExistenciasSqlServer
{
    public string produc { get; set; } = string.Empty;

    public string producDescrip { get; set; } = string.Empty;

   public int cant_depo { get; set; }

    public string depo { get; set; } = string.Empty;

    public string depoDescrip { get; set; } = string.Empty;

    public string ubi { get; set; } = string.Empty;

    public decimal costo_ulti_compra { get; set; }

    public DateTime fecha_ulti_compra { get; set; } 
    public string cuenta { get; set; } = string.Empty;        
}