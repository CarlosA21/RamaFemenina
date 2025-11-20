using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RamaFemenina.Services;

namespace RamaFemenina.Utilities
{
    /// <summary>
    /// Utilidad para generar hashes de contraseñas BCrypt
    /// Útil para crear usuarios manualmente o para testing
    /// </summary>
    public static class PasswordHashUtility
    {
        /// <summary>
        /// Genera un hash BCrypt de una contraseña
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <returns>Hash BCrypt de la contraseña</returns>
        public static string GenerateHash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));
            }

            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        /// <summary>
        /// Verifica una contraseña contra un hash BCrypt
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <param name="hash">Hash BCrypt</param>
        /// <returns>True si la contraseña coincide con el hash</returns>
        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Imprime un hash de contraseña para uso en scripts SQL
        /// </summary>
        /// <param name="username">Nombre de usuario</param>
        /// <param name="password">Contraseña en texto plano</param>
        public static void PrintSqlInsert(string username, string password)
        {
            var hash = GenerateHash(password);
            Console.WriteLine($"-- Usuario: {username}");
            Console.WriteLine($"-- Contraseña: {password}");
            Console.WriteLine($"INSERT INTO acceso (usuario, contraseña) VALUES ('{username}', '{hash}');");
            Console.WriteLine();
        }

        /// <summary>
        /// Genera y muestra hashes para usuarios comunes de prueba
        /// </summary>
        public static void GenerateTestUsers()
        {
            Console.WriteLine("=== USUARIOS DE PRUEBA ===");
            Console.WriteLine();
            
            PrintSqlInsert("admin", "admin123");
            PrintSqlInsert("usuario1", "password123");
            PrintSqlInsert("demo", "demo");
            
            Console.WriteLine("=== FIN DE USUARIOS DE PRUEBA ===");
        }

        /// <summary>
        /// Método de prueba para validar que BCrypt funciona correctamente
        /// </summary>
        public static void TestBCrypt()
        {
            Console.WriteLine("=== PRUEBA DE BCRYPT ===");
            
            string testPassword = "TestPassword123!";
            Console.WriteLine($"Contraseña de prueba: {testPassword}");
            
            // Generar hash
            string hash = GenerateHash(testPassword);
            Console.WriteLine($"Hash generado: {hash}");
            
            // Verificar contraseña correcta
            bool isValid = VerifyPassword(testPassword, hash);
            Console.WriteLine($"Verificación con contraseña correcta: {isValid}");
            
            // Verificar contraseña incorrecta
            bool isInvalid = VerifyPassword("WrongPassword", hash);
            Console.WriteLine($"Verificación con contraseña incorrecta: {isInvalid}");
            
            Console.WriteLine("=== FIN DE PRUEBA ===");
        }
    }
}
