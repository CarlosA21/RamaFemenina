using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using RamaFemenina.Data;
using RamaFemenina.Services;
using System.IO;
using DevExpress.XtraReports.Security;

namespace RamaFemenina
{
    public partial class App : Application
    {
        private Window? _window;
        private IServiceProvider? _serviceProvider;
        private static string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_error_log.txt");

        public Window? CurrentWindow => _window;
        public IServiceProvider Services => _serviceProvider!;

        public App()
        {
            try
            {
                // IMPORTANTE: Configurar DevExpress para permitir ejecución de scripts
                ScriptPermissionManager.GlobalInstance = new ScriptPermissionManager(ExecutionMode.Unrestricted);
                
                // Configurar manejo de excepciones no capturadas
                this.UnhandledException += App_UnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

                LogInfo("===== INICIO DE APLICACION =====");
                LogInfo($"Directorio base: {AppDomain.CurrentDomain.BaseDirectory}");
                LogInfo($"Versión .NET: {Environment.Version}");
                LogInfo($"OS: {Environment.OSVersion}");

                InitializeComponent();
                LogInfo("InitializeComponent completado");

                ConfigureServices();
                LogInfo("Servicios configurados exitosamente");
                
                // Verificar conectividad a base de datos
                Task.Run(async () => await VerifyDatabaseConnection());
            }
            catch (Exception ex)
            {
                LogError("ERROR CRÍTICO en constructor App:", ex);
                throw;
            }
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            System.Diagnostics.Debug.WriteLine($"[APP] Configurando servicios...");
            LogInfo("Configurando servicios...");

            // Connection string por defecto
            string connectionString = "Server=localhost;Database=RamaFemenina;Integrated Security=True;TrustServerCertificate=True;";
            
            // Intentar cargar desde appsettings.json
            try
            {
                var configPath = System.IO.Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                LogInfo($"Buscando configuración en: {configPath}");
                
                if (System.IO.File.Exists(configPath))
                {
                    LogInfo("appsettings.json encontrado");
                    
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: true)
                        .Build();

                    services.AddSingleton<IConfiguration>(configuration);

                    var cs = configuration.GetConnectionString("DefaultConnection");
                    if (!string.IsNullOrEmpty(cs))
                    {
                        connectionString = cs;
                        LogInfo("Connection string cargado desde appsettings.json");
                        
                        // Log de información de la conexión (sin contraseña)
                        var csWithoutPassword = HidePassword(cs);
                        LogInfo($"Connection String: {csWithoutPassword}");
                    }
                    else
                    {
                        LogInfo("No se encontró DefaultConnection en appsettings.json, usando valores por defecto");
                    }
                }
                else
                {
                    LogInfo("appsettings.json NO EXISTE, usando configuración por defecto");
                    LogInfo($"Archivos en directorio base:");
                    foreach (var file in Directory.GetFiles(AppContext.BaseDirectory))
                    {
                        LogInfo($"  - {Path.GetFileName(file)}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error al cargar appsettings.json:", ex);
                System.Diagnostics.Debug.WriteLine($"[APP] Error: {ex.Message}");
            }

            // Configurar servicios
            try
            {
                LogInfo("Configurando DbContext...");
                services.AddDbContext<RamaFemeninaContext>(options =>
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        // Habilitar reintentos automáticos para errores transitorios
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);
                        
                        // Configurar timeouts
                        sqlOptions.CommandTimeout(60); // 60 segundos para comandos SQL
                    }));
                LogInfo("DbContext configurado con resiliencia de reintentos");

                LogInfo("Registrando servicios...");
                services.AddScoped<AuthenticationService>();
                services.AddScoped<FacturaService>();
                services.AddScoped<DataCacheService>(provider => new DataCacheService(provider));
                services.AddScoped<CrystalReportService>();
                services.AddScoped<SimpleReportService>();
                services.AddScoped<ReportManager>();
                LogInfo("Servicios registrados");

                _serviceProvider = services.BuildServiceProvider();
                System.Diagnostics.Debug.WriteLine($"[APP] ✅ Servicios configurados");
                LogInfo("ServiceProvider creado exitosamente");
            }
            catch (Exception ex)
            {
                LogError("Error al configurar servicios:", ex);
                throw;
            }
        }

        private async Task VerifyDatabaseConnection()
        {
            try
            {
                LogInfo("===== VERIFICACION DE CONEXION A BASE DE DATOS =====");
                
                using (var scope = _serviceProvider!.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                    
                    LogInfo("Intentando conectar a la base de datos...");
                    
                    var canConnect = await dbContext.Database.CanConnectAsync();
                    
                    if (canConnect)
                    {
                        LogInfo("✅ CONEXION A BASE DE DATOS EXITOSA");
                        
                        // Obtener información adicional
                        var dbName = dbContext.Database.GetDbConnection().Database;
                        LogInfo($"Base de datos: {dbName}");
                    }
                    else
                    {
                        LogInfo("❌ NO SE PUEDE CONECTAR A LA BASE DE DATOS");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("❌ ERROR AL CONECTAR A LA BASE DE DATOS:", ex);
                LogInfo("POSIBLES CAUSAS:");
                LogInfo("1. El servidor SQL Server no está accesible");
                LogInfo("2. Las credenciales son incorrectas");
                LogInfo("3. La base de datos no existe");
                LogInfo("4. Firewall bloqueando la conexión");
                LogInfo("5. El archivo appsettings.json no se copió correctamente");
            }
        }

        private string HidePassword(string connectionString)
        {
            try
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                if (!string.IsNullOrEmpty(builder.Password))
                {
                    builder.Password = "***";
                }
                return builder.ConnectionString;
            }
            catch
            {
                return connectionString.Contains("Password=") 
                    ? System.Text.RegularExpressions.Regex.Replace(connectionString, @"Password=[^;]*", "Password=***")
                    : connectionString;
            }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                LogInfo("OnLaunched iniciado");
                _window = new MainWindow();
                LogInfo("MainWindow creada");
                _window.Activate();
                LogInfo("MainWindow activada - Aplicación iniciada correctamente");
            }
            catch (Exception ex)
            {
                LogError("ERROR en OnLaunched:", ex);
                throw;
            }
        }

        public void NavigateToHome(string userName)
        {
            try
            {
                LogInfo($"Navegando a HomeWindow para usuario: {userName}");
                var homeWindow = new HomeWindow();
                homeWindow.SetUserName(userName);
                homeWindow.Activate();

                _window?.Close();
                _window = homeWindow;
                LogInfo("Navegación a HomeWindow completada");
            }
            catch (Exception ex)
            {
                LogError("Error al navegar a HomeWindow:", ex);
                throw;
            }
        }

        #region Event Handlers para Excepciones No Capturadas

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            LogError("EXCEPCIÓN NO CAPTURADA (UnhandledException):", e.Exception);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogError("EXCEPCIÓN NO CAPTURADA (AppDomain):", ex);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogError("EXCEPCIÓN NO OBSERVADA (Task):", e.Exception);
            e.SetObserved();
        }

        #endregion

        #region Métodos de Logging

        private static void LogInfo(string message)
        {
            Log($"[INFO] {message}");
        }

        private static void LogError(string message, Exception ex)
        {
            Log($"[ERROR] {message}");
            Log($"  Tipo: {ex.GetType().FullName}");
            Log($"  Mensaje: {ex.Message}");
            Log($"  Stack Trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Log($"  Inner Exception: {ex.InnerException.Message}");
                Log($"  Inner Stack Trace: {ex.InnerException.StackTrace}");
            }
        }

        private static void Log(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] {message}";
                
                // Escribir al archivo de log
                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
                
                // También escribir a Debug para Visual Studio
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
            catch
            {
                // Si falla el logging, no hacer nada para evitar crashes
            }
        }

        #endregion
    }
}
