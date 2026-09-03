using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class ComprobantesMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public ComprobantesMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración COMPROBANTES_COMPRA -> COMPROBANTES.");

        var ComprobantesOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.",
            ComprobantesOrigen.Count);

        var ComprobantesClase =
            await CargarComprobantesClaseAsync();
        Log.Information(
            "Se leyeron {Cantidad} registros desde Oracle (COMPROBANTESCLASE).", ComprobantesClase.Count);

        var GruposAutorizacion =
            await CargarGruposAutorizacionAsync();
        Log.Information(
            "Se leyeron {Cantidad} registros desde Oracle (GRUPOSAUTORIZACION).", GruposAutorizacion.Count);            

        var ComprobantesDestino = Transformar(ComprobantesOrigen, ComprobantesClase, GruposAutorizacion);

        await InsertarOracleAsync(ComprobantesDestino);

        Log.Information(
            "Migración COMPROBANTES_COMPRA -> COMPROBANTES finalizada correctamente.");
    }

    private async Task<List<ComprobantesSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<ComprobantesSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT 
                compro_tipo AS codi,  
                descrip, 
                COMPRO_CLASE AS clase, 
                GRUPO_AUTO AS grupoauto
            FROM COMPROBANTES_COMPRA        
        """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var Comprobantes = new ComprobantesSqlServer
            {
                codi = reader["codi"]
                    .ToString()!
                    .Trim(),

                descrip = reader["descrip"]
                    .ToString()!
                    .Trim(),

                clase =
                    reader["clase"] == DBNull.Value
                        ? string.Empty
                        : reader["clase"].ToString()?.Trim() ?? string.Empty,

                grupoauto =
                    reader["grupoauto"] == DBNull.Value
                        ? null
                        : reader["grupoauto"].ToString()?.Trim()                        
            };

            resultado.Add(Comprobantes);
        }

        return resultado;
    }

    private async Task<Dictionary<string, int>> CargarComprobantesClaseAsync()
    {
        var resultado = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT
                COMPROBANTESCLASECODIGO,
                COMPROBANTESCLASEID
            FROM COMPROBANTESCLASE
            """;

        await using var command =
            new OracleCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var codigo =
                reader["COMPROBANTESCLASECODIGO"]
                    .ToString()!
                    .Trim();

            var id =
                Convert.ToInt32(
                    reader["COMPROBANTESCLASEID"]);

            resultado[codigo] = id;
        }

        return resultado;
    }

    private async Task<Dictionary<string, int>> CargarGruposAutorizacionAsync()
    {
        var resultado = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT
                GRUPOSAUTORIZACIONCODIGO,
                GRUPOSAUTORIZACIONID
            FROM GRUPOSAUTORIZACION
            """;

        await using var command =
            new OracleCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var codigo =
                reader["GRUPOSAUTORIZACIONCODIGO"]
                    .ToString()!
                    .Trim();

            var id =
                Convert.ToInt32(
                    reader["GRUPOSAUTORIZACIONID"]);

            resultado[codigo] = id;
        }

        return resultado;
    }        

    private List<ComprobantesOracle> Transformar(
        List<ComprobantesSqlServer> origen,
        Dictionary<string, int> ComprobantesClase,
        Dictionary<string, int> GruposAutorizacion
    ){
        var resultado = new List<ComprobantesOracle>();

        foreach (var item in origen)
        {
            // BUSCAMOS COMPROBANTESCLASEID
            int? ComprobantesClaseId = null;

            if (!string.IsNullOrWhiteSpace(item.clase))
            {
                var ComprobantesClaseCodi = item.clase.Trim();

                if (ComprobantesClase.TryGetValue(
                        ComprobantesClaseCodi,
                        out var id))
                {
                    ComprobantesClaseId = id;
                }
                else
                {
                    Log.Warning(
                        "No se encontró la compro_clase {ComprobantesClase} " +
                        "para el comprobante {codi}. " +
                        "Se insertará COMPROBANTESCLASEID = NULL.",
                        ComprobantesClaseCodi,
                        item.codi
                    );
                }
            }            

            // BUSCAMOS GRUPOSAUTORIZACIONID
            int? GruposAutorizacionId = null;

            if (!string.IsNullOrWhiteSpace(item.grupoauto))
            {
                var GruposAutorizacionCodi = item.grupoauto.Trim();

                if (GruposAutorizacion.TryGetValue(
                        GruposAutorizacionCodi,
                        out var id))
                {
                    GruposAutorizacionId = id;
                }
                else
                {
                    Log.Warning(
                        "No se encontró la grupo_auto {GruposAutorizacion} " +
                        "para el comprobante {codi}. " +
                        "Se insertará GRUPOSAUTORIZACIONID = NULL.",
                        GruposAutorizacionCodi,
                        item.codi
                    );
                }
            }

            var Comprobantes = new ComprobantesOracle
            {
                codi = item.codi.Trim(),
                descrip = item.descrip.Trim(),
                clase = ComprobantesClaseId,
                grupoauto = GruposAutorizacionId
            };

            resultado.Add(Comprobantes);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<ComprobantesOracle> Comprobantes)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO COMPROBANTES
                (
                    COMPROBANTESCODIGO,
                    COMPROBANTESDESCRIP,
                    COMPROBANTESCLASEID,
                    GRUPOSAUTORIZACIONID,
                    COMPROBANTESFCHALTA,
                    COMPROBANTESFCHBAJA,
                    COMPROBANTESUSUARIOSIDALTA,
                    COMPROBANTESUSUARIOSIDBAJA
                )
                VALUES
                (
                    :codi,
                    :descrip,
                    :clase,
                    :grupoauto,
                    SYSDATE,
                    NULL,
                    1,
                    NULL
                )
                """;

            foreach (var Comprobante in Comprobantes)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "codi",
                    OracleDbType.NVarchar2
                ).Value = Comprobante.codi;

                command.Parameters.Add(
                    "descrip",
                    OracleDbType.NVarchar2
                ).Value = Comprobante.descrip;

                command.Parameters.Add(
                    "clase",
                    OracleDbType.Int32
                ).Value =
                    Comprobante.clase.HasValue
                        ? Comprobante.clase.Value
                        : DBNull.Value;                

                command.Parameters.Add(
                    "grupoauto",
                    OracleDbType.Int32
                ).Value =
                    Comprobante.grupoauto.HasValue
                        ? Comprobante.grupoauto.Value
                        : DBNull.Value; 

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "Comprobante {Codigo} - {Descripcion} insertado.",
                    Comprobante.codi,
                    Comprobante.descrip);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en COMPROBANTES.",
                Comprobantes.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de COMPROBANTES.");
            throw;
        }
    }
}