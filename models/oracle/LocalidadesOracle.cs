namespace MigradorSqlServerOracle.Models.Oracle;

public class LocalidadesOracle
{
    public string DESC_LOC { get; set; } = string.Empty;

    public string? CODIGO_AM { get; set; } 
    // es la ID_UOP (PERO ME LLEGA EL CODIGO_AM Y LO TENGO QUE 
    // BUSCAR EN UNIDADESOPERATIVASCODIGO)

    public int? UNIDADESOPERATIVASID { get; set; }

    public int? COD_POSTAL { get; set; }

    public int? ID_LOC { get; set; }

    public string? CODIGOCORTO { get; set; }    
}