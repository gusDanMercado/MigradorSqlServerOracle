using MigradorSqlServerOracle.Models.Oracle;
using Serilog;

using OracleDb =
    MigradorSqlServerOracle.Database.Oracle.OracleDatabase;

using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class FuentesAbastecimientoMigration
{
    private readonly OracleDb _oracleDatabase;

    public FuentesAbastecimientoMigration(
        OracleDb oracleDatabase)
    {
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración PRODUCCION.FUENTES_ABASTECIMIENTO -> FUENTESABASTECIMIENTO.");

        var FuentesAbastecimientoOrigen =
            await LeerOracleOrigenAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde PRODUCCION.FUENTES_ABASTECIMIENTO.",
            FuentesAbastecimientoOrigen.Count);

        var Localidades =
            await CargarLocalidadesAsync();
        Log.Information(
            "Se leyeron {Cantidad} registros desde Oracle (Localidades).", Localidades.Count);

        await InsertarOracleAsync(FuentesAbastecimientoOrigen, Localidades);

        Log.Information(
            "Migración PRODUCCION.FUENTES_ABASTECIMIENTO -> FUENTESABASTECIMIENTO finalizada correctamente.");
    }

    private async Task<List<FuentesAbastecimientoOracle>> LeerOracleOrigenAsync()
    {
        var resultado =
            new List<FuentesAbastecimientoOracle>();

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT 
                DISTINCT(fa.DENOMINACION),
                fa.ID_TIPO_FUENTE,
                fa.ID_LOC,
                fa.NIS,
                rc.LAT,
                rc.LNG
            FROM PRODUCCION.FUENTES_ABASTECIMIENTO fa
            LEFT JOIN DATASTORE5.RELFUENTES_CATASTROSDGI rc ON fa.ID_FUENTE = rc.ID_FUENTE
            WHERE fa.FCH_CAD IS NULL
        """;

        await using var command =
            new OracleCommand(sql, connection);

        command.BindByName = true;

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var FuentesAbastecimiento =
                new FuentesAbastecimientoOracle
                {
                    DENOMINACION =
                        reader["DENOMINACION"] == DBNull.Value
                            ? string.Empty
                            : reader["DENOMINACION"]
                                .ToString()!
                                .Trim(),

                    ID_TIPO_FUENTE = Convert.ToInt32(reader["ID_TIPO_FUENTE"]),
                    ID_LOC = Convert.ToInt32(reader["ID_LOC"]),

                    NIS = reader["NIS"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["NIS"]),

                    LAT = reader["LAT"] == DBNull.Value
                        ? null
                        : Convert.ToDouble(reader["LAT"]),

                    LNG = reader["LNG"] == DBNull.Value
                        ? null
                        : Convert.ToDouble(reader["LNG"]),                    
                };

            resultado.Add(FuentesAbastecimiento);
        }

        return resultado;
    }

    private async Task<Dictionary<int, int>> CargarLocalidadesAsync()
    {
        var resultado = new Dictionary<int, int>();

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT
                LOCALIDADESIDGESP,
                LOCALIDADESID
            FROM LOCALIDADES
        """;

        await using var command =
            new OracleCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var idgesp =
                Convert.ToInt32(
                    reader["LOCALIDADESIDGESP"]);

            var id =
                Convert.ToInt32(
                    reader["LOCALIDADESID"]);

            resultado[idgesp] = id;
        }

        return resultado;
    }    

    private static int ObtenerLocalidadesId(
        int idgesp,
        Dictionary<int, int> Localidades)
    {

        if (Localidades.TryGetValue(
                idgesp,
                out var id))
        {
            return id;
        }

        throw new InvalidOperationException(
            $"No se encontró Localidad con código '{idgesp}'."
        );
    }

    private async Task InsertarOracleAsync(
        List<FuentesAbastecimientoOracle> FuentesAbastecimiento,
        Dictionary<int, int> Localidades
    )
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO FUENTESABASTECIMIENTO
                (
                    FUENTESABASTECIMIENTODENOM,
                    TIPOSFUENTEID,
                    LOCALIDADESID,
                    FUENTESABASTECIMIENTONIS,
                    FUENTESABASTECIMIENTOLATITUD,
                    FUENTESABASTECIMIENTOLONGITUD,
                    FUENTESABASTECIMIENTOFCHALTA,
                    FUENTESABASTECIMIENTOFCHBAJA,
                    FUENTESABASTECIMIENTOUSUALTA,
                    FUENTESABASTECIMIENTOUSUBAJA
                )
                VALUES
                (
                    :DENOMINACION,
                    :ID_TIPO_FUENTE,
                    :ID_LOC,
                    :NIS,
                    :LAT,
                    :LNG,
                    SYSDATE,
                    NULL,
                    1,
                    NULL
                )
            """;

            foreach (var FuenteAbastecimiento in FuentesAbastecimiento)
            {
                await using var command =
                    new OracleCommand(
                        sql,
                        connection);

                command.Transaction =
                    transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "DENOMINACION",
                    OracleDbType.NVarchar2
                ).Value =
                    string.IsNullOrWhiteSpace(
                        FuenteAbastecimiento.DENOMINACION)
                        ? DBNull.Value
                        : FuenteAbastecimiento.DENOMINACION;

                command.Parameters.Add(
                    "ID_TIPO_FUENTE",
                    OracleDbType.Int32
                ).Value = FuenteAbastecimiento.ID_TIPO_FUENTE;

                command.Parameters.Add(
                    "ID_LOC",
                    OracleDbType.Int32
                ).Value = ObtenerLocalidadesId(FuenteAbastecimiento.ID_LOC, Localidades);  
                //FuenteAbastecimiento.ID_LOC;

                command.Parameters.Add(
                    "NIS",
                    OracleDbType.Int32
                ).Value =
                    FuenteAbastecimiento.NIS.HasValue
                        ? FuenteAbastecimiento.NIS.Value
                        : DBNull.Value;

                command.Parameters.Add(
                    "LAT",
                    OracleDbType.Double
                ).Value =
                    FuenteAbastecimiento.LAT.HasValue
                        ? FuenteAbastecimiento.LAT.Value
                        : DBNull.Value;

                command.Parameters.Add(
                    "LNG",
                    OracleDbType.Double
                ).Value =
                    FuenteAbastecimiento.LNG.HasValue
                        ? FuenteAbastecimiento.LNG.Value
                        : DBNull.Value;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "Fuente abastecimiento {localidad} - {fuente} insertada.",
                    FuenteAbastecimiento.ID_LOC,
                    FuenteAbastecimiento.DENOMINACION);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en FUENTESABASTECIMIENTO.",
                FuentesAbastecimiento.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de FUENTESABASTECIMIENTO.");
            throw;
        }
    }
}