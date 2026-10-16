using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class DepositosMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public DepositosMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración DEPOSITOS -> DEPOSITOS.");

        var DepositosOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.",
            DepositosOrigen.Count);

        var Localidades =
            await CargarLocalidadesAsync();
        Log.Information(
            "Se leyeron {Cantidad} registros desde ORACLE (LOCALIDADES).", Localidades.Count);

        var DepositosDestino = Transformar(DepositosOrigen, Localidades);

        await InsertarOracleAsync(DepositosDestino);

        Log.Information(
            "Migración DEPOSITOS -> DEPOSITOS finalizada correctamente.");
    }

    private async Task<List<DepositosSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<DepositosSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT 
                descrip, 
                depo, loca, tele
            from DEPOSITOS
        """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var Depositos = new DepositosSqlServer
            {
                descrip = reader["descrip"]
                    .ToString()!
                    .Trim(),

                depo = reader["depo"]
                    .ToString()!
                    .Trim(),

                loca =
                    reader["loca"] == DBNull.Value
                        ? null
                        : reader["loca"]
                            .ToString()!
                            .Trim().ToUpper(),

                tele =
                    reader["tele"] == DBNull.Value
                        ? null
                        : reader["tele"]
                            .ToString()!
                            .Trim(),
            };

            resultado.Add(Depositos);
        }

        return resultado;
    }

    private async Task<Dictionary<string, int>> CargarLocalidadesAsync()
    {
        var resultado = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT
                LOCALIDADESDESCRIP,
                LOCALIDADESID
            FROM LOCALIDADES
        """;

        await using var command =
            new OracleCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var codigo =
                reader["LOCALIDADESDESCRIP"]
                    .ToString()!
                    .Trim();

            var id =
                Convert.ToInt32(
                    reader["LOCALIDADESID"]);

            resultado[codigo] = id;
        }

        return resultado;
    }

    /*
    private static int? ObtenerLocalidadesId(
        string? codigo,
        Dictionary<string, int> Localidades)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return null;
        }        

        if (Localidades.TryGetValue(
                codigo.Trim(),
                out var id))
        {
            return id;
        }

        throw new InvalidOperationException(
            $"No se encontró Localidad con código '{codigo}'."
        );
    }
    */
    private static int? ObtenerLocalidadesId(
        string? descripcion,
        Dictionary<string, int> localidades)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            return 204;  // "SIN LOCALIDAD"
        }

        descripcion = descripcion.Trim();

        var equivalencias = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            { "SALTA", "CAPITAL - SALTA" },
            { "CAMPAMENTO VESPUCIO", "VESPUCIO" },
            { "J. V. GONZALEZ", "J.V.GONZALEZ" },
            { "SAN RAMON DE LA NUEVA ORAN", "S R DE LA NUEVA ORAN" }
        };

        if (equivalencias.TryGetValue(
                descripcion,
                out var descripcionOracle))
        {
            descripcion = descripcionOracle;
        }

        if (localidades.TryGetValue(
                descripcion,
                out var id))
        {
            return id;
        }

        throw new InvalidOperationException(
            $"No se encontró Localidad con descripción '{descripcion}'."
        );
    }

    private List<DepositosOracle> Transformar(
        List<DepositosSqlServer> origen,
        Dictionary<string, int> Localidades
    ){
        var resultado = new List<DepositosOracle>();

        foreach (var item in origen)
        {
            var Depositos = new DepositosOracle
            {
                DepositosDescrip = item.descrip?.Trim() ?? "",
                DepositosCodigo = item.depo?.Trim() ?? "",
                LOCALIDADESID = ObtenerLocalidadesId(item.loca, Localidades),
                DepositosTelefono = item.tele?.Trim()
            };
            resultado.Add(Depositos);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<DepositosOracle> Depositos)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();
        try
        {
            const string sql = """
                INSERT INTO DEPOSITOS
                (
                    DEPOSITOSDESCRIP,
                    DEPOSITOSCODIGO,                
                    LOCALIDADESID,
                    DEPOSITOSTELEFONO,
                    DEPOSITOSFCHALTA,
                    DEPOSITOSFCHBAJA,
                    DEPOSITOSUSUALTA,
                    DEPOSITOSUSUBAJA
                )
                VALUES
                (
                    :DepositosDescrip,
                    :DepositosCodigo,
                    :LOCALIDADESID,
                    :DepositosTelefono,
                    SYSDATE,
                    NULL,
                    1,
                    NULL
                )
                """;

            foreach (var deposito in Depositos)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "DepositosCodigo",
                    OracleDbType.NVarchar2
                ).Value = deposito.DepositosCodigo;

                command.Parameters.Add(
                    "DepositosDescrip",
                    OracleDbType.NVarchar2
                ).Value = deposito.DepositosDescrip;

                command.Parameters.Add(
                    "LOCALIDADESID",
                    OracleDbType.Int32
                ).Value =
                    deposito.LOCALIDADESID.HasValue
                        ? deposito.LOCALIDADESID.Value
                        : DBNull.Value;

                command.Parameters.Add(
                    "DepositosTelefono",
                    OracleDbType.NVarchar2
                ).Value =
                    string.IsNullOrWhiteSpace(deposito.DepositosTelefono)
                        ? DBNull.Value
                        : deposito.DepositosTelefono;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "Deposito {Codigo} - {Descripcion} insertado.",
                    deposito.DepositosCodigo,
                    deposito.DepositosDescrip);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en DEPOSITOS.",
                Depositos.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de DEPOSITOS.");
            throw;
        }
    }
}