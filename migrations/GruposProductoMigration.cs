using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class GruposProductoMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public GruposProductoMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración GRUPOS -> GRUPOSPRODUCTO.");

        var GruposProductoOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.",
            GruposProductoOrigen.Count);

        var GruposProductoDestino = Transformar(GruposProductoOrigen);

        await InsertarOracleAsync(GruposProductoDestino);

        Log.Information(
            "Migración GRUPOS -> GRUPOSPRODUCTO finalizada correctamente.");
    }

    private async Task<List<GruposProductoSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<GruposProductoSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT codigo, descrip, estado 
            FROM GRUPOS
        """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var GruposProducto = new GruposProductoSqlServer
            {
                codigo = reader["codigo"]
                    .ToString()!
                    .Trim(),

                descrip = reader["descrip"]
                    .ToString()!
                    .Trim(),

                estado = reader["estado"]
                    .ToString()!
                    .Trim(),                    
            };

            resultado.Add(GruposProducto);
        }

        return resultado;
    }

    private List<GruposProductoOracle> Transformar(
        List<GruposProductoSqlServer> origen)
    {
        var resultado = new List<GruposProductoOracle>();

        foreach (var item in origen)
        {
            var GruposProducto = new GruposProductoOracle
            {
                GRUPOSPRODUCTOCODIGO = item.codigo.Trim(),
                GRUPOSPRODUCTODESCRIP = item.descrip.Trim(),
                GRUPOSPRODUCTOESTADO = item.estado.Trim(),
            };
            resultado.Add(GruposProducto);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<GruposProductoOracle> GruposProducto)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO GRUPOSPRODUCTO
                (
                    GRUPOSPRODUCTOCODIGO,
                    GRUPOSPRODUCTODESCRIP,
                    GRUPOSPRODUCTOESTADO,
                    GRUPOSPRODUCTOFCHALTA,
                    GRUPOSPRODUCTOFCHBAJA,
                    GRUPOSPRODUCTOUSUALTA,
                    GRUPOSPRODUCTOUSUBAJA
                )
                VALUES
                (
                    :GRUPOSPRODUCTOCODIGO,
                    :GRUPOSPRODUCTODESCRIP,
                    :GRUPOSPRODUCTOESTADO,
                    SYSDATE,
                    NULL,
                    1,
                    NULL
                )
                """;

            foreach (var grupo in GruposProducto)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "GRUPOSPRODUCTOCODIGO",
                    OracleDbType.NVarchar2
                ).Value = grupo.GRUPOSPRODUCTOCODIGO;

                command.Parameters.Add(
                    "GRUPOSPRODUCTODESCRIP",
                    OracleDbType.NVarchar2
                ).Value = grupo.GRUPOSPRODUCTODESCRIP;

                command.Parameters.Add(
                    "GRUPOSPRODUCTOESTADO",
                    OracleDbType.NVarchar2
                ).Value = grupo.GRUPOSPRODUCTOESTADO;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "Grupo {Codigo} - {Descripcion} insertado.",
                    grupo.GRUPOSPRODUCTOCODIGO,
                    grupo.GRUPOSPRODUCTODESCRIP);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en GRUPOSPRODUCTO.",
                GruposProducto.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de GRUPOSPRODUCTO.");
            throw;
        }
    }
}