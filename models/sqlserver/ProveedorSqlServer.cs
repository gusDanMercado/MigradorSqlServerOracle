namespace MigradorSqlServerOracle.Models.SqlServer;

public class ProveedorSqlServer
{
    public string nombre { get; set; } = string.Empty;

    public string cuit { get; set; } = string.Empty;

    public string? iva_cate { get; set; } = string.Empty;

    public bool iva_percep { get; set; } 

    public string? ingre_brutos { get; set; } 

    public string? obser { get; set; } 

    public string? agru_1 { get; set; }

    public string? agru_2 { get; set; }    

    public string? agru_3 { get; set; }

    public string? agru_4 { get; set; }    
    
    public string? socie_juri { get; set; }

    public string? nume_cai { get; set; }
}