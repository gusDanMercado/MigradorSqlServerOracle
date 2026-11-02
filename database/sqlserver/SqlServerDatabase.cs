/*
// PRIMERA PRUEBA -> TODO OK!!!
using Microsoft.Data.SqlClient;

namespace MigradorSqlServerOracle.Database.SqlServer;

public class SqlServerDatabase
{
    private readonly string _connectionString;

    public SqlServerDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqlConnection CrearConexion()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<bool> ProbarConexionAsync()
    {
        try
        {
            await using var connection = CrearConexion();
            await connection.OpenAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }
}
*/

using Microsoft.Data.SqlClient;

namespace MigradorSqlServerOracle.Database.SqlServer;

public class SqlServerDatabase
{
    private readonly string _connectionString;

    public SqlServerDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqlConnection CrearConexion()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task ProbarConexionAsync()
    {
        await using var connection = CrearConexion();
        await connection.OpenAsync();
    }
}