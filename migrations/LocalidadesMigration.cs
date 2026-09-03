using Microsoft.Data.SqlClient;

using MigradorSqlServerOracle.Database.SqlServer;

using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;

using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class LocalidadesMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;

    private readonly OracleDb _oracleDatabase;

    public LocalidadesMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;        
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración GESTION.LOCALIDADES -> LOCALIDADES.");

        var localidadesOrigen =
            await LeerOracleOrigenAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde GESTION.LOCALIDADES.",
            localidadesOrigen.Count);

        // 2. Códigos desde SQL Server
        var codigosLocalidades =
            await LeerCodigosLocalidadesSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} códigos desde SQL Server NUEVAS_LOCALIDADES.",
            codigosLocalidades.Count);

        // 3. Relacionar código con cada localidad
        foreach (var localidad in localidadesOrigen)
        {
            if (codigosLocalidades.TryGetValue(
                    localidad.DESC_LOC.Trim(),
                    out var codigoCorto))
            {
                localidad.CODIGOCORTO = codigoCorto;
            }
            else
            {
                localidad.CODIGOCORTO = null;
            }
        }

        await InsertarOracleAsync(localidadesOrigen);

        Log.Information(
            "Migración GESTION.LOCALIDADES -> LOCALIDADES finalizada correctamente.");
    }

    private async Task<int?> ObtenerUnidadOperativaIdAsync(
        OracleConnection connection,
        string? CODIGO_AM)
    {
        if (string.IsNullOrWhiteSpace(CODIGO_AM))
            return null;

        const string sql = """
            SELECT 
                UNIDADESOPERATIVASID
            FROM GXCONTABLE.UNIDADESOPERATIVAS
            WHERE UPPER(TRIM(UNIDADESOPERATIVASCODIGO)) = UPPER(TRIM(:CODIGO_AM))
        """;

        await using var command =
            new OracleCommand(sql, connection);

        command.BindByName = true;

        command.Parameters.Add(
            "CODIGO_AM",
            OracleDbType.Varchar2
        ).Value = CODIGO_AM.Trim().ToUpperInvariant();

        var resultado =
            await command.ExecuteScalarAsync();

        if (resultado == null ||
            resultado == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt32(resultado);
    }

    private async Task<List<LocalidadesOracle>> LeerOracleOrigenAsync()
    {
        var resultado =
            new List<LocalidadesOracle>();

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT
                L.ID_LOC,
                L.DESC_LOC,
                L.COD_POSTAL,
                UO.CODIGO_AM
            FROM GESTION.LOCALIDADES L
            LEFT JOIN GESTION.UNIDADOPERATIVA UO ON L.ID_UOP = UO.ID_UOP  
            WHERE ID_PROV = 1
            ORDER BY ID_LOC
        """;

        await using var command =
            new OracleCommand(sql, connection);

        command.BindByName = true;

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var localidad =
                new LocalidadesOracle
                {
                    DESC_LOC =
                        reader["DESC_LOC"] == DBNull.Value
                            ? string.Empty
                            : reader["DESC_LOC"]
                                .ToString()!
                                .Trim(),

                    CODIGO_AM =
                        reader["CODIGO_AM"] == DBNull.Value
                            ? null
                            : reader["CODIGO_AM"]
                                .ToString()!
                                .Trim(),

                    COD_POSTAL =
                        reader["COD_POSTAL"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(
                                reader["COD_POSTAL"]),

                    ID_LOC =
                        reader["ID_LOC"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(
                                reader["ID_LOC"])
                };

            resultado.Add(localidad);
        }

        return resultado;
    }

private async Task<Dictionary<string, string?>> LeerCodigosLocalidadesSqlServerAsync()
{
    var resultado =
        new Dictionary<string, string?>(
            StringComparer.OrdinalIgnoreCase);

    await using var connection =
        _sqlServerDatabase.CrearConexion();

    await connection.OpenAsync();

    const string sql = """
        SELECT
            REPLACE(auxi, 'LOCAL', '') AS codigocorto,
            descrip
        FROM AUXILIARES 
        WHERE 
            UPPER(auxi_tipo) LIKE '%LOCAL%'
        ORDER BY auxi
    """;

    await using var command =
        connection.CreateCommand();

    command.CommandText = sql;

    await using var reader =
        await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        var codigoCorto =
            reader["codigocorto"] == DBNull.Value
                ? null
                : reader["codigocorto"]
                    .ToString()!
                    .Trim();

        var descripcion =
            reader["descrip"] == DBNull.Value
                ? null
                : reader["descrip"]
                    .ToString()!
                    .Trim();

        if (!string.IsNullOrWhiteSpace(descripcion))
        {
            resultado[descripcion] = codigoCorto;
        }
    }

    return resultado;
}    

    private async Task InsertarOracleAsync(
        List<LocalidadesOracle> localidades)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO LOCALIDADES
                (
                    LOCALIDADESDESCRIP,
                    UNIDADESOPERATIVASID,
                    LOCALIDADESCODPOSTAL,
                    LOCALIDADESIDGESP,
                    LOCALIDADESCODIGO,
                    LOCALIDADESFCHALTA,
                    LOCALIDADESFCHBAJA,
                    LOCALIDADESUSUALTA,
                    LOCALIDADESUSUBAJA
                )
                VALUES
                (
                    :DESC_LOC,
                    :UNIDADESOPERATIVASID,
                    :COD_POSTAL,
                    :ID_LOC,
                    :CODIGOCORTO,
                    SYSDATE,
                    NULL,
                    1,
                    NULL
                )
            """;

            foreach (var localidad in localidades)
            {
                // =====================================================
                // Buscar UNIDADESOPERATIVASID mediante CODIGO_AM
                // Si CODIGO_AM es NULL/vacío, devuelve NULL
                // =====================================================
                var unidadOperativaId =
                    await ObtenerUnidadOperativaIdAsync(
                        connection,
                        localidad.CODIGO_AM);

                // Si vino un CODIGO_AM pero no encontramos equivalencia,
                // detenemos la migración.
                if (!string.IsNullOrWhiteSpace(localidad.CODIGO_AM)
                    && !unidadOperativaId.HasValue)
                {
                    throw new Exception(
                        $"No se encontró una Unidad Operativa para " +
                        $"CODIGO_AM = '{localidad.CODIGO_AM}' " +
                        $"de la localidad ID_LOC = {localidad.ID_LOC} " +
                        $"({localidad.DESC_LOC}).");
                }

                await using var command =
                    new OracleCommand(
                        sql,
                        connection);

                command.Transaction =
                    transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "DESC_LOC",
                    OracleDbType.NVarchar2
                ).Value =
                    string.IsNullOrWhiteSpace(
                        localidad.DESC_LOC)
                        ? DBNull.Value
                        : localidad.DESC_LOC;

                command.Parameters.Add(
                    "UNIDADESOPERATIVASID",
                    OracleDbType.Int32
                ).Value =
                    unidadOperativaId.HasValue
                        ? unidadOperativaId.Value
                        : DBNull.Value;

                command.Parameters.Add(
                    "COD_POSTAL",
                    OracleDbType.Int32
                ).Value =
                    localidad.COD_POSTAL.HasValue
                        ? localidad.COD_POSTAL.Value
                        : DBNull.Value;

                command.Parameters.Add(
                    "ID_LOC",
                    OracleDbType.Int32
                ).Value =
                    localidad.ID_LOC.HasValue
                        ? localidad.ID_LOC.Value
                        : DBNull.Value;

                command.Parameters.Add(
                    "CODIGOCORTO",
                    OracleDbType.Varchar2
                ).Value =
                    string.IsNullOrWhiteSpace(localidad.CODIGOCORTO)
                        ? DBNull.Value
                        : localidad.CODIGOCORTO;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "Localidad origen {IdOrigen} - {Descripcion} insertada.",
                    unidadOperativaId,
                    localidad.DESC_LOC);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en LOCALIDADES.",
                localidades.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de LOCALIDADES.");

            throw;
        }
    }
}