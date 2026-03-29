using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RamaFemenina.Data;
using BCrypt.Net;
using System.Collections.Generic;

namespace RamaFemenina.Services
{
    public class AuthenticationService
    {
        private readonly RamaFemeninaContext _context;
        private static bool _conexionVerificada = false;
        private static DateTime _ultimaVerificacion = DateTime.MinValue;
        private static readonly TimeSpan _intervaloVerificacion = TimeSpan.FromMinutes(5);

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
                System.Diagnostics.Debug.WriteLine($"[AUTH] ?? Usuario o contraseña vacíos");
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] Buscando usuario: '{usuario}'");
                
                // Optimización: Usar AsNoTracking para consultas de solo lectura
                var acceso = await _context.Accesos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Usuario == usuario);

                if (acceso == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ? Usuario '{usuario}' NO ENCONTRADO en la base de datos");
                    
                    // **OPTIMIZADO: Solo verificar usuarios una vez si la conexión ya fue verificada**
                    if (!_conexionVerificada || DateTime.UtcNow - _ultimaVerificacion > _intervaloVerificacion)
                    {
                        var totalUsuarios = await _context.Accesos.AsNoTracking().CountAsync();
                        _conexionVerificada = true;
                        _ultimaVerificacion = DateTime.UtcNow;
                        
                        if (totalUsuarios == 0 && usuario.Equals("admin", StringComparison.OrdinalIgnoreCase))
                        {
                            System.Diagnostics.Debug.WriteLine($"[AUTH] ?? No hay usuarios en el sistema. Intentando crear usuario por defecto...");
                            var usuarioCreado = await CrearUsuarioPorDefectoSiNoExisteAsync();
                            if (usuarioCreado)
                            {
                                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Usuario por defecto creado. Reintentando validación...");
                                return await ValidarCredencialesAsync(usuario, contraseña);
                            }
                        }
                    }
                    
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Usuario '{usuario}' encontrado en BD");
                System.Diagnostics.Debug.WriteLine($"[AUTH] Hash en BD: {acceso.Contraseña.Substring(0, Math.Min(20, acceso.Contraseña.Length))}...");

                // Verificar la contraseña usando BCrypt
                if (acceso.Contraseña.StartsWith("$2"))
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Contraseña hasheada con BCrypt detectada");
                    
                    bool resultado = BCrypt.Net.BCrypt.Verify(contraseña, acceso.Contraseña);
                    
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Resultado de BCrypt.Verify: {(resultado ? "? MATCH" : "? NO MATCH")}");
                    
                    return resultado;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ?? CONTRASEÑA EN TEXTO PLANO DETECTADA");
                    
                    bool resultado = acceso.Contraseña == contraseña;
                    
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Resultado: {(resultado ? "? MATCH" : "? NO MATCH")}");
                    
                    if (resultado)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AUTH] ?????? URGENTE: Migre esta contraseña a BCrypt ??????");
                    }
                    
                    return resultado;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ??? EXCEPCIÓN EN ValidarCredencialesAsync ???");
                System.Diagnostics.Debug.WriteLine($"[AUTH] Tipo: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[AUTH] Mensaje: {ex.Message}");
                
                return false;
            }
        }

        /// <summary>
        /// Crea el usuario por defecto si no existe ningún usuario en el sistema
        /// </summary>
        /// <returns>True si se creó exitosamente</returns>
        public async Task<bool> CrearUsuarioPorDefectoSiNoExisteAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ?? Verificando necesidad de crear usuario por defecto...");
                
                // Optimización: Usar AsNoTracking para consultas de solo lectura
                var totalUsuarios = await _context.Accesos.AsNoTracking().CountAsync();
                
                if (totalUsuarios > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ?? Ya existen {totalUsuarios} usuario(s). No se necesita crear usuario por defecto");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[AUTH] ?? No hay usuarios. Creando usuario por defecto...");

                // Credenciales por defecto
                const string usuarioAdmin = "admin";
                const string contrasenaAdmin = "admin123";

                // Crear el usuario con rol Admin
                var resultado = await CrearUsuarioAsync(usuarioAdmin, contrasenaAdmin, "Admin");

                if (resultado)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ? Usuario por defecto '{usuarioAdmin}' creado exitosamente");
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ?? CREDENCIALES CREADAS:");
                    System.Diagnostics.Debug.WriteLine($"[AUTH]    Usuario: {usuarioAdmin}");
                    System.Diagnostics.Debug.WriteLine($"[AUTH]    Contraseña: {contrasenaAdmin}");
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ?? IMPORTANTE: Cambie esta contraseña después del primer login");
                }

                return resultado;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Error creando usuario por defecto: {ex.Message}");
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
        /// Verifica si la conexión a la base de datos está disponible (optimizada con cache)
        /// </summary>
        public async Task<bool> VerificarConexionAsync()
        {
            // Optimización: Evitar verificaciones frecuentes
            if (_conexionVerificada && DateTime.UtcNow - _ultimaVerificacion < _intervaloVerificacion)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Conexión ya verificada recientemente (cache)");
                return true;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] Verificando conexión a la base de datos...");
                
                // Optimización: Usar CanConnectAsync que es más ligero que operaciones complejas
                bool canConnect = await _context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ? Conexión exitosa a la base de datos");
                    
                    // Optimización: Solo contar usuarios si la conexión fue exitosa
                    try
                    {
                        int totalUsuarios = await _context.Accesos.AsNoTracking().CountAsync();
                        System.Diagnostics.Debug.WriteLine($"[AUTH] Total de usuarios en tabla 'acceso': {totalUsuarios}");
                        
                        if (totalUsuarios > 0)
                        {
                            // Optimización: Solo obtener nombres si hay pocos usuarios (evitar cargar listas grandes)
                            if (totalUsuarios <= 10)
                            {
                                var usuarios = await _context.Accesos
                                    .AsNoTracking()
                                    .Select(a => a.Usuario)
                                    .ToListAsync();
                                System.Diagnostics.Debug.WriteLine($"[AUTH] Usuarios encontrados: {string.Join(", ", usuarios)}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[AUTH] Sistema tiene {totalUsuarios} usuarios registrados");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[AUTH] ?? La tabla 'acceso' está vacía.");
                            System.Diagnostics.Debug.WriteLine($"[AUTH] ?? Intente hacer login con 'admin/admin123' para crear el usuario automáticamente");
                        }
                        
                        // Marcar como verificado
                        _conexionVerificada = true;
                        _ultimaVerificacion = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AUTH] ? Error al contar usuarios: {ex.Message}");
                        // Aún podemos continuar si la conexión básica funciona
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ? No se pudo conectar a la base de datos");
                    _conexionVerificada = false;
                }
                
                return canConnect;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Excepción al verificar conexión");
                System.Diagnostics.Debug.WriteLine($"[AUTH] Tipo: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[AUTH] Mensaje: {ex.Message}");
                
                _conexionVerificada = false;
                return false;
            }
        }

        /// <summary>
        /// Crea un nuevo usuario con contraseña hasheada (optimizada)
        /// </summary>
        /// <param name="usuario">Nombre de usuario</param>
        /// <param name="contraseña">Contraseña en texto plano</param>
        /// <param name="rol">Rol del usuario (Admin o Moderador)</param>
        /// <returns>True si se creó exitosamente, False en caso contrario</returns>
        public async Task<bool> CrearUsuarioAsync(string usuario, string contraseña, string rol = "Moderador")
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contraseña))
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Usuario o contraseña vacíos para crear usuario");
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ?? Creando nuevo usuario: '{usuario}' con rol '{rol}'");
                
                // Optimización: Verificar existencia con AsNoTracking
                var existeUsuario = await _context.Accesos
                    .AsNoTracking()
                    .AnyAsync(a => a.Usuario == usuario);

                if (existeUsuario)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ? El usuario '{usuario}' ya existe");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[AUTH] ?? Generando hash BCrypt para la contraseña...");
                
                var nuevoAcceso = new Models.Acceso
                {
                    Usuario = usuario,
                    Contraseña = HashearContraseña(contraseña),
                    Rol = rol
                };

                System.Diagnostics.Debug.WriteLine($"[AUTH] ?? Guardando usuario en la base de datos...");
                
                // Optimización: Usar transacción más eficiente
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    // Limpiar el change tracker para evitar conflictos
                    _context.ChangeTracker.Clear();
                    
                    _context.Accesos.Add(nuevoAcceso);
                    
                    var filasAfectadas = await _context.SaveChangesAsync();
                    
                    if (filasAfectadas > 0)
                    {
                        await transaction.CommitAsync();
                        
                        System.Diagnostics.Debug.WriteLine($"[AUTH] ? Usuario '{usuario}' creado exitosamente con rol '{rol}'");
                        System.Diagnostics.Debug.WriteLine($"[AUTH] ?? ID asignado: {nuevoAcceso.IdUsuario}");
                        
                        // Invalidar cache de conexión para forzar recarga
                        _conexionVerificada = false;

                        return true;
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        System.Diagnostics.Debug.WriteLine($"[AUTH] ? No se insertó ninguna fila");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Error al crear usuario '{usuario}': {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Inner Exception: {ex.InnerException.Message}");
                }
                
                // Método de respaldo con SQL directo
                System.Diagnostics.Debug.WriteLine($"[AUTH] ?? Intentando creación con SQL directo...");
                return await CrearUsuarioConSQLDirectoAsync(usuario, contraseña, rol);
            }
        }

        /// <summary>
        /// Método de respaldo: Crear usuario usando SQL directo (optimizado)
        /// </summary>
        private async Task<bool> CrearUsuarioConSQLDirectoAsync(string usuario, string contraseña, string rol = "Moderador")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ?? Creando usuario con SQL directo...");
                
                var hashContraseña = HashearContraseña(contraseña);
                
                // SQL directo optimizado
                var sql = @"INSERT INTO acceso (usuario, contraseña, rol) VALUES (@usuario, @contraseña, @rol)";
                
                var parametros = new[]
                {
                    new Microsoft.Data.SqlClient.SqlParameter("@usuario", usuario),
                    new Microsoft.Data.SqlClient.SqlParameter("@contraseña", hashContraseña),
                    new Microsoft.Data.SqlClient.SqlParameter("@rol", rol)
                };
                
                var filasAfectadas = await _context.Database.ExecuteSqlRawAsync(sql, parametros);
                
                if (filasAfectadas > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ? Usuario '{usuario}' creado con SQL directo");
                    
                    // Invalidar cache de conexión
                    _conexionVerificada = false;
                    
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ? SQL directo no insertó ninguna fila");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Error en SQL directo: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene la lista de todos los usuarios registrados (optimizada)
        /// </summary>
        /// <returns>Lista de nombres de usuario</returns>
        public async Task<List<string>> ObtenerUsuariosAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ?? Obteniendo lista de usuarios...");
                
                // Optimización: Usar AsNoTracking para consultas de solo lectura
                var usuarios = await _context.Accesos
                    .AsNoTracking()
                    .Select(a => a.Usuario)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[AUTH] ? {usuarios.Count} usuario(s) encontrado(s)");
                
                return usuarios;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Error al obtener usuarios: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Verifica si existe al menos un usuario en el sistema (optimizada con cache)
        /// </summary>
        /// <returns>True si existe al menos un usuario</returns>
        public async Task<bool> ExistenUsuariosAsync()
        {
            try
            {
                // Usar cache si está disponible
                if (_conexionVerificada && DateTime.UtcNow - _ultimaVerificacion < _intervaloVerificacion)
                {
                    return true; // Si la conexión fue verificada recientemente, asumimos que hay usuarios
                }

                var count = await _context.Accesos.AsNoTracking().CountAsync();
                System.Diagnostics.Debug.WriteLine($"[AUTH] ?? Total de usuarios en sistema: {count}");
                
                _conexionVerificada = count > 0;
                _ultimaVerificacion = DateTime.UtcNow;
                
                return count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Error al verificar existencia de usuarios: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cambia la contraseña de un usuario existente (optimizada)
        /// </summary>
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
                // Primero validar credenciales actuales
                var esValido = await ValidarCredencialesAsync(usuario, contraseñaActual);
                if (!esValido)
                {
                    return false;
                }

                // Obtener el usuario para actualizar (con tracking para modificación)
                var acceso = await _context.Accesos
                    .FirstOrDefaultAsync(a => a.Usuario == usuario);

                if (acceso == null)
                {
                    return false;
                }

                // Actualizar contraseña
                acceso.Contraseña = HashearContraseña(contraseñaNueva);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Contraseña actualizada para '{usuario}'");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Error al cambiar contraseña: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Elimina un usuario del sistema (optimizada con validaciones)
        /// </summary>
        public async Task<bool> EliminarUsuarioAsync(string usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario))
            {
                return false;
            }

            try
            {
                // Verificar que no sea el último usuario (optimizado)
                var totalUsuarios = await _context.Accesos.AsNoTracking().CountAsync();
                if (totalUsuarios <= 1)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTH] ? No se puede eliminar el último usuario del sistema");
                    return false;
                }

                var acceso = await _context.Accesos
                    .FirstOrDefaultAsync(a => a.Usuario == usuario);

                if (acceso == null)
                {
                    return false;
                }

                _context.Accesos.Remove(acceso);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Usuario '{usuario}' eliminado exitosamente");

                // Invalidar cache
                _conexionVerificada = false;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] ? Error al eliminar usuario '{usuario}': {ex.Message}");
                return false;
            }
        }
    }
}
