namespace MigradorSqlServerOracle.Models.Oracle;
public class FuentesAbastecimientoOracle
{
    public string DENOMINACION { get; set; } = string.Empty;
    public int ID_TIPO_FUENTE { get; set; }
    public int ID_LOC { get; set; }
    public int? NIS { get; set; }
    public double? LAT { get; set; }    
    public double? LNG { get; set; }            
}