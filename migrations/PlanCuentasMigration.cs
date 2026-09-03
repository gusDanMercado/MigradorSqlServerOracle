using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class PlanCuentasMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public PlanCuentasMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración PLAN_CUENTAS_PRESU -> PLANCUENTAS.");

        var PlanCuentasOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.",
            PlanCuentasOrigen.Count);

        var PlanCuentasDestino = Transformar(PlanCuentasOrigen);

        // Inicio Agregar registro especial "SIN CUENTA"
        if (!PlanCuentasDestino.Any(x => x.PlanCuentasId == 0))
        {
            PlanCuentasDestino.Insert(0, new PlanCuentasOracle
            {
                PlanCuentasId = 0,
                PlanCuentasDescrip = "SIN CUENTA ASOCIADA",
                PlanCuentasDescripRedu = "SIN CUENTA",
                PlanCuentasTipo = null,
                PlanCuentasMoneMarca = false,
                PlanCuentasFchAlta = DateTime.Now,
                PlanCuentasAjusMarca = false,
                PlanCuentasAjusCuenta = 0,
                PlanCuentasAgru1 = "",
                PlanCuentasAgru2 = "",
                PlanCuentasAgru3 = "",
                PlanCuentasAgru4 = "",
                PlanCuentasPresuFinan = false
            });
        }        
        // Fin Agregar registro especial "SIN CUENTA"
  
        await InsertarOracleAsync(PlanCuentasDestino);

        Log.Information(
            "Migración PLANCUENTAS -> PLANCUENTAS finalizada correctamente.");
    }

    private async Task<List<PlanCuentasSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<PlanCuentasSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT 
                cuenta, nombre, nombre_redu, tipo, mone_marca, alta_fecha, ajus_marca, ajus_cuenta, agru_1, agru_2, agru_3, agru_4, presu_finan
            FROM PLAN_CUENTAS_PRESU
        """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var PlanCuentas = new PlanCuentasSqlServer
            {
                cuenta = reader["cuenta"]
                    .ToString()!
                    .Trim(),

                nombre = reader["nombre"]
                    .ToString()!
                    .Trim(),

                nombre_redu = reader["nombre_redu"]
                    .ToString()!
                    .Trim(),

                tipo = reader["tipo"]
                    .ToString()!
                    .Trim(),

                mone_marca = Convert.ToInt32(reader["mone_marca"]),

                alta_fecha = Convert.ToDateTime(reader["alta_fecha"]),

                ajus_marca = Convert.ToInt32(reader["ajus_marca"]),

                ajus_cuenta = reader["ajus_cuenta"]
                    .ToString()!
                    .Trim(),

                agru_1 = reader["agru_1"]
                    .ToString()!
                    .Trim(),

                agru_2 = reader["agru_2"]
                    .ToString()!
                    .Trim(),                    

                agru_3 = reader["agru_3"]
                    .ToString()!
                    .Trim(),

                agru_4 = reader["agru_4"]
                    .ToString()!
                    .Trim(),                    

                presu_finan = reader["presu_finan"]
                    .ToString()!
                    .Trim(),                                                                
            };

            resultado.Add(PlanCuentas);
        }

        return resultado;
    }

    private List<PlanCuentasOracle> Transformar(
        List<PlanCuentasSqlServer> origen)
    {
        var resultado = new List<PlanCuentasOracle>();

        foreach (var item in origen)
        {
            var PlanCuentas = new PlanCuentasOracle
            {
                PlanCuentasId = Convert.ToInt32(item.cuenta.Trim()),
                PlanCuentasDescrip = item.nombre.Trim(),
                PlanCuentasDescripRedu = item.nombre_redu.Trim(),
                PlanCuentasTipo = item.tipo.Trim(),
                PlanCuentasMoneMarca = Convert.ToBoolean(item.mone_marca),
                PlanCuentasFchAlta = item.alta_fecha,
                PlanCuentasAjusMarca = Convert.ToBoolean(item.ajus_marca), 
                PlanCuentasAjusCuenta = Convert.ToInt32(string.IsNullOrWhiteSpace(item.ajus_cuenta) ? "0" : item.ajus_cuenta),
                PlanCuentasAgru1 = item.agru_1.Trim(),
                PlanCuentasAgru2 = item.agru_2.Trim(),
                PlanCuentasAgru3 = item.agru_3.Trim(),
                PlanCuentasAgru4 = item.agru_4.Trim(),     
                PlanCuentasPresuFinan = string.Equals(item.presu_finan, "S", StringComparison.OrdinalIgnoreCase), 
            };
            resultado.Add(PlanCuentas);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<PlanCuentasOracle> PlanCuentas)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO PLANCUENTAS
                (
                    PLANCUENTASID,
                    PLANCUENTASDESCRIP,
                    PLANCUENTASDESCRIPREDU,
                    PLANCUENTASTIPO,
                    PLANCUENTASMONEMARCA,
                    PLANCUENTASFCHALTA,
                    PLANCUENTASAJUSMARCA,
                    PLANCUENTASAJUSCUENTA,
                    PLANCUENTASAGRU1,
                    PLANCUENTASAGRU2,
                    PLANCUENTASAGRU3,
                    PLANCUENTASAGRU4,
                    PLANCUENTASPRESUFINAN,
                    PLANCUENTASFCHBAJA,
                    PLANCUENTASUSUARIOALTA,
                    PLANCUENTASUSUARIOBAJA
                )
                VALUES
                (
                    :PlanCuentasId,
                    :PlanCuentasDescrip,
                    :PlanCuentasDescripRedu,
                    :PlanCuentasTipo,
                    :PlanCuentasMoneMarca,
                    :PlanCuentasFchAlta,
                    :PlanCuentasAjusMarca,
                    :PlanCuentasAjusCuenta,
                    :PlanCuentasAgru1,
                    :PlanCuentasAgru2,
                    :PlanCuentasAgru3,
                    :PlanCuentasAgru4,
                    :PlanCuentasPresuFinan,
                    NULL,
                    1,
                    NULL
                )
                """;

            foreach (var plancuenta in PlanCuentas)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "PlanCuentasId",
                    OracleDbType.Int32
                ).Value = plancuenta.PlanCuentasId;

                command.Parameters.Add(
                    "PlanCuentasDescrip",
                    OracleDbType.NVarchar2
                ).Value = plancuenta.PlanCuentasDescrip;

                command.Parameters.Add(
                    "PlanCuentasDescripRedu",
                    OracleDbType.NVarchar2
                ).Value = plancuenta.PlanCuentasDescripRedu;

                command.Parameters.Add(
                    "PlanCuentasTipo",
                    OracleDbType.NVarchar2
                ).Value = plancuenta.PlanCuentasTipo;

                command.Parameters.Add(
                    "PlanCuentasMoneMarca",
                    OracleDbType.Int32
                ).Value = plancuenta.PlanCuentasMoneMarca ? 1 : 0;

                command.Parameters.Add(
                    "PlanCuentasFchAlta",
                    OracleDbType.Date
                ).Value = plancuenta.PlanCuentasFchAlta;                                                                

                command.Parameters.Add(
                    "PlanCuentasAjusMarca",
                    OracleDbType.Int32
                ).Value = plancuenta.PlanCuentasAjusMarca ? 1 : 0;

                command.Parameters.Add(
                    "PlanCuentasAjusCuenta",
                    OracleDbType.Int32
                ).Value = plancuenta.PlanCuentasAjusCuenta;

                command.Parameters.Add(
                    "PlanCuentasAgru1",
                    OracleDbType.NVarchar2
                ).Value = plancuenta.PlanCuentasAgru1;

                command.Parameters.Add(
                    "PlanCuentasAgru2",
                    OracleDbType.NVarchar2
                ).Value = plancuenta.PlanCuentasAgru2;

                command.Parameters.Add(
                    "PlanCuentasAgru3",
                    OracleDbType.NVarchar2
                ).Value = plancuenta.PlanCuentasAgru3;

                command.Parameters.Add(
                    "PlanCuentasAgru4",
                    OracleDbType.NVarchar2
                ).Value = plancuenta.PlanCuentasAgru4;

                command.Parameters.Add(
                    "PlanCuentasPresuFinan",
                    OracleDbType.Int32
                ).Value = plancuenta.PlanCuentasPresuFinan.HasValue
                    ? (plancuenta.PlanCuentasPresuFinan.Value ? 1 : 0)
                    : DBNull.Value;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "Plancuenta {Codigo} - {Descripcion} insertado.",
                    plancuenta.PlanCuentasId,
                    plancuenta.PlanCuentasDescripRedu);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en PLANCUENTAS.",
                PlanCuentas.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de PLANCUENTAS.");
            throw;
        }
    }
}