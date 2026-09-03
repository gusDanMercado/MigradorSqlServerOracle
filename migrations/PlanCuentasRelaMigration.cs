using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;
using System.Globalization;

namespace MigradorSqlServerOracle.Migrations;

public class PlanCuentasRelaMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public PlanCuentasRelaMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración PLAN_CUENTAS_RELA -> PLANCUENTASRELA.");

        var PlanCuentasRelaOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.",
            PlanCuentasRelaOrigen.Count);

        var Presupuestos =
            await CargarPresupuestosAsync();
        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server (Presupuestos).", Presupuestos.Count);

        var PlanCuentasRelaDestino = Transformar(PlanCuentasRelaOrigen, Presupuestos);

        await InsertarOracleAsync(PlanCuentasRelaDestino);

        Log.Information(
            "Migración PLAN_CUENTAS_RELA -> PLANCUENTASRELA finalizada correctamente.");
    }

    private async Task<List<PlanCuentasRelaSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<PlanCuentasRelaSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT presu, cuenta_presu, cuenta_conta FROM PLAN_CUENTAS_RELA
            WHERE
                presu LIKE '%P2026%'
        """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var PlanCuentasRela = new PlanCuentasRelaSqlServer
            {
                presu = reader["presu"]
                    .ToString()!
                    .Trim(),

                cuenta_presu = reader["cuenta_presu"]
                    .ToString()!
                    .Trim(),

                cuenta_conta = reader["cuenta_conta"]
                    .ToString()!
                    .Trim(),
            };

            resultado.Add(PlanCuentasRela);
        }

        return resultado;
    }

    private async Task<Dictionary<string, int>> CargarPresupuestosAsync()
    {
        var resultado = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT
                PRESUPUESTOSDESCRIPCIONREDU,
                PRESUPUESTOSID
            FROM PRESUPUESTOS
        """;

        await using var command =
            new OracleCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var codigo =
                reader["PRESUPUESTOSDESCRIPCIONREDU"]
                    .ToString()!
                    .Trim();

            var id =
                Convert.ToInt32(
                    reader["PRESUPUESTOSID"]);

            resultado[codigo] = id;
        }

        return resultado;
    }

    private static int ObtenerPresupuestosId(
        string codigo,
        Dictionary<string, int> Presupuestos)
    {

        if (Presupuestos.TryGetValue(
                codigo.Trim(),
                out var id))
        {
            return id;
        }

        throw new InvalidOperationException(
            $"No se encontró Presupuesto con código '{codigo}'."
        );
    }        

    private List<PlanCuentasRelaOracle> Transformar(
        List<PlanCuentasRelaSqlServer> origen,
        Dictionary<string, int> Presupuestos
    ){
        var resultado = new List<PlanCuentasRelaOracle>();

        foreach (var item in origen)
        {
            var PlanCuentasRela = new PlanCuentasRelaOracle
            {
                PresupuestosId = ObtenerPresupuestosId(item.presu, Presupuestos),
                STPCuentasCuentasRelaId1 = Convert.ToInt32(item.cuenta_presu), 
                STPCuentasCuentasRelaId2 = Convert.ToInt32(item.cuenta_conta),
            };
            resultado.Add(PlanCuentasRela);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<PlanCuentasRelaOracle> PlanCuentasRela)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO PLANCUENTASRELA
                (
                    PRESUPUESTOSID,
                    STPCUENTASCUENTASRELAID1,
                    STPCUENTASCUENTASRELAID2,
                    PLANCUENTASRELAFCHALTA,
                    PLANCUENTASRELAFCHBAJA,
                    PLANCUENTASRELAUSUARIOSIDALTA,
                    PLANCUENTASRELAUSUARIOSIDBAJA
                )
                VALUES
                (
                    :PresupuestosId,
                    :STPCuentasCuentasRelaId1,
                    :STPCuentasCuentasRelaId2,
                    SYSDATE,
                    NULL,
                    1,
                    NULL
                )
            """;

            foreach (var plancuenta in PlanCuentasRela)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "PresupuestosId",
                    OracleDbType.Int32
                ).Value = plancuenta.PresupuestosId;

                command.Parameters.Add(
                    "STPCuentasCuentasRelaId1",
                    OracleDbType.Int32
                ).Value = plancuenta.STPCuentasCuentasRelaId1;

                command.Parameters.Add(
                    "STPCuentasCuentasRelaId2",
                    OracleDbType.Int32
                ).Value = plancuenta.STPCuentasCuentasRelaId2;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "PlanCuentaRela {Codigo} - {Descripcion} insertado.",
                    plancuenta.STPCuentasCuentasRelaId1,
                    plancuenta.STPCuentasCuentasRelaId2);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en PLANCUENTASRELA.",
                PlanCuentasRela.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de PLANCUENTASRELA.");
            throw;
        }
    }
}