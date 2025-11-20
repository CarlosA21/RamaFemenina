using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RamaFemenina.Data;
using BCrypt.Net;

namespace RamaFemenina.Services
{
    public class AuthenticationService
    {
        private readonly RamaFemeninaContext _context;

        public AuthenticationService(RamaFemeninaContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Valida las credenciales del usuario usando BCrypt para verificar la contraseña hasheada
        /// </summary>
        /// <param name="usuario">Nombre de usuario</param>
        /// <param name="contraseña">Contraseña en texto plano</param>
        /// <returns>True si las credenciales son válidas, False en caso contrario</returns>
        public async Task<bool> ValidarCredencialesAsync(string usuario, string contraseña)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contraseña))
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Usuario o contraseña vacíos");
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] Buscando usuario: '{usuario}'");
                
                var acceso = await _context.Accesos
                    .FirstOrDefaultAsync(a => a.Usuario == usuario);

                if (acceso == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ? Usuario '{usuario}' NO ENCONTRADO en la base de datos");
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Sugerencia: Ejecute SELECT * FROM acceso para ver usuarios existentes");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Usuario '{usuario}' encontrado en BD");
                System.Diagnostics.Debug.WriteLine($"[AUTH] Hash en BD: {acceso.Contraseña.Substring(0, Math.Min(20, acceso.Contraseña.Length))}...");
                System.Diagnostics.Debug.WriteLine($"[AUTH] Longitud del hash: {acceso.Contraseña.Length} caracteres");

                // Verificar la contraseña usando BCrypt
                // Si la contraseña en la BD no está hasheada (por compatibilidad con datos antiguos),
                // hacer comparación directa
                if (acceso.Contraseña.StartsWith("$2"))
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Contraseña hasheada con BCrypt detectada");
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Verificando contraseña con BCrypt...");
                    
                    bool resultado = BCrypt.Net.BCrypt.Verify(contraseña, acceso.Contraseña);
                    
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Resultado de BCrypt.Verify: {(resultado ? "? MATCH" : "? NO MATCH")}");

                    
                    if (!resultado)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AUTH] ?? La contraseña NO coincide con el hash");
                        System.Diagnostics.Debug.WriteLine($"[AUTH] Contraseña ingresada (primeros 3 chars): {contraseña.Substring(0, Math.Min(3, contraseña.Length))}...");
                        System.Diagnostics.Debug.WriteLine($"[AUTH] ¿Contraseña correcta? Verifique mayúsculas/minúsculas");
                    }
                    
                    return resultado;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ?? CONTRASEÑA EN TEXTO PLANO DETECTADA");
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Hash almacenado: '{acceso.Contraseña}'");
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Contraseña ingresada: '{contraseña}'");
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Comparación directa (inseguro)");
                    
                    bool resultado = acceso.Contraseña == contraseña;
                    
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Resultado: {(resultado ? "? MATCH" : "? NO MATCH")}");

                    
                    if (resultado)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AUTH] ?????? URGENTE: Migre esta contraseña a BCrypt ??????");
                        System.Diagnostics.Debug.WriteLine($"[AUTH] Ejecute: UPDATE acceso SET contraseña = '<hash_bcrypt>' WHERE usuario = '{usuario}'");
                    }
                    
                    return resultado;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ??? EXCEPCIÓN EN ValidarCredencialesAsync ???");
                System.Diagnostics.Debug.WriteLine($"[AUTH] Tipo: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[AUTH] Mensaje: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AUTH] Stack Trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Inner Exception: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// Hashea una contraseña usando BCrypt
        /// </summary>
        /// <param name="contraseña">Contraseña en texto plano</param>
        /// <returns>Contraseña hasheada</returns>
        public string HashearContraseña(string contraseña)
        {
            return BCrypt.Net.BCrypt.HashPassword(contraseña, workFactor: 12);
        }

        /// <summary>
        /// Verifica si la conexión a la base de datos está disponible
        /// </summary>
        public async Task<bool> VerificarConexionAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] Verificando conexión a la base de datos...");
                System.Diagnostics.Debug.WriteLine($"[AUTH] Connection String: {_context.Database.GetConnectionString()}");
                
                bool canConnect = await _context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ? Conexión exitosa a la base de datos");
                    
                    // Contar usuarios en la tabla acceso
                    try
                    {
                        int totalUsuarios = await _context.Accesos.CountAsync();
                        System.Diagnostics.Debug.WriteLine($"[AUTH] Total de usuarios en tabla 'acceso': {totalUsuarios}");
                        
                        if (totalUsuarios > 0)
                        {
                            var usuarios = await _context.Accesos.Select(a => a.Usuario).ToListAsync();
                            System.Diagnostics.Debug.WriteLine($"[AUTH] Usuarios encontrados: {string.Join(", ", usuarios)}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[AUTH] ?? La tabla 'acceso' está vacía. Cree usuarios primero.");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AUTH] ?? Error al contar usuarios: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ? No se pudo conectar a la base de datos");
                }
                
                return canConnect;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Excepción al verificar conexión");
                System.Diagnostics.Debug.WriteLine($"[AUTH] Tipo: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[AUTH] Mensaje: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Inner Exception: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// Crea un nuevo usuario con contraseña hasheada
        /// </summary>
        /// <param name="usuario">Nombre de usuario</param>
        /// <param name="contraseña">Contraseña en texto plano</param>
        /// <returns>True si se creó exitosamente, False en caso contrario</returns>
        public async Task<bool> CrearUsuarioAsync(string usuario, string contraseña)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contraseña))
            {
                return false;
            }

            try
            {
                // Verificar si el usuario ya existe
                var existeUsuario = await _context.Accesos
                    .AnyAsync(a => a.Usuario == usuario);

                if (existeUsuario)
                {
                    return false;
                }

                // Crear nuevo acceso con contraseña hasheada
                var nuevoAcceso = new Models.Acceso
                {
                    Usuario = usuario,
                    Contraseña = HashearContraseña(contraseña)
                };

                _context.Accesos.Add(nuevoAcceso);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al crear usuario: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cambia la contraseña de un usuario existente
        /// </summary>
        /// <param name="usuario">Nombre de usuario</param>
        /// <param name="contraseñaActual">Contraseña actual en texto plano</param>
        /// <param name="contraseñaNueva">Nueva contraseña en texto plano</param>
        /// <returns>True si se cambió exitosamente, False en caso contrario</returns>
        public async Task<bool> CambiarContraseñaAsync(string usuario, string contraseñaActual, string contraseñaNueva)
        {
            if (string.IsNullOrWhiteSpace(usuario) || 
                string.IsNullOrWhiteSpace(contraseñaActual) || 
                string.IsNullOrWhiteSpace(contraseñaNueva))
            {
                return false;
            }

            try
            {
                // Primero validar que las credenciales actuales sean correctas
                var esValido = await ValidarCredencialesAsync(usuario, contraseñaActual);
                if (!esValido)
                {
                    return false;
                }

                // Obtener el usuario
                var acceso = await _context.Accesos
                    .FirstOrDefaultAsync(a => a.Usuario == usuario);

                if (acceso == null)
                {
                    return false;
                }

                // Actualizar con la nueva contraseña hasheada
                acceso.Contraseña = HashearContraseña(contraseñaNueva);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cambiar contraseña: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Migra una contraseña de texto plano a BCrypt hash
        /// (Útil para migrar datos existentes)
        /// </summary>
        /// <param name="usuario">Nombre de usuario</param>
        /// <returns>True si se migró exitosamente, False en caso contrario</returns>
        public async Task<bool> MigrarContraseñaABCryptAsync(string usuario)
        {
            try
            {
                var acceso = await _context.Accesos
                    .FirstOrDefaultAsync(a => a.Usuario == usuario);

                if (acceso == null)
                {
                    return false;
                }

                // Solo migrar si la contraseña no está ya hasheada
                if (!acceso.Contraseña.StartsWith("$2"))
                {
                    acceso.Contraseña = HashearContraseña(acceso.Contraseña);
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false; // Ya estaba hasheada
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al migrar contraseña: {ex.Message}");
                return false;
            }
        }
    }
}
