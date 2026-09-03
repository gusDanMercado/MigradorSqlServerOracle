using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class CategoriasIvaMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public CategoriasIvaMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración CATEGORIAS_IVA -> CATEGORIASIVA.");

        var categoriasOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.",
            categoriasOrigen.Count);

        var categoriasDestino = Transformar(categoriasOrigen);

        await InsertarOracleAsync(categoriasDestino);

        Log.Information(
            "Migración CATEGORIAS_IVA -> CATEGORIASIVA finalizada correctamente.");
    }

    private async Task<List<CategoriaIvaSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<CategoriaIvaSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT
                codi,
                descrip,
                discrimina,
                lleva_cuit
            FROM CATEGORIAS_IVA
            """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var categoria = new CategoriaIvaSqlServer
            {
                Codi = reader["codi"]
                    .ToString()!
                    .Trim(),

                Descrip = reader["descrip"]
                    .ToString()!
                    .Trim(),

                Discrimina =
                    Convert.ToBoolean(reader["discrimina"]),

                LlevaCuit =
                    reader["lleva_cuit"] == DBNull.Value
                        ? null
                        : reader["lleva_cuit"]
                            .ToString()!
                            .Trim()
            };

            resultado.Add(categoria);
        }

        return resultado;
    }

    private List<CategoriaIvaOracle> Transformar(
        List<CategoriaIvaSqlServer> origen)
    {
        var resultado = new List<CategoriaIvaOracle>();

        foreach (var item in origen)
        {
            var categoria = new CategoriaIvaOracle
            {
                Codi = item.Codi.Trim(),
                Descrip = item.Descrip.Trim(),
                Discrimina = item.Discrimina,
                LlevaCuit = ConvertirBooleanChar(item.LlevaCuit)
            };
            resultado.Add(categoria);
        }

        return resultado;
    }

    private static bool ConvertirBooleanChar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        return valor.Trim() switch
        {
            "1" => true,
            "0" => false,
            _ => throw new InvalidOperationException(
                $"Valor de lleva_cuit no reconocido: '{valor}'.")
        };
    }

    private async Task InsertarOracleAsync(
        List<CategoriaIvaOracle> categorias)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO CATEGORIASIVA
                (
                    CATEGORIASIVACODI,
                    CATEGORIASIVADESCRIP,
                    CATEGORIASIVADISCRIMINA,
                    CATEGORIASIVALLEVACUIT,
                    CATEGORIASIVAFCHALTA,
                    CATEGORIASIVAFCHBAJA,
                    CATEGORIASIVAUSUARIOSIDALTA,
                    CATEGORIASIVAUSUARIOSIDBAJA
                )
                VALUES
                (
                    :Codi,
                    :Descrip,
                    :Discrimina,
                    :LlevaCuit,
                    SYSDATE,
                    NULL,
                    1,
                    NULL
                )
                """;

            foreach (var categoria in categorias)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "Codi",
                    OracleDbType.NVarchar2
                ).Value = categoria.Codi;

                command.Parameters.Add(
                    "Descrip",
                    OracleDbType.NVarchar2
                ).Value = categoria.Descrip;

                command.Parameters.Add(
                    "Discrimina",
                    OracleDbType.Int32
                ).Value = categoria.Discrimina ? 1 : 0;

                command.Parameters.Add(
                    "LlevaCuit",
                    OracleDbType.Int32
                ).Value = categoria.LlevaCuit ? 1 : 0;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "Categoría IVA {Codigo} - {Descripcion} insertada.",
                    categoria.Codi,
                    categoria.Descrip);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en CATEGORIASIVA.",
                categorias.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de CATEGORIASIVA.");
            throw;
        }
    }
}