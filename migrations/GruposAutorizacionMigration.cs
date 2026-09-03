using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class GruposAutorizacionMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public GruposAutorizacionMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración GRUPOS_AUTO -> GRUPOSAUTORIZACION.");

        var GruposAutorizacionOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.",
            GruposAutorizacionOrigen.Count);

        var GruposAutorizacionDestino = Transformar(GruposAutorizacionOrigen);

        await InsertarOracleAsync(GruposAutorizacionDestino);

        Log.Information(
            "Migración GRUPOS_AUTO -> GRUPOSAUTORIZACION finalizada correctamente.");
    }

    private async Task<List<GruposAutorizacionSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<GruposAutorizacionSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """SELECT CODI, DESCRIP FROM GRUPOS_AUTO""";

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var GruposAutorizacion = new GruposAutorizacionSqlServer
            {
                Codi = reader["codi"]
                    .ToString()!
                    .Trim(),

                Descrip = reader["descrip"]
                    .ToString()!
                    .Trim(),
            };

            resultado.Add(GruposAutorizacion);
        }

        return resultado;
    }

    private List<GruposAutorizacionOracle> Transformar(
        List<GruposAutorizacionSqlServer> origen)
    {
        var resultado = new List<GruposAutorizacionOracle>();

        foreach (var item in origen)
        {
            var GruposAutorizacion = new GruposAutorizacionOracle
            {
                Codi = item.Codi.Trim(),
                Descrip = item.Descrip.Trim(),
            };
            resultado.Add(GruposAutorizacion);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<GruposAutorizacionOracle> GruposAutorizacion)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO GRUPOSAUTORIZACION
                (
                    GRUPOSAUTORIZACIONCODIGO,
                    GRUPOSAUTORIZACIONDESCRIP,
                    GRUPOSAUTORIZACIONFCHALTA,
                    GRUPOSAUTORIZACIONFCHBAJA,
                    GRUPOSAUTORIZACIONUSUALTA,
                    GRUPOSAUTORIZACIONUSUBAJA
                )
                VALUES
                (
                    :Codi,
                    :Descrip,
                    SYSDATE,
                    NULL,
                    1,
                    NULL
                )
                """;

            foreach (var grupoautorizacion in GruposAutorizacion)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "Codi",
                    OracleDbType.NVarchar2
                ).Value = grupoautorizacion.Codi;

                command.Parameters.Add(
                    "Descrip",
                    OracleDbType.NVarchar2
                ).Value = grupoautorizacion.Descrip;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "grupoautorizacion {Codigo} - {Descripcion} insertado.",
                    grupoautorizacion.Codi,
                    grupoautorizacion.Descrip);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en GRUPOSAUTORIZACION.",
                GruposAutorizacion.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de GRUPOSAUTORIZACION.");
            throw;
        }
    }
}