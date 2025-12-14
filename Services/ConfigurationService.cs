using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace RamaFemenina.Services;

public class ConfigurationService
{
    private const string ConfigFileName = "dbconfig.json";
    private static readonly string ConfigFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, 
        ConfigFileName);

    public class DatabaseConfig
    {
        public string Server { get; set; } = "localhost";
        public string Database { get; set; } = "RamaFemenina";
        public string UserId { get; set; } = "";
        public string Password { get; set; } = "";
        public bool UseIntegratedSecurity { get; set; } = true;
        public bool TrustServerCertificate { get; set; } = true;
        public int ConnectionTimeout { get; set; } = 30;
    }

    public class NcfSequenceConfig
    {
        public string Prefix { get; set; } = "B01";  // Prefijo del NCF (ej: B01, B02, etc.)
        public int CurrentSequence { get; set; } = 1;  // Número secuencial actual
        public int MaxSequence { get; set; } = 9999999;  // Límite máximo (7 dígitos)
        public DateTime? ExpirationDate { get; set; }  // Fecha de vencimiento del rango
        public string SerialNumber { get; set; } = "";  // Número de serie autorizado por DGII
        public bool AutoIncrement { get; set; } = true;  // Auto-incrementar después de usar
    }

    public class ChequeSequenceConfig
    {
        public string BankName { get; set; } = "";  // Nombre del banco
        public string AccountNumber { get; set; } = "";  // Número de cuenta
        public int CurrentSequence { get; set; } = 1;  // Número secuencial actual
        public int StartNumber { get; set; } = 1;  // Número inicial del talonario
        public int EndNumber { get; set; } = 100;  // Número final del talonario
        public bool AutoIncrement { get; set; } = true;  // Auto-incrementar después de usar
    }

    public class AppConfig
    {
        public DatabaseConfig Database { get; set; } = new();
        public NcfSequenceConfig NcfSequence { get; set; } = new();
        public ChequeSequenceConfig ChequeSequence { get; set; } = new();
    }

    /// <summary>
    /// Carga la configuración de la base de datos desde el archivo (SYNC)
    /// </summary>
    public static DatabaseConfig LoadConfiguration()
    {
        var appConfig = LoadAppConfiguration();
        return appConfig.Database;
    }

    /// <summary>
    /// Carga la configuración completa de la aplicación (SYNC)
    /// </summary>
    public static AppConfig LoadAppConfiguration()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                return config ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar configuración: {ex.Message}");
        }

        return new AppConfig();
    }

    /// <summary>
    /// Carga la configuración de la base de datos desde el archivo (ASYNC)
    /// </summary>
    public static async Task<DatabaseConfig> LoadConfigurationAsync()
    {
        var appConfig = await LoadAppConfigurationAsync();
        return appConfig.Database;
    }

    /// <summary>
    /// Carga la configuración completa de la aplicación (ASYNC)
    /// </summary>
    public static async Task<AppConfig> LoadAppConfigurationAsync()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = await File.ReadAllTextAsync(ConfigFilePath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                return config ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar configuración: {ex.Message}");
        }

        return new AppConfig();
    }

    /// <summary>
    /// Guarda la configuración de la base de datos en el archivo
    /// </summary>
    public static async Task<bool> SaveConfigurationAsync(DatabaseConfig config)
    {
        var appConfig = await LoadAppConfigurationAsync();
        appConfig.Database = config;
        return await SaveAppConfigurationAsync(appConfig);
    }

    /// <summary>
    /// Guarda la configuración completa de la aplicación en el archivo
    /// </summary>
    public static async Task<bool> SaveAppConfigurationAsync(AppConfig config)
    {
        try
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true 
            };
            
            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(ConfigFilePath, json);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al guardar configuración: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene el próximo número de NCF y lo incrementa automáticamente si está configurado
    /// </summary>
    public static async Task<(bool Success, string NCF, string Message)> GetNextNcfAsync()
    {
        try
        {
            var config = await LoadAppConfigurationAsync();
            var ncfConfig = config.NcfSequence;

            // Validar fecha de expiración
            if (ncfConfig.ExpirationDate.HasValue && DateTime.Now > ncfConfig.ExpirationDate.Value)
            {
                return (false, "", "El rango de NCF ha expirado. Por favor, configure un nuevo rango autorizado.");
            }

            // Validar que no se exceda el límite
            if (ncfConfig.CurrentSequence > ncfConfig.MaxSequence)
            {
                return (false, "", $"Se ha alcanzado el límite máximo del rango de NCF ({ncfConfig.MaxSequence}). Por favor, solicite un nuevo rango a la DGII.");
            }

            // Generar NCF: Formato estándar dominicano es B01 + 10 dígitos
            // Ejemplo: B0100000001
            string ncf = $"{ncfConfig.Prefix}{ncfConfig.CurrentSequence:D10}";

            // Incrementar si está configurado
            if (ncfConfig.AutoIncrement)
            {
                ncfConfig.CurrentSequence++;
                await SaveAppConfigurationAsync(config);
            }

            return (true, ncf, "NCF generado correctamente");
        }
        catch (Exception ex)
        {
            return (false, "", $"Error al generar NCF: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene el próximo número de cheque y lo incrementa automáticamente si está configurado
    /// </summary>
    public static async Task<(bool Success, int ChequeNumber, string Message)> GetNextChequeNumberAsync()
    {
        try
        {
            var config = await LoadAppConfigurationAsync();
            var chequeConfig = config.ChequeSequence;

            // Validar que no se exceda el límite del talonario
            if (chequeConfig.CurrentSequence > chequeConfig.EndNumber)
            {
                return (false, 0, $"Se han agotado los cheques del talonario actual (hasta {chequeConfig.EndNumber}). Por favor, configure un nuevo talonario.");
            }

            int chequeNumber = chequeConfig.CurrentSequence;

            // Incrementar si está configurado
            if (chequeConfig.AutoIncrement)
            {
                chequeConfig.CurrentSequence++;
                await SaveAppConfigurationAsync(config);
            }

            return (true, chequeNumber, "Número de cheque generado correctamente");
        }
        catch (Exception ex)
        {
            return (false, 0, $"Error al generar número de cheque: {ex.Message}");
        }
    }

    /// <summary>
    /// Actualiza la configuración de secuencia de NCF
    /// </summary>
    public static async Task<bool> UpdateNcfSequenceAsync(NcfSequenceConfig ncfConfig)
    {
        try
        {
            var config = await LoadAppConfigurationAsync();
            config.NcfSequence = ncfConfig;
            return await SaveAppConfigurationAsync(config);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al actualizar configuración de NCF: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Actualiza la configuración de secuencia de cheques
    /// </summary>
    public static async Task<bool> UpdateChequeSequenceAsync(ChequeSequenceConfig chequeConfig)
    {
        try
        {
            var config = await LoadAppConfigurationAsync();
            config.ChequeSequence = chequeConfig;
            return await SaveAppConfigurationAsync(config);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al actualizar configuración de cheques: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Genera un connection string basado en la configuración
    /// </summary>
    public static string BuildConnectionString(DatabaseConfig config)
    {
        if (config.UseIntegratedSecurity)
        {
            return $"Server={config.Server};" +
                   $"Database={config.Database};" +
                   $"Integrated Security=True;" +
                   $"TrustServerCertificate={config.TrustServerCertificate};" +
                   $"Connection Timeout={config.ConnectionTimeout};";
        }
        else
        {
            return $"Server={config.Server};" +
                   $"Database={config.Database};" +
                   $"User Id={config.UserId};" +
                   $"Password={config.Password};" +
                   $"TrustServerCertificate={config.TrustServerCertificate};" +
                   $"Connection Timeout={config.ConnectionTimeout};";
        }
    }

    /// <summary>
    /// Prueba la conexión a la base de datos
    /// </summary>
    public static async Task<(bool Success, string Message)> TestConnectionAsync(DatabaseConfig config)
    {
        try
        {
            var connectionString = BuildConnectionString(config);
            
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync();
            
            return (true, "Conexión exitosa a la base de datos");
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexión: {ex.Message}");
        }
    }
}
