using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using RamaFemenina.Services;

namespace RamaFemenina
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly AuthenticationService _authService;

        public MainWindow()
        {
            InitializeComponent();

            // Obtener el servicio de autenticación desde el contenedor de DI
            var app = Application.Current as App;
            _authService = app!.Services.GetRequiredService<AuthenticationService>();
            
            // Verificar conexión al iniciar (para debug)
            VerificarConexionInicial();
        }

        private async void VerificarConexionInicial()
        {
            try
            {
                bool conectado = await _authService.VerificarConexionAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Conexión a BD: {(conectado ? "? EXITOSA" : "? FALLIDA")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Error al verificar conexión: {ex.Message}");
            }
        }

        private async void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            txtMessage.Text = string.Empty;

            var user = txtUsername.Text?.Trim();
            var pass = pwdPassword.Password ?? string.Empty;

            // Debug: Mostrar información de entrada (sin la contraseña completa)
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ===== INICIO DE LOGIN =====");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Usuario ingresado: '{user}'");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Longitud de contraseña: {pass.Length} caracteres");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Contraseña (primeros 3 chars): {(pass.Length > 0 ? pass.Substring(0, Math.Min(3, pass.Length)) + "..." : "vacía")}");

            if (string.IsNullOrEmpty(user))
            {
                txtMessage.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                txtMessage.Text = "Por favor ingrese el nombre de usuario.";
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Error: Usuario vacío");
                return;
            }

            if (string.IsNullOrEmpty(pass))
            {
                txtMessage.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                txtMessage.Text = "Por favor ingrese la contraseña.";
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Error: Contraseña vacía");
                return;
            }

            // Deshabilitar botones durante la validación
            btnAccept.IsEnabled = false;
            txtMessage.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray);
            txtMessage.Text = "Validando credenciales...";

            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Verificando conexión a BD...");
                
                // Verificar conexión primero
                bool conexionOk = await _authService.VerificarConexionAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Estado de conexión: {(conexionOk ? "? CONECTADO" : "? NO CONECTADO")}");

                if (!conexionOk)
                {
                    txtMessage.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                    txtMessage.Text = "? No se puede conectar a la base de datos.\nVerifique que SQL Server esté ejecutándose.";
                    btnAccept.IsEnabled = true;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ERROR: No hay conexión a la base de datos");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Validando credenciales en BD...");
                
                // Validar credenciales contra la base de datos
                bool isValid = await _authService.ValidarCredencialesAsync(user, pass);

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Resultado de validación: {(isValid ? "? VÁLIDO" : "? INVÁLIDO")}");

                if (isValid)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ? Login exitoso para usuario: {user}");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Navegando a HomeWindow...");
                    
                    // Login exitoso - navegar a la página principal
                    var app = Application.Current as App;
                    app?.NavigateToHome(user);
                }
                else
                {
                    txtMessage.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                    txtMessage.Text = $"? Usuario o contraseña incorrectos.\n\n" +
                                     $"?? Debug Info:\n" +
                                     $"• Usuario: '{user}'\n" +
                                     $"• Longitud contraseña: {pass.Length} chars\n" +
                                     $"• Verifique que el usuario exista en la tabla 'acceso'\n" +
                                     $"• Verifique que la contraseña esté hasheada con BCrypt";
                    btnAccept.IsEnabled = true;
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ? LOGIN FALLIDO");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Usuario buscado: '{user}'");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Sugerencias:");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 1. Ejecute: SELECT * FROM acceso WHERE usuario = '{user}'");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 2. Verifique que la contraseña en BD comience con $2a$12$");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 3. Si la contraseña está en texto plano, ejecute el script UPDATE");
                }
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                txtMessage.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                txtMessage.Text = $"? Error de SQL Server:\n{sqlEx.Message}\n\n" +
                                 $"?? Posibles causas:\n" +
                                 $"• SQL Server no está ejecutándose\n" +
                                 $"• Base de datos 'Ramafemenina' no existe\n" +
                                 $"• Tabla 'acceso' no existe\n" +
                                 $"• Permisos insuficientes";
                btnAccept.IsEnabled = true;
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ??? ERROR DE SQL ???");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Mensaje: {sqlEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Número de error: {sqlEx.Number}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Estado: {sqlEx.State}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Stack Trace: {sqlEx.StackTrace}");
            }
            catch (Exception ex)
            {
                txtMessage.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                txtMessage.Text = $"? Error inesperado:\n{ex.Message}\n\n" +
                                 $"Tipo: {ex.GetType().Name}";
                btnAccept.IsEnabled = true;
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ??? ERROR GENERAL ???");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Tipo: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Mensaje: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Stack Trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Inner Exception: {ex.InnerException.Message}");
                }
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ===== FIN DE LOGIN =====");
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnGestionUsuarios_Click(object sender, RoutedEventArgs e)
        {
            // Abrir ventana de gestión de usuarios
            var usuariosWindow = new CreateUser();
            usuariosWindow.Activate();
        }
    }
}
