using MigradorSqlServerOracle.Models.Oracle;
using Serilog;

using OracleDb =
    MigradorSqlServerOracle.Database.Oracle.OracleDatabase;

using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class UnidadesOperativasMigration
{
    private readonly OracleDb _oracleDatabase;

    public UnidadesOperativasMigration(
        OracleDb oracleDatabase)
    {
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración MANTENIMIENTO.M_CENTROCOSTO -> UNIDADESOPERATIVAS.");

        var UnidadesOperativasOrigen =
            await LeerOracleOrigenAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde MANTENIMIENTO.M_CENTROCOSTO.",
            UnidadesOperativasOrigen.Count);

        await InsertarOracleAsync(UnidadesOperativasOrigen);

        Log.Information(
            "Migración MANTENIMIENTO.M_CENTROCOSTO -> UNIDADESOPERATIVAS finalizada correctamente.");
    }

    private async Task<List<UnidadesOperativasOracle>> LeerOracleOrigenAsync()
    {
        var resultado =
            new List<UnidadesOperativasOracle>();

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT DISTINCT(CODDISTRITO), DISTRITO  
            FROM MANTENIMIENTO.M_CENTROCOSTO
        """;

        await using var command =
            new OracleCommand(sql, connection);

        command.BindByName = true;

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var UnidadesOperativas =
                new UnidadesOperativasOracle
                {
                    CODDISTRITO =
                        reader["CODDISTRITO"] == DBNull.Value
                            ? string.Empty
                            : reader["CODDISTRITO"]
                                .ToString()!
                                .Trim(),

                    DISTRITO =
                        reader["DISTRITO"] == DBNull.Value
                            ? string.Empty
                            : reader["DISTRITO"]
                                .ToString()!
                                .Trim()                                                                
                };

            resultado.Add(UnidadesOperativas);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<UnidadesOperativasOracle> UnidadesOperativas)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO UNIDADESOPERATIVAS
                (
                    UNIDADESOPERATIVASDESCRIP,
                    UNIDADESOPERATIVASCODIGO,
                    UNIDADESOPERATIVASFCHALTA,
                    UNIDADESOPERATIVASFCHBAJA,
                    UNIDADESOPERATIVASUSUALTA,
                    UNIDADESOPERATIVASUSUBAJA
                )
                VALUES
                (
                    :DISTRITO,
                    :CODDISTRITO,
                    SYSDATE,
                    NULL,
                    1,
                    NULL
                )
            """;

            foreach (var unidadoperativa in UnidadesOperativas)
            {
                await using var command =
                    new OracleCommand(
                        sql,
                        connection);

                command.Transaction =
                    transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "DISTRITO",
                    OracleDbType.NVarchar2
                ).Value =
                    string.IsNullOrWhiteSpace(
                        unidadoperativa.DISTRITO)
                        ? DBNull.Value
                        : unidadoperativa.DISTRITO;

                command.Parameters.Add(
                    "CODDISTRITO",
                    OracleDbType.NVarchar2
                ).Value =
                    string.IsNullOrWhiteSpace(
                        unidadoperativa.CODDISTRITO)
                        ? DBNull.Value
                        : unidadoperativa.CODDISTRITO;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "Unidad Operativa distrito {distrito} - {Descripcion} insertada.",
                    unidadoperativa.DISTRITO,
                    unidadoperativa.CODDISTRITO);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en UNIDADESOPERATIVAS.",
                UnidadesOperativas.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de UNIDADESOPERATIVAS.");
            throw;
        }
    }
}