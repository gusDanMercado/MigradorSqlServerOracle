namespace MigradorSqlServerOracle.Models.Oracle;

public class ProveedorOracle
{
    public string nombre { get; set; } = string.Empty;

    public string cuit { get; set; } = string.Empty;

    public int? iva_cate { get; set; }

    public bool iva_percep { get; set; }   

    public string? ingre_brutos { get; set; }

    public string? obser { get; set; } 

    public int? agru_1 { get; set; }

    public int? agru_2 { get; set; }    

    public int? agru_3 { get; set; }

    public int? agru_4 { get; set; }    
    
    public string? socie_juri { get; set; }

    public string? nume_cai { get; set; }    
}