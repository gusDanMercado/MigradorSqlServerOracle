using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class ExistenciasMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public ExistenciasMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración PRODUCTOS, PRODUC_DETA, DESPOSITOS -> EXISTENCIAS.");

        var ExistenciasOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.",
            ExistenciasOrigen.Count);

        var ExistenciasDestino = Transformar(ExistenciasOrigen);

        await InsertarOracleAsync(ExistenciasDestino);

        Log.Information(
            "Migración PRODUCTOS, PRODUC_DETA, DESPOSITOS -> EXISTENCIAS finalizada correctamente.");
    }

    private async Task<List<ExistenciasSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<ExistenciasSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT  
                det.produc,
                replace(p.descrip_larga,';',' ') as producDescrip,
                det.canti_real AS cant_depo,
                det.depo,
                depo.descrip as depoDescrip,
                det.ubi,
                p.costo_ulti_compra,
                CONVERT(VARCHAR(20), p.fecha_ulti_compra, 120) AS fecha_ulti_compra,
                p.cuenta
            FROM productos p
            INNER JOIN produc_deta det 
                ON p.produc = det.produc
            INNER JOIN depositos depo 
                ON det.depo = depo.depo
            WHERE det.canti_real <> 0
            ORDER BY det.produc, det.depo;
        """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var Existencias = new ExistenciasSqlServer
            {
                produc = reader["produc"]
                    .ToString()!
                    .Trim(),

                producDescrip = reader["producDescrip"]
                    .ToString()!
                    .Trim(),

                cant_depo = Convert.ToInt32(reader["cant_depo"]),

                depo = reader["depo"]
                    .ToString()!
                    .Trim(),

                depoDescrip = reader["depoDescrip"]
                    .ToString()!
                    .Trim(),

                ubi = reader["ubi"]
                    .ToString()!
                    .Trim(),

                costo_ulti_compra = Convert.ToDecimal(reader["costo_ulti_compra"]),

                fecha_ulti_compra = Convert.ToDateTime(reader["fecha_ulti_compra"]),

                cuenta = reader["cuenta"]
                    .ToString()!
                    .Trim(),                                                                                                                        
            };

            resultado.Add(Existencias);
        }

        return resultado;
    }

    private List<ExistenciasOracle> Transformar(
        List<ExistenciasSqlServer> origen)
    {
        var resultado = new List<ExistenciasOracle>();

        foreach (var item in origen)
        {
            var Existencias = new ExistenciasOracle
            {
                ExistenciasProductoCodi = item.produc.Trim(),
                ExistenciasProductoDescr = item.producDescrip.Trim(),
                ExistenciasCantidad = Convert.ToInt32(item.cant_depo),
                ExistenciasDepositoCodi = item.depo.Trim(),
                ExistenciasDepositoDescr = item.depoDescrip.Trim(),
                ExistenciasUbicacionCodi = item.ubi.Trim(),
                ExistenciasCostoUltiCompra = Convert.ToDecimal(item.costo_ulti_compra),
                ExistenciasFchUltiCompra = Convert.ToDateTime(item.fecha_ulti_compra), 
                ExistenciasCuentaCodi = item.cuenta.Trim()
            };
            resultado.Add(Existencias);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<ExistenciasOracle> Existencias)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO EXISTENCIAS
                (
                    ExistenciasProductoCodi,
                    ExistenciasProductoDescr,
                    ExistenciasCantidad,
                    ExistenciasDepositoCodi,
                    ExistenciasDepositoDescr,
                    ExistenciasUbicacionCodi,
                    ExistenciasCostoUltiCompra,
                    ExistenciasFchUltiCompra,
                    ExistenciasCuentaCodi
                )
                VALUES
                (
                    :ExistenciasProductoCodi,
                    :ExistenciasProductoDescr,
                    :ExistenciasCantidad,
                    :ExistenciasDepositoCodi,
                    :ExistenciasDepositoDescr,
                    :ExistenciasUbicacionCodi,
                    :ExistenciasCostoUltiCompra,
                    :ExistenciasFchUltiCompra,
                    :ExistenciasCuentaCodi
                )
                """;

            foreach (var existencia in Existencias)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "ExistenciasProductoCodi",
                    OracleDbType.NVarchar2
                ).Value = existencia.ExistenciasProductoCodi;

                command.Parameters.Add(
                    "ExistenciasProductoDescr",
                    OracleDbType.NVarchar2
                ).Value = existencia.ExistenciasProductoDescr;

                command.Parameters.Add(
                    "ExistenciasCantidad",
                    OracleDbType.Int32
                ).Value = existencia.ExistenciasCantidad;

                command.Parameters.Add(
                    "ExistenciasDepositoCodi",
                    OracleDbType.NVarchar2
                ).Value = existencia.ExistenciasDepositoCodi;

                command.Parameters.Add(
                    "ExistenciasDepositoDescr",
                    OracleDbType.NVarchar2
                ).Value = existencia.ExistenciasDepositoDescr;

                command.Parameters.Add(
                    "ExistenciasUbicacionCodi",
                    OracleDbType.NVarchar2
                ).Value = existencia.ExistenciasUbicacionCodi;

                command.Parameters.Add(
                    "ExistenciasCostoUltiCompra",
                    OracleDbType.Decimal
                ).Value = existencia.ExistenciasCostoUltiCompra;

                command.Parameters.Add(
                    "ExistenciasFchUltiCompra",
                    OracleDbType.Date
                ).Value = existencia.ExistenciasFchUltiCompra;

                command.Parameters.Add(
                    "ExistenciasCuentaCodi",
                    OracleDbType.NVarchar2
                ).Value = existencia.ExistenciasCuentaCodi;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "Exitencia {Codigo} - {Descripcion} insertado.",
                    existencia.ExistenciasProductoCodi,
                    existencia.ExistenciasDepositoDescr);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en EXISTENCIAS.",
                Existencias.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de EXISTENCIAS.");
            throw;
        }
    }
}