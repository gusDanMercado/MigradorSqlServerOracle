namespace MigradorSqlServerOracle.Models.SqlServer;

public class AgruProveSqlServer
{
    public string Codi { get; set; } = string.Empty;

    public string Descrip { get; set; } = string.Empty;
}

/*
NOTAS:
    utilizar nombres como figuran exactamente en la tabla Origen y en caso de que usemos 
    otros nombres utilizar en la consulta que leer los datos el AS

    usar nombres de campos en minusculas todo el tiempo para evitar errores, en este caso
    no lo use porque me di cuenta despues de los errores que esto puede ocacionar

    string.Empty; se usa para indicar una cadena vacía ("") y asi NO USAR NULL

    tipo_de_dato? este signo de pregunta significa que este campo puede ser NULL
*/