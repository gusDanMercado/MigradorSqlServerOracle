using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class ProductosMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public ProductosMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración PRODUCTOS -> PRODUCTOS.");

        var ProductosOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.",
            ProductosOrigen.Count);

        var UnidadesMedida =
            await CargarUnidadesMedidaAsync();
        Log.Information(
            "Se leyeron {Cantidad} registros desde Oracle (UnidadesMedida).", UnidadesMedida.Count);

        var SubGruposProduc =
            await CargarSubGruposProducAsync();
        Log.Information(
            "Se leyeron {Cantidad} registros desde Oracle (SubGruposProduc).", SubGruposProduc.Count);

        var ProductosDestino = Transformar(ProductosOrigen, UnidadesMedida, SubGruposProduc);

        await InsertarOracleAsync(ProductosDestino);

        Log.Information(
            "Migración PRODUCTOS -> PRODUCTOS finalizada correctamente.");
    }

    private async Task<List<ProductosSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<ProductosSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT 
                uni_medi_stock, agru_2, cuenta, produc, descrip_corta, descrip_larga, tipo
            FROM PRODUCTOS
        """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var Productos = new ProductosSqlServer
            {
                uni_medi_stock = reader["uni_medi_stock"]
                    .ToString()!
                    .Trim(),

                agru_2 = reader["agru_2"]
                    .ToString()!
                    .Trim(),

                cuenta = reader["cuenta"] == DBNull.Value
                    ? null
                    : reader["cuenta"].ToString()?.Trim(),                      

                produc = reader["produc"]
                    .ToString()!
                    .Trim(),                    

                descrip_corta = reader["descrip_corta"]
                    .ToString()!
                    .Trim(),

                descrip_larga = reader["descrip_larga"]
                    .ToString()!
                    .Trim(),

                tipo = reader["tipo"]
                    .ToString()!
                    .Trim(),                                                                                                    
            };

            resultado.Add(Productos);
        }

        return resultado;
    }
    private async Task<Dictionary<string, int>> CargarUnidadesMedidaAsync()
    {
        var resultado = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT
                UNIDADESMEDIDACODIGO,
                UNIDADESMEDIDAID
            FROM UNIDADESMEDIDA
        """;

        await using var command =
            new OracleCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var codigo =
                reader["UNIDADESMEDIDACODIGO"]
                    .ToString()!
                    .Trim();

            var id =
                Convert.ToInt32(
                    reader["UNIDADESMEDIDAID"]);

            resultado[codigo] = id;
        }

        return resultado;
    }

    private static int ObtenerUnidadesMedidaId(
        string codigo,
        Dictionary<string, int> UnidadesMedida)
    {

        if (UnidadesMedida.TryGetValue(
                codigo.Trim(),
                out var id))
        {
            return id;
        }

        throw new InvalidOperationException(
            $"No se encontró UnidadesMedida con código '{codigo}'."
        );
    } 

    private async Task<Dictionary<string, int>> CargarSubGruposProducAsync()
    {
        var resultado = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT
                SUBGRUPOSPRODUCCODIGO,
                SUBGRUPOSPRODUCID
            FROM SUBGRUPOSPRODUC
        """;

        await using var command =
            new OracleCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var codigo =
                reader["SUBGRUPOSPRODUCCODIGO"]
                    .ToString()!
                    .Trim();

            var id =
                Convert.ToInt32(
                    reader["SUBGRUPOSPRODUCID"]);

            resultado[codigo] = id;
        }

        return resultado;
    }       

    private static int ObtenerSubGruposProducId(
        string codigo,
        Dictionary<string, int> SubGruposProduc)
    {

        if (SubGruposProduc.TryGetValue(
                codigo.Trim(),
                out var id))
        {
            return id;
        }

        throw new InvalidOperationException(
            $"No se encontró SubGruposProduc con código '{codigo}'."
        );
    }     

    private List<ProductosOracle> Transformar(
        List<ProductosSqlServer> origen,
        Dictionary<string, int> UnidadesMedida,
        Dictionary<string, int> SubGruposProduc
    ){
        var resultado = new List<ProductosOracle>();

        foreach (var item in origen)
        {
            var Productos = new ProductosOracle
            {
                UNIDADESMEDIDAID = ObtenerUnidadesMedidaId(item.uni_medi_stock, UnidadesMedida),
                SubGruposProducId = ObtenerSubGruposProducId(item.agru_2, SubGruposProduc),
                PLANCUENTASID = Convert.ToInt32(item.cuenta),
                PRODUCTOSCODIGO = item.produc.Trim(),
                PRODUCTOSDESCRIPCORTA = item.descrip_corta.Trim(),
                PRODUCTOSDESCRIPLARGA = item.descrip_larga.Trim(),
                PRODUCTOSTIPO = item.tipo.Trim(),
            };
            resultado.Add(Productos);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<ProductosOracle> Productos)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            const string sql = """
                INSERT INTO PRODUCTOS
                (
                    UNIDADESMEDIDAID, 
                    SubGruposProducId,
                    PLANCUENTASID,
                    PRODUCTOSCODIGO,
                    PRODUCTOSDESCRIPCORTA,
                    PRODUCTOSDESCRIPLARGA,
                    PRODUCTOSTIPO,
                    PRODUCTOSCANTIDADACTUAL,
                    PRODUCTOSFCHALTA,
                    PRODUCTOSFCHBAJA,
                    PRODUCTOSUSUALTA,
                    PRODUCTOSUSUBAJA
                )
                VALUES
                (
                    :UNIDADESMEDIDAID, 
                    :SubGruposProducId,
                    :PLANCUENTASID,
                    :PRODUCTOSCODIGO,
                    :PRODUCTOSDESCRIPCORTA,
                    :PRODUCTOSDESCRIPLARGA,
                    :PRODUCTOSTIPO,
                    0,
                    SYSDATE,
                    NULL,
                    1,
                    NULL
                )
                """;

            foreach (var producto in Productos)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "UNIDADESMEDIDAID",
                    OracleDbType.Int32
                ).Value = producto.UNIDADESMEDIDAID;

                command.Parameters.Add(
                    "SubGruposProducId",
                    OracleDbType.Int32
                ).Value = producto.SubGruposProducId;

                command.Parameters.Add(
                    "PLANCUENTASID",
                    OracleDbType.Int32
                ).Value = producto.PLANCUENTASID;

                command.Parameters.Add(
                    "PRODUCTOSCODIGO",
                    OracleDbType.NVarchar2
                ).Value = producto.PRODUCTOSCODIGO;

                command.Parameters.Add(
                    "PRODUCTOSDESCRIPCORTA",
                    OracleDbType.NVarchar2
                ).Value = producto.PRODUCTOSDESCRIPCORTA;

                command.Parameters.Add(
                    "PRODUCTOSDESCRIPLARGA",
                    OracleDbType.NVarchar2
                ).Value = producto.PRODUCTOSDESCRIPLARGA;

                command.Parameters.Add(
                    "PRODUCTOSTIPO",
                    OracleDbType.NVarchar2
                ).Value = producto.PRODUCTOSTIPO;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "Producto {Codigo} - {Descripcion} insertado.",
                    producto.PRODUCTOSCODIGO,
                    producto.PRODUCTOSDESCRIPCORTA);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en PRODUCTOS.",
                Productos.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de PRODUCTOS.");
            throw;
        }
    }
}