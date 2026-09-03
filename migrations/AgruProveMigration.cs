using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class AgruProveMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public AgruProveMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración PROVE_AGRU -> PROVE_AGRU.");

        var proveagruOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.",
            proveagruOrigen.Count);

        var proveagruDestino = Transformar(proveagruOrigen);

        await InsertarOracleAsync(proveagruDestino);

        Log.Information(
            "Migración PROVE_AGRU -> AGRUPROVE finalizada correctamente.");
    }

    private async Task<List<AgruProveSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<AgruProveSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT * FROM prove_agru1
            UNION ALL
            SELECT * FROM prove_agru2
            UNION ALL
            SELECT * FROM prove_agru3
            UNION ALL
            SELECT * FROM prove_agru4
        """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var agruprove = new AgruProveSqlServer
            {
                Codi = reader["codi"]
                    .ToString()!
                    .Trim(),

                Descrip = reader["descrip"]
                    .ToString()!
                    .Trim(),
            };

            resultado.Add(agruprove);
        }

        return resultado;
    }

    private List<AgruProveOracle> Transformar(
        List<AgruProveSqlServer> origen)
    {
        var resultado = new List<AgruProveOracle>();

        foreach (var item in origen)
        {
            var agruprove = new AgruProveOracle
            {
                Codi = item.Codi.Trim(),
                Descrip = item.Descrip.Trim(),
            };
            resultado.Add(agruprove);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<AgruProveOracle> agruprove)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();
        try
        {
            const string sql = """
                INSERT INTO AGRUPROVE
                (
                    AGRUPROVECODIGO,                
                    AGRUPROVEDESCRIP,
                    AGRUPROVEFCHALTA,
                    AGRUPROVEFCHBAJA,
                    AGRUPROVEUSUARIOSIDALTA,
                    AGRUPROVEUSUARIOSIDBAJA
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

            foreach (var agrpro in agruprove)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "Codi",
                    OracleDbType.NVarchar2
                ).Value = agrpro.Codi;

                command.Parameters.Add(
                    "Descrip",
                    OracleDbType.NVarchar2
                ).Value = agrpro.Descrip;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "AgruProve {Codigo} - {Descripcion} insertada.",
                    agrpro.Codi,
                    agrpro.Descrip);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en AGRUPROVE.",
                agruprove.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de AGRUPROVE.");
            throw;
        }
    }
}