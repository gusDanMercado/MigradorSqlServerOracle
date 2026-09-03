using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class SubGruposProducMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public SubGruposProducMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración SUBGRUPOS -> SUBGRUPOSPRODUC.");

        var SubGruposProducOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.",
            SubGruposProducOrigen.Count);

        var GruposProducto =
            await CargarGruposProductoAsync();
        Log.Information(
            "Se leyeron {Cantidad} registros desde Oracle (GruposProducto).", GruposProducto.Count);

        var SubGruposProducDestino = Transformar(SubGruposProducOrigen, GruposProducto);

        await InsertarOracleAsync(SubGruposProducDestino);

        Log.Information(
            "Migración SUBGRUPOS -> SUBGRUPOSPRODUC finalizada correctamente.");
    }

    private async Task<List<SubGruposProducSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<SubGruposProducSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT grupo, codigo, descrip 
            FROM SUBGRUPOS;
        """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var SubGruposProduc = new SubGruposProducSqlServer
            {
                grupo = reader["grupo"]
                    .ToString()!
                    .Trim(),

                codigo = reader["codigo"]
                    .ToString()!
                    .Trim(),

                descrip = reader["descrip"]
                    .ToString()!
                    .Trim(),
            };

            resultado.Add(SubGruposProduc);
        }

        return resultado;
    }

    private async Task<Dictionary<string, int>> CargarGruposProductoAsync()
    {
        var resultado = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT
                GRUPOSPRODUCTOCODIGO,
                GRUPOSPRODUCTOID
            FROM GRUPOSPRODUCTO
        """;

        await using var command =
            new OracleCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var codigo =
                reader["GRUPOSPRODUCTOCODIGO"]
                    .ToString()!
                    .Trim();

            var id =
                Convert.ToInt32(
                    reader["GRUPOSPRODUCTOID"]);

            resultado[codigo] = id;
        }

        return resultado;
    }    

    private static int ObtenerGruposProductoId(
        string codigo,
        Dictionary<string, int> GruposProducto)
    {

        if (GruposProducto.TryGetValue(
                codigo.Trim(),
                out var id))
        {
            return id;
        }

        throw new InvalidOperationException(
            $"No se encontró GruposProducto con código '{codigo}'."
        );
    }     

    private List<SubGruposProducOracle> Transformar(
        List<SubGruposProducSqlServer> origen,
        Dictionary<string, int> GruposProducto
    ){
        var resultado = new List<SubGruposProducOracle>();

        foreach (var item in origen)
        {
            var SubGruposProduc = new SubGruposProducOracle
            {
                GRUPOSPRODUCTOID = ObtenerGruposProductoId(item.grupo, GruposProducto),
                SUBGRUPOSPRODUCCODIGO = item.codigo.Trim(),
                SUBGRUPOSPRODUCDESCRIP = item.descrip.Trim(),
            };
            
            resultado.Add(SubGruposProduc);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<SubGruposProducOracle> SubGruposProduc)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO SUBGRUPOSPRODUC
                (
                    GRUPOSPRODUCTOID,
                    SUBGRUPOSPRODUCCODIGO,
                    SUBGRUPOSPRODUCDESCRIP,
                    SUBGRUPOSPRODUCFCHALTA,
                    SUBGRUPOSPRODUCFCHBAJA,
                    SUBGRUPOSPRODUCUSUALTA,
                    SUBGRUPOSPRODUCUSUBAJA
                )
                VALUES
                (
                    :GRUPOSPRODUCTOID,
                    :SUBGRUPOSPRODUCCODIGO,
                    :SUBGRUPOSPRODUCDESCRIP,
                    SYSDATE,
                    NULL,
                    1,
                    NULL
                )
                """;

            foreach (var SubGrupo in SubGruposProduc)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "GRUPOSPRODUCTOID",
                    OracleDbType.Int32
                ).Value = SubGrupo.GRUPOSPRODUCTOID;

                command.Parameters.Add(
                    "SUBGRUPOSPRODUCCODIGO",
                    OracleDbType.NVarchar2
                ).Value = SubGrupo.SUBGRUPOSPRODUCCODIGO;

                command.Parameters.Add(
                    "SUBGRUPOSPRODUCDESCRIP",
                    OracleDbType.NVarchar2
                ).Value = SubGrupo.SUBGRUPOSPRODUCDESCRIP;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "SubGrupo {Codigo} - {Descripcion} insertado.",
                    SubGrupo.SUBGRUPOSPRODUCCODIGO,
                    SubGrupo.SUBGRUPOSPRODUCDESCRIP);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en SubGruposProduc.",
                SubGruposProduc.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de SUBGRUPOSPRODUC.");
            throw;
        }
    }
}