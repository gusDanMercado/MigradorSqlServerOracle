namespace MigradorSqlServerOracle.Models.Oracle;

public class DepositosOracle
{
    public string DepositosDescrip { get; set; } = string.Empty;

    public string DepositosCodigo { get; set; } = string.Empty;

    public int? LOCALIDADESID { get; set; }

    public string? DepositosTelefono { get; set; }
}