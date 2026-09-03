using Microsoft.Data.SqlClient;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Models.Oracle;
using MigradorSqlServerOracle.Models.SqlServer;
using Serilog;

using OracleDb = MigradorSqlServerOracle.Database.Oracle.OracleDatabase;
using Oracle.ManagedDataAccess.Client;

namespace MigradorSqlServerOracle.Migrations;

public class ProveedorMigration
{
    private readonly SqlServerDatabase _sqlServerDatabase;
    private readonly OracleDb _oracleDatabase;

    public ProveedorMigration(
        SqlServerDatabase sqlServerDatabase,
        OracleDb oracleDatabase)
    {
        _sqlServerDatabase = sqlServerDatabase;
        _oracleDatabase = oracleDatabase;
    }

    public async Task EjecutarAsync()
    {
        Log.Information(
            "Iniciando migración PROVEEDORES -> PROVEEDORES.");

        var proveedorOrigen = await LeerSqlServerAsync();

        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server.", proveedorOrigen.Count);

        var categoriasIva =
            await CargarCategoriasIvaAsync();
        Log.Information(
            "Se leyeron {Cantidad} registros desde SQL Server (categoriaIva).", categoriasIva.Count);

        var agrupacionesProveedor =
            await CargarAgruProveAsync();            
        Log.Information(
                    "Se leyeron {Cantidad} registros desde SQL Server (AgruProve).", agrupacionesProveedor.Count);

        var proveedorDestino = Transformar(proveedorOrigen, categoriasIva, agrupacionesProveedor);

        await InsertarOracleAsync(proveedorDestino);

        Log.Information(
            "Migración PROVEEEDORES -> PROVEEDORES finalizada correctamente.");
    }

    private async Task<Dictionary<string, int>> CargarCategoriasIvaAsync()
    {
        var resultado = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT
                CATEGORIASIVACODI,
                CATEGORIASIVAID
            FROM CATEGORIASIVA
            """;

        await using var command =
            new OracleCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var codigo =
                reader["CATEGORIASIVACODI"]
                    .ToString()!
                    .Trim();

            var id =
                Convert.ToInt32(
                    reader["CATEGORIASIVAID"]);

            resultado[codigo] = id;
        }

        return resultado;
    }    

    private async Task<Dictionary<string, int>> CargarAgruProveAsync()
    {
        var resultado = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        const string sql = """
            SELECT
                AGRUPROVECODIGO,
                AGRUPROVEID
            FROM AGRUPROVE
            """;

        await using var command =
            new OracleCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var codigo =
                reader["AGRUPROVECODIGO"]
                    .ToString()!
                    .Trim();

            var id =
                Convert.ToInt32(
                    reader["AGRUPROVEID"]);

            resultado[codigo] = id;
        }

        return resultado;
    }

    private async Task<List<ProveedorSqlServer>> LeerSqlServerAsync()
    {
        var resultado = new List<ProveedorSqlServer>();

        await using var connection =
            _sqlServerDatabase.CrearConexion();

        await connection.OpenAsync();

        // Actualice mi tabla PROVEEDORES EN GENEXUS PARA QUE TENGA UN INDICE COMBIANDO CON RAZONSOC Y CUIT Y ASI PUEDA MIGRAR LOS DATOS
        // esto no deberia pasar pero hay muchos datos duplicados en la tabla original de PROVEEDORES de COSAYSA 
        const string sql = """
            WITH ProveedoresUnicos AS (
                SELECT
                    p.nombre, p.cuit, p.iva_cate, p.iva_percep, p.ingre_brutos,
                    p.obser, p.agru_1, p.agru_2, p.agru_3, p.agru_4, p.socie_juri, p.nume_cai,
                    ROW_NUMBER() OVER (
                        PARTITION BY p.nombre, p.cuit 
                        ORDER BY (SELECT NULL)
                    ) AS FilaNumero
                FROM PROVEEDORES p
            )
            SELECT 
                nombre, cuit, iva_cate, iva_percep, ingre_brutos,
                obser, agru_1, agru_2, agru_3, agru_4, socie_juri, nume_cai
            FROM ProveedoresUnicos
            WHERE FilaNumero = 1
        """;

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var proveedor = new ProveedorSqlServer
            {
                nombre = reader["nombre"]
                    .ToString()!
                    .Trim(),

                cuit = reader["cuit"]
                    .ToString()!
                    .Trim(),

                iva_cate =
                    reader["iva_cate"] == DBNull.Value
                        ? null
                        : reader["iva_cate"].ToString()?.Trim(),

                iva_percep = Convert.ToBoolean(reader["iva_percep"]),

                ingre_brutos = reader["ingre_brutos"]
                    .ToString()!
                    .Trim(),

                obser = reader["obser"]
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

                socie_juri = reader["socie_juri"]
                    .ToString()!
                    .Trim(),

                nume_cai = reader["nume_cai"]
                    .ToString()!
                    .Trim(),                       
            };

            resultado.Add(proveedor);
        }

        return resultado;
    }

    private static int? ObtenerAgruProveId(
        string? codigo,
        Dictionary<string, int> agruProve)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return null;
        }

        if (agruProve.TryGetValue(
                codigo.Trim(),
                out var id))
        {
            return id;
        }

        throw new InvalidOperationException(
            $"No se encontró AgruProve con código '{codigo}'."
        );
    }    

    private List<ProveedorOracle> Transformar(
        List<ProveedorSqlServer> origen,
        Dictionary<string, int> categoriasIva,
        Dictionary<string, int> agruProve
    ){
        var resultado = new List<ProveedorOracle>();

        foreach (var item in origen)
        {
            int? categoriaIvaId = null;

            if (!string.IsNullOrWhiteSpace(item.iva_cate))
            {
                var codigoCategoria = item.iva_cate.Trim();

                if (categoriasIva.TryGetValue(
                        codigoCategoria,
                        out var id))
                {
                    categoriaIvaId = id;
                }
                else
                {
                    Log.Warning(
                        "No se encontró la categoría IVA {CategoriaIva} " +
                        "para el proveedor CUIT {Cuit}. " +
                        "Se insertará CATEGORIASIVAID = NULL.",
                        codigoCategoria,
                        item.cuit
                    );
                }
            }

            var proveedor = new ProveedorOracle
            {
                nombre = item.nombre.Trim(),
                cuit = item.cuit.Trim(),
                iva_cate = categoriaIvaId,
                iva_percep = item.iva_percep,
                ingre_brutos = item.ingre_brutos?.Trim() ?? string.Empty,  //ingre_brutos = item.ingre_brutos.Trim(),                                  
                obser = item.obser?.Trim() ?? string.Empty,  //obser = item.obser.Trim(),
                agru_1 = ObtenerAgruProveId(item.agru_1, agruProve),
                agru_2 = ObtenerAgruProveId(item.agru_2, agruProve),
                agru_3 = ObtenerAgruProveId(item.agru_3, agruProve),
                agru_4 = ObtenerAgruProveId(item.agru_4, agruProve),
                socie_juri = item.socie_juri?.Trim() ?? string.Empty,  //socie_juri = item.socie_juri.Trim(),
                nume_cai = item.nume_cai?.Trim() ?? string.Empty,   //nume_cai = item.nume_cai.Trim(),                                                                                                                                               
            };
            resultado.Add(proveedor);
        }

        return resultado;
    }

    private async Task InsertarOracleAsync(
        List<ProveedorOracle> proveedor)
    {
        await using var connection =
            _oracleDatabase.CrearConexion();

        await connection.OpenAsync();

        using var transaction =
            connection.BeginTransaction();
        try
        {
            const string sql = """
                INSERT INTO PROVEEDORES
                (
                    PROVEEDORESRAZONSOC,
                    PROVEEDORESCUIT,
                    CATEGORIASIVAID,
                    PROVEEDORESPERCIBEIVA,
                    PROVEEDORESINGRESOSBRUTOS,
                    PROVEEDORESOBSERV,
                    PROVEEDORESAGRUPROVEID1,
                    PROVEEDORESAGRUPROVEID2,
                    PROVEEDORESAGRUPROVEID3,
                    PROVEEDORESAGRUPROVEID4,
                    PROVEEDORESSOCJURI,
                    PROVEEDORESNUMEROCAI,
                    PROVEEDORESFCHALTA,
                    PROVEEDORESFCHBAJA,
                    PROVEEDORESUSUARIOSIDALTA,
                    PROVEEDORESUSUARIOSIDBAJA
                )
                VALUES
                (                    
                    :nombre, 
                    :cuit,
                    :iva_cate, 
                    :iva_percep,  
                    :ingre_brutos, 
                    :obser,  
                    :agru_1, 
                    :agru_2,    
                    :agru_3, 
                    :agru_4,     
                    :socie_juri, 
                    :nume_cai,
                    SYSDATE,
                    NULL,
                    1,
                    NULL
                )
                """;

            foreach (var prove in proveedor)
            {
                await using var command =
                    new OracleCommand(sql, connection);

                command.Transaction = transaction;

                command.BindByName = true;

                command.Parameters.Add(
                    "nombre",
                    OracleDbType.NVarchar2
                ).Value = prove.nombre;

                command.Parameters.Add(
                    "cuit",
                    OracleDbType.NVarchar2
                ).Value = prove.cuit;

                command.Parameters.Add(
                    "iva_cate",
                    OracleDbType.Int32
                ).Value =
                    prove.iva_cate.HasValue
                        ? prove.iva_cate.Value
                        : DBNull.Value;

                command.Parameters.Add(
                    "iva_percep",
                    OracleDbType.Int32
                ).Value = prove.iva_percep ? 1 : 0;

                command.Parameters.Add(
                    "ingre_brutos",
                    OracleDbType.NVarchar2
                ).Value = prove.ingre_brutos;

                command.Parameters.Add(
                    "obser",
                    OracleDbType.NVarchar2
                ).Value = prove.obser;

                command.Parameters.Add(
                    "agru_1",
                    OracleDbType.Int32
                ).Value =
                    prove.agru_1.HasValue
                        ? prove.agru_1.Value
                        : DBNull.Value;

                command.Parameters.Add(
                    "agru_2",
                    OracleDbType.Int32
                ).Value =
                    prove.agru_2.HasValue
                        ? prove.agru_2.Value
                        : DBNull.Value;

                command.Parameters.Add(
                    "agru_3",
                    OracleDbType.Int32
                ).Value =
                    prove.agru_3.HasValue
                        ? prove.agru_3.Value
                        : DBNull.Value;

                command.Parameters.Add(
                    "agru_4",
                    OracleDbType.Int32
                ).Value =
                    prove.agru_4.HasValue
                        ? prove.agru_4.Value
                        : DBNull.Value;

                command.Parameters.Add(
                    "socie_juri",
                    OracleDbType.NVarchar2
                ).Value = prove.socie_juri;

                command.Parameters.Add(
                    "nume_cai",
                    OracleDbType.NVarchar2
                ).Value = prove.nume_cai;

                await command.ExecuteNonQueryAsync();

                Log.Information(
                    "Proveedor {cuit} - {nombre} insertado.",
                    prove.cuit,
                    prove.nombre);
            }

            transaction.Commit();

            Log.Information(
                "{Cantidad} registros insertados en PROVEEDOR.",
                proveedor.Count);
        }
        catch
        {
            transaction.Rollback();

            Log.Error(
                "Se realizó ROLLBACK de PROVEEDOR.");
            throw;
        }
    }
}