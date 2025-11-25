using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using RamaFemenina.Data;
using RamaFemenina.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RamaFemenina
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        private IServiceProvider? _serviceProvider;

        public Window? CurrentWindow => _window;

        public IServiceProvider Services => _serviceProvider!;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            ConfigureServices();
        }

        private void ConfigureServices()
        {
            try
            {
                var services = new ServiceCollection();

                System.Diagnostics.Debug.WriteLine($"[APP] Configurando servicios...");
                System.Diagnostics.Debug.WriteLine($"[APP] Base Directory: {AppContext.BaseDirectory}");

                // Configuración
                var configPath = System.IO.Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                System.Diagnostics.Debug.WriteLine($"[APP] Buscando appsettings.json en: {configPath}");
                
                if (!System.IO.File.Exists(configPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[APP] ❌ ERROR: appsettings.json NO ENCONTRADO");
                    System.Diagnostics.Debug.WriteLine($"[APP] Archivos en directorio:");
                    foreach (var file in System.IO.Directory.GetFiles(AppContext.BaseDirectory, "*.json"))
                    {
                        System.Diagnostics.Debug.WriteLine($"[APP]   - {System.IO.Path.GetFileName(file)}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[APP] ✅ appsettings.json encontrado");
                }

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                System.Diagnostics.Debug.WriteLine($"[APP] Configuración cargada");

                // Registrar IConfiguration como Singleton para que esté disponible en todos los servicios
                services.AddSingleton<IConfiguration>(configuration);

                // DbContext
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                System.Diagnostics.Debug.WriteLine($"[APP] Connection String leída: {connectionString}");

                if (string.IsNullOrEmpty(connectionString))
                {
                    System.Diagnostics.Debug.WriteLine($"[APP] ❌ ERROR: Connection String está vacía");
                    throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada en appsettings.json");
                }

                services.AddDbContext<RamaFemeninaContext>(options =>
                {
                    System.Diagnostics.Debug.WriteLine($"[APP] Configurando DbContext con: {connectionString}");
                    options.UseSqlServer(connectionString);
                });

                System.Diagnostics.Debug.WriteLine($"[APP] DbContext configurado");

                // Servicios de Negocio
                services.AddScoped<AuthenticationService>();
                services.AddScoped<FacturaService>();
                System.Diagnostics.Debug.WriteLine($"[APP] ✅ Servicios de negocio registrados");

                // Servicios de Reportes
                services.AddScoped<CrystalReportService>();  // Reportes Crystal (configuración automática de BD)
                services.AddScoped<SimpleReportService>();    // Reportes PDF simples con iText
                services.AddScoped<ReportManager>();          // Gestor unificado de reportes
                System.Diagnostics.Debug.WriteLine($"[APP] ✅ Servicios de reportes registrados");

                _serviceProvider = services.BuildServiceProvider();
                System.Diagnostics.Debug.WriteLine($"[APP] ✅ ServiceProvider construido exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APP] ❌❌❌ ERROR EN ConfigureServices ❌❌❌");
                System.Diagnostics.Debug.WriteLine($"[APP] Tipo: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[APP] Mensaje: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[APP] Stack Trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[APP] Inner Exception: {ex.InnerException.Message}");
                }
                
                throw; // Re-lanzar para que la app no inicie en estado inválido
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }

        public void NavigateToHome(string userName)
        {
            var homeWindow = new HomeWindow();
            homeWindow.SetUserName(userName);
            homeWindow.Activate();

            // Cerrar la ventana de login
            _window?.Close();
            _window = homeWindow;
        }
    }
}
