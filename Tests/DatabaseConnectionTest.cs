using System;
using Microsoft.Data.SqlClient;

namespace RamaFemenina.Tests
{
    public static class DatabaseConnectionTest
    {
        public static void TestConnection()
        {
            var connectionString = "Server=localhost;Database=Ramafemenina;Trusted_Connection=True;TrustServerCertificate=True;";
            
            try
            {
                Console.WriteLine("Intentando conectar a la base de datos...");
                Console.WriteLine($"Connection String: {connectionString}");
                
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("? Conexión exitosa!");
                    
                    using (var command = new SqlCommand("SELECT @@VERSION", connection))
                    {
                        var version = command.ExecuteScalar()?.ToString();
                        Console.WriteLine($"SQL Server Version: {version}");
                    }
                    
                    using (var command = new SqlCommand("SELECT DB_NAME()", connection))
                    {
                        var dbName = command.ExecuteScalar()?.ToString();
                        Console.WriteLine($"Base de datos actual: {dbName}");
                    }
                    
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM sys.tables", connection))
                    {
                        var tableCount = command.ExecuteScalar();
                        Console.WriteLine($"Número de tablas: {tableCount}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error de conexión: {ex.Message}");
                Console.WriteLine($"Tipo de error: {ex.GetType().Name}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
