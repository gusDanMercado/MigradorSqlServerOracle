namespace MigradorSqlServerOracle.Models.Oracle;

public class ExistenciasOracle
{
    public string ExistenciasProductoCodi { get; set; } = string.Empty;

    public string ExistenciasProductoDescr { get; set; } = string.Empty;

   public int ExistenciasCantidad { get; set; }

    public string ExistenciasDepositoCodi { get; set; } = string.Empty;

    public string ExistenciasDepositoDescr { get; set; } = string.Empty;

    public string ExistenciasUbicacionCodi { get; set; } = string.Empty;

    public decimal ExistenciasCostoUltiCompra { get; set; }

    public DateTime ExistenciasFchUltiCompra { get; set; } 
    public string ExistenciasCuentaCodi { get; set; } = string.Empty;      
}