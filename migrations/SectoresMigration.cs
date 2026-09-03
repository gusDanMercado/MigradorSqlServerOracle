using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class SectoresMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public SectoresMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración SECTORES -> SECTORES.");

        var sectoresOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.",
            sectoresOrigen.Count);

        var sectoresDestino = Transformar(sectoresOrigen);

        await InsertarOracleAsync(sectoresDestino);

        Log.Information(
            "Migración SECTORES -> SECTORES finalizada correctamente.");
    }

    private async Task<List<SectoresSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<SectoresSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT *
            FROM SECTORES
        """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var sectores = new SectoresSqlServer
            {
                Codi = reader["codi"]
                    .ToString()!
                    .Trim(),

                Descrip = reader["descrip"]
                    .ToString()!
                    .Trim(),
            };

            resultado.Add(sectores);
        }

        return resultado;
    }

    private List<SectoresOracle> Transformar(
        List<SectoresSqlServer> origen)
    {
        var resultado = new List<SectoresOracle>();

        foreach (var item in origen)
        {
            var sectores = new SectoresOracle
            {
                Codi = item.Codi.Trim(),
                Descrip = item.Descrip.Trim(),
            };
            resultado.Add(sectores);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<SectoresOracle> sectores)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO SECTORES
                (
                    SECTORESCODIGO,
                    SECTORESDESCRIP,
                    SECTORESFCHALTA,
                    SECTORESFCHBAJA,
                    SECTORESUSUARIOSIDALTA,
                    SECTORESUSUARIOSIDBAJA
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

            foreach (var sector in sectores)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "Codi",
                    OracleDbType.NVarchar2
                ).Value = sector.Codi;

                command.Parameters.Add(
                    "Descrip",
                    OracleDbType.NVarchar2
                ).Value = sector.Descrip;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "Sector {Codigo} - {Descripcion} insertado.",
                    sector.Codi,
                    sector.Descrip);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en SECTORES.",
                sectores.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de SECTORES.");
            throw;
        }
    }
}