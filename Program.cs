// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!"); // original

/*
// PRIMERA PRUEBA --> OK!!!
using Microsoft.Extensions.Configuration;

Console.WriteLine("==============================================");
Console.WriteLine(" MIGRADOR SQL SERVER 2005 -> ORACLE 11g");
Console.WriteLine(" Versión 1.0.0");
Console.WriteLine("==============================================");
Console.WriteLine();

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

Console.WriteLine("Configuración cargada correctamente.");
Console.WriteLine();

var sqlServerConnection =
    configuration.GetConnectionString("SqlServer");

var oracleConnection =
    configuration.GetConnectionString("Oracle");

Console.WriteLine(
    string.IsNullOrWhiteSpace(sqlServerConnection)
        ? "SQL Server: cadena de conexión pendiente."
        : "SQL Server: configuración encontrada."
);

Console.WriteLine(
    string.IsNullOrWhiteSpace(oracleConnection)
        ? "Oracle: cadena de conexión pendiente."
        : "Oracle: configuración encontrada."
);

Console.WriteLine();
Console.WriteLine("Fin.");
*/

/*
// SEGUNDA PRUEBA --> OK!!!
using Microsoft.Extensions.Configuration;
using Serilog;

Console.WriteLine("==============================================");
Console.WriteLine(" MIGRADOR SQL SERVER 2005 -> ORACLE 11g");
Console.WriteLine(" Versión 1.0.0");
Console.WriteLine("==============================================");
Console.WriteLine();

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var logPath = configuration["Logging:Path"] ?? "logs/migrador-.log";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        logPath,
        rollingInterval: RollingInterval.Day
    )
    .CreateLogger();

try
{
    Log.Information("Iniciando migrador.");

    var sqlServerConnection =
        configuration.GetConnectionString("SqlServer");

    var oracleConnection =
        configuration.GetConnectionString("Oracle");

    if (string.IsNullOrWhiteSpace(sqlServerConnection))
    {
        Log.Warning("SQL Server: cadena de conexión pendiente.");
    }
    else
    {
        Log.Information("SQL Server: configuración encontrada.");
    }

    if (string.IsNullOrWhiteSpace(oracleConnection))
    {
        Log.Warning("Oracle: cadena de conexión pendiente.");
    }
    else
    {
        Log.Information("Oracle: configuración encontrada.");
    }

    Log.Information("Inicialización finalizada correctamente.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Ocurrió un error fatal durante la inicialización.");
}
finally
{
    Log.CloseAndFlush();
}
*/

/* 
// TERCERA PRUEBA --> OK!!!
using Microsoft.Extensions.Configuration;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Database.Oracle;
using Serilog;

Console.WriteLine("==============================================");
Console.WriteLine(" MIGRADOR SQL SERVER 2005 -> ORACLE 11g");
Console.WriteLine(" Versión 1.0.0");
Console.WriteLine("==============================================");
Console.WriteLine();

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var logPath =
    configuration["Logging:Path"]
    ?? "logs/migrador-.log";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        logPath,
        rollingInterval: RollingInterval.Day
    )
    .CreateLogger();

try
{
    Log.Information("Iniciando migrador.");

    var sqlServerConnection =
        configuration.GetConnectionString("SqlServer");

    var oracleConnection =
        configuration.GetConnectionString("Oracle");

    if (string.IsNullOrWhiteSpace(sqlServerConnection))
    {
        Log.Warning("SQL Server: cadena de conexión pendiente.");
    }
    else
    {
        try
        {
            Log.Information("Probando conexión a SQL Server...");

            var sqlServerDatabase =
                new SqlServerDatabase(sqlServerConnection);

            await sqlServerDatabase.ProbarConexionAsync();

            Log.Information("SQL Server: conexión correcta.");
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "SQL Server: error al intentar establecer la conexión."
            );
        }
    }

    if (string.IsNullOrWhiteSpace(oracleConnection))
    {
        Log.Warning("Oracle: cadena de conexión pendiente.");
    }
    else
    {
        try
        {
            Log.Information("Probando conexión a Oracle...");

            var oracleDatabase =
                new OracleDatabase(oracleConnection);

            await oracleDatabase.ProbarConexionAsync();

            Log.Information("Oracle: conexión correcta.");
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Oracle: error al intentar establecer la conexión."
            );
        }
    }

    Log.Information("Inicialización finalizada.");
}
catch (Exception ex)
{
    Log.Fatal(
        ex,
        "Ocurrió un error fatal durante la inicialización."
    );
}
finally
{
    Log.CloseAndFlush();
}*/

using Microsoft.Extensions.Configuration;
using MigradorSqlServerOracle.Database.SqlServer;
using MigradorSqlServerOracle.Database.Oracle;
using MigradorSqlServerOracle.Migrations;
using Serilog;

Console.WriteLine("==============================================");
Console.WriteLine(" MIGRADOR SQL SERVER 2005 -> ORACLE 11g");
Console.WriteLine(" Versión 1.0.0");
Console.WriteLine("==============================================");
Console.WriteLine();

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var logPath =
    configuration["Logging:Path"]
    ?? "logs/migrador-.log";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        logPath,
        rollingInterval: RollingInterval.Day
    )
    .CreateLogger();

try
{
    Log.Information("Iniciando migrador.");

    var sqlServerConnection =
        configuration.GetConnectionString("SqlServer");

    var oracleConnection =
        configuration.GetConnectionString("Oracle");

    // ============================================
    // PRUEBA CONEXIÓN SQL SERVER
    // ============================================

    if (string.IsNullOrWhiteSpace(sqlServerConnection))
    {
        Log.Warning("SQL Server: cadena de conexión pendiente.");
    }
    else
    {
        try
        {
            Log.Information("Probando conexión a SQL Server...");

            var sqlServerDatabase =
                new SqlServerDatabase(sqlServerConnection);

            await sqlServerDatabase.ProbarConexionAsync();

            Log.Information("SQL Server: conexión correcta.");
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "SQL Server: error al intentar establecer la conexión."
            );
        }
    }

    // ============================================
    // PRUEBA CONEXIÓN ORACLE
    // ============================================

    if (string.IsNullOrWhiteSpace(oracleConnection))
    {
        Log.Warning("Oracle: cadena de conexión pendiente.");
    }
    else
    {
        try
        {
            Log.Information("Probando conexión a Oracle...");

            var oracleDatabase =
                new OracleDatabase(oracleConnection);

            await oracleDatabase.ProbarConexionAsync();

            Log.Information("Oracle: conexión correcta.");
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Oracle: error al intentar establecer la conexión."
            );
        }
    }

    // ============================================
    // EJECUCIÓN DE MIGRACIONES
    // ============================================

    if (args.Length > 0)
    {
        var comando = args[0].ToLowerInvariant();

        // PARA EJECUTAR CADA MIGRACION POR SEPARADO
        switch (comando)
        {
            case "unidadesoperativas":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración LOCALIDADES -> LOCALIDADES."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new UnidadesOperativasMigration(
                        //sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }

            case "localidades":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración LOCALIDADES -> LOCALIDADES."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new LocalidadesMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }

            case "categoriasiva":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración CATEGORIAS_IVA -> CATEGORIASIVA."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new CategoriasIvaMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }

            case "agruprove":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración PROVE_AGRU -> AGRUPROVE."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new AgruProveMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }
        
            case "proveedores":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración PROVEEDORES -> PROVEEDORES."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new ProveedorMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }

            case "sectores":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración PROVEEDORES -> PROVEEDORES."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new SectoresMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }

            case "gruposautorizacion":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración PROVEEDORES -> PROVEEDORES."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new GruposAutorizacionMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }         

            case "comprobantes":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración PROVEEDORES -> PROVEEDORES."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new ComprobantesMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }       

            case "plancuentasrela":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración PLAN_CUENTAS_RELA -> PLANCUENTASRELA."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new PlanCuentasRelaMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }         

            case "plancuentas":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración PLAN_CUENTAS_RELA -> PLANCUENTAS."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new PlanCuentasMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }                 

            case "rubroporcuenta":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración PLAN_CUENTAS_RELA -> PLANCUENTASRELA."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new RubroPorCuentaMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }

            case "gruposproducto":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración GRUPOS -> GRUPOSPRODUCTO."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new GruposProductoMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }

            case "subgruposproduc":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración PRODUCTOS -> PRODUCTOS."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new SubGruposProducMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }

            case "productos":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración PRODUCTOS -> PRODUCTOS."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new ProductosMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }

            case "existencias":
            {
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a SQL Server."
                    );

                    break;
                }

                if (string.IsNullOrWhiteSpace(oracleConnection))
                {
                    Log.Error(
                        "No se puede ejecutar la migración: falta la conexión a Oracle."
                    );

                    break;
                }

                Log.Information(
                    "Ejecutando migración PRODUCTOS, PRODUC_DETA, DESPOSITOS -> EXITENCIAS."
                );

                var sqlServerDatabase =
                    new SqlServerDatabase(sqlServerConnection);

                var oracleDatabase =
                    new OracleDatabase(oracleConnection);

                var migration =
                    new ExistenciasMigration(
                        sqlServerDatabase,
                        oracleDatabase
                    );

                await migration.EjecutarAsync();

                break;
            }            

            default:
            {
                Log.Warning(
                    "Comando no reconocido: {Comando}",
                    comando
                );

                break;
            }
        }
    }

    Log.Information("Inicialización finalizada.");
}
catch (Exception ex)
{
    Log.Fatal(
        ex,
        "Ocurrió un error fatal durante la inicialización."
    );
}
finally
{
    Log.CloseAndFlush();
}