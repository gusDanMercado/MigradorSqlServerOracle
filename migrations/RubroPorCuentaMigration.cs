using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class RubroPorCuentaMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public RubroPorCuentaMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración INFOR_ESTRUC_AUXI_PRESU -> RUBROPORCUENTA.");

        var RubroPorCuentaOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.",
            RubroPorCuentaOrigen.Count);

        var PresuEstrucInfo =
            await CargarPresuEstrucInfoAsync();
        Log.Information(
            "Se leyeron {Cantidad} registros desde Oracle (PresuEstrucInfo).", PresuEstrucInfo.Count);

        var RubroPorCuentaDestino = Transformar(RubroPorCuentaOrigen, PresuEstrucInfo);

        await InsertarOracleAsync(RubroPorCuentaDestino);

        Log.Information(
            "Migración INFOR_ESTRUC_AUXI_PRESU -> RUBROPORCUENTA finalizada correctamente.");
    }

    private async Task<List<RubroPorCuentaSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<RubroPorCuentaSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT rubro, cuenta, descrip 
            FROM INFOR_ESTRUC_AUXI_PRESU
        """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var RubroPorCuenta = new RubroPorCuentaSqlServer
            {
                rubro = reader["rubro"]
                    .ToString()!
                    .Trim(),

                cuenta = reader["cuenta"]
                    .ToString()!
                    .Trim(),

                descrip = reader["descrip"]
                    .ToString()!
                    .Trim(),                    
            };

            resultado.Add(RubroPorCuenta);
        }

        return resultado;
    }

    private async Task<Dictionary<string, int>> CargarPresuEstrucInfoAsync()
    {
        var resultado = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT
                PRESUESTRUCINFORUBRO,
                PRESUESTRUCINFOID
            FROM PRESUESTRUCINFO
        """;

        await using var command =
            new OracleCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var codigo =
                reader["PRESUESTRUCINFORUBRO"]
                    .ToString()!
                    .Trim();

            var id =
                Convert.ToInt32(
                    reader["PRESUESTRUCINFOID"]);

            resultado[codigo] = id;
        }

        return resultado;
    }
    private static int ObtenerPresuEstrucInfoId(
        string codigo,
        Dictionary<string, int> PresuEstrucInfo)
    {

        if (PresuEstrucInfo.TryGetValue(
                codigo.Trim(),
                out var id))
        {
            return id;
        }

        throw new InvalidOperationException(
            $"No se encontró PresuEstrucInfo con código '{codigo}'."
        );
    }      

    private List<RubroPorCuentaOracle> Transformar(
        List<RubroPorCuentaSqlServer> origen,
        Dictionary<string, int> PresuEstrucInfo
    ){
        var resultado = new List<RubroPorCuentaOracle>();

        foreach (var item in origen)
        {
            var RubroPorCuenta = new RubroPorCuentaOracle
            {
                PresuEstrucInfoId = ObtenerPresuEstrucInfoId(item.rubro, PresuEstrucInfo),
                PlanCuentasId = Convert.ToInt32(item.cuenta),
                RubroPorCuentaDescrip = item.descrip.Trim(),
            };
            resultado.Add(RubroPorCuenta);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<RubroPorCuentaOracle> RubroPorCuenta)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO RUBROPORCUENTA
                (
                    PRESUESTRUCINFOID,                        
                    PLANCUENTASID,
                    RUBROPORCUENTADESCRIP,
                    RUBROPORCUENTAFCHALTA,
                    RUBROPORCUENTAFCHBAJA,
                    RUBROPORCUENTAUSUARIOIDA,
                    RUBROPORCUENTAUSUARIOIDB
                )
                VALUES
                (
                    :PresuEstrucInfoId,
                    :PlanCuentasId,
                    :RubroPorCuentaDescrip,
                    SYSDATE,
                    NULL,
                    1,
                    NULL
                )
                """;

            foreach (var rubro in RubroPorCuenta)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "PresuEstrucInfoId",
                    OracleDbType.Int32
                ).Value = rubro.PresuEstrucInfoId;

                command.Parameters.Add(
                    "PlanCuentasId",
                    OracleDbType.Int32
                ).Value = rubro.PlanCuentasId;                

                command.Parameters.Add(
                    "RubroPorCuentaDescrip",
                    OracleDbType.NVarchar2
                ).Value = rubro.RubroPorCuentaDescrip;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "Rubro {PlanCuentasId} - {RubroPorCuentaDescrip} insertado.",
                    rubro.PlanCuentasId,
                    rubro.RubroPorCuentaDescrip);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en RUBROPORCUENTA.",
                RubroPorCuenta.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de RUBROPORCUENTA.");
            throw;
        }
    }
}