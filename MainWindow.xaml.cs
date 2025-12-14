using System;
using System.Threading.Tasks;
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
                // Pequeño delay para que la UI se renderice primero
                await Task.Delay(500);
                
                bool conectado = await _authService.VerificarConexionAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Conexión a BD: {(conectado ? "? EXITOSA" : "? FALLIDA")}");
                
                if (!conectado)
                {
                    // Mostrar mensaje al usuario sobre problema de conexión
                    txtMessage.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);
                    txtMessage.Text = "?? No se puede conectar a la base de datos.\n\n" +
                                     "Posibles causas:\n" +
                                     "• SQL Server no está ejecutándose\n" +
                                     "• La base de datos no existe\n" +
                                     "• Configuración de conexión incorrecta\n\n" +
                                     "Haga clic en 'Configurar BD' para configurar la conexión.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Error al verificar conexión: {ex.Message}");
                
                // Mostrar mensaje de error al usuario
                txtMessage.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);
                txtMessage.Text = "?? Error al verificar conexión a la base de datos.\n\n" +
                                 "Haga clic en 'Configurar BD' para configurar la conexión.";
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
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 2. Verifique que la contraseña en BD comience con $2$12$");
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
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ???? ERROR DE SQL ????");
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
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ???? ERROR GENERAL ????");
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

        private async void BtnConfigDB_Click(object sender, RoutedEventArgs e)
        {
            await MostrarConfiguracionBD();
        }

        private async Task MostrarConfiguracionBD()
        {
            // Cargar configuración actual
            var config = await ConfigurationService.LoadConfigurationAsync();

            // Crear UI para configuración
            var stackPanel = new StackPanel { Spacing = 16 };

            // Servidor
            var txtServidor = new TextBox
            {
                Header = "Servidor",
                PlaceholderText = "localhost o .\\SQLEXPRESS",
                Text = config.Server
            };
            stackPanel.Children.Add(txtServidor);

            // Base de datos
            var txtBD = new TextBox
            {
                Header = "Base de Datos",
                PlaceholderText = "RamaFemenina",
                Text = config.Database
            };
            stackPanel.Children.Add(txtBD);

            // Tipo de autenticación
            var rbWindows = new RadioButton { Content = "Autenticación de Windows", IsChecked = config.UseIntegratedSecurity };
            var rbSQL = new RadioButton { Content = "Autenticación SQL Server", IsChecked = !config.UseIntegratedSecurity };
            
            var pnlAuth = new StackPanel { Spacing = 8 };
            pnlAuth.Children.Add(new TextBlock { Text = "Tipo de Autenticación", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            pnlAuth.Children.Add(rbWindows);
            pnlAuth.Children.Add(rbSQL);
            stackPanel.Children.Add(pnlAuth);

            // Credenciales SQL (solo si se selecciona)
            var txtUsuario = new TextBox
            {
                Header = "Usuario SQL",
                PlaceholderText = "sa",
                Text = config.UserId,
                Visibility = config.UseIntegratedSecurity ? Visibility.Collapsed : Visibility.Visible
            };

            var pwdPassword = new PasswordBox
            {
                Header = "Contraseña SQL",
                Password = config.Password,
                Visibility = config.UseIntegratedSecurity ? Visibility.Collapsed : Visibility.Visible
            };

            rbWindows.Checked += (s, e) => 
            {
                txtUsuario.Visibility = Visibility.Collapsed;
                pwdPassword.Visibility = Visibility.Collapsed;
            };

            rbSQL.Checked += (s, e) => 
            {
                txtUsuario.Visibility = Visibility.Visible;
                pwdPassword.Visibility = Visibility.Visible;
            };

            stackPanel.Children.Add(txtUsuario);
            stackPanel.Children.Add(pwdPassword);

            // Nota informativa
            var infoPanel = new Border
            {
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorAttentionBackgroundBrush"],
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 8, 0, 0)
            };

            var infoStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            infoStack.Children.Add(new FontIcon { Glyph = "\uE946", FontSize = 16 });
            infoStack.Children.Add(new TextBlock 
            { 
                Text = "La aplicación se cerrará después de guardar para aplicar los cambios.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12
            });
            infoPanel.Child = infoStack;
            stackPanel.Children.Add(infoPanel);

            // ScrollViewer para el contenido
            var scrollViewer = new ScrollViewer
            {
                Content = stackPanel,
                MaxHeight = 500
            };

            // Crear y mostrar el diálogo
            var dialog = new ContentDialog
            {
                Title = "Configuración de Base de Datos",
                Content = scrollViewer,
                PrimaryButtonText = "Probar Conexión",
                SecondaryButtonText = "Guardar y Cerrar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Probar conexión
                var testConfig = new ConfigurationService.DatabaseConfig
                {
                    Server = txtServidor.Text.Trim(),
                    Database = txtBD.Text.Trim(),
                    UseIntegratedSecurity = rbWindows.IsChecked == true,
                    UserId = rbWindows.IsChecked == true ? "" : txtUsuario.Text.Trim(),
                    Password = rbWindows.IsChecked == true ? "" : pwdPassword.Password,
                    TrustServerCertificate = true,
                    ConnectionTimeout = 30
                };

                // Validar campos
                if (string.IsNullOrWhiteSpace(testConfig.Server) || string.IsNullOrWhiteSpace(testConfig.Database))
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error de Validación",
                        Content = "El servidor y la base de datos son obligatorios.",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                    return;
                }

                if (!testConfig.UseIntegratedSecurity && (string.IsNullOrWhiteSpace(testConfig.UserId) || string.IsNullOrWhiteSpace(testConfig.Password)))
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error de Validación",
                        Content = "El usuario y contraseña SQL son obligatorios para autenticación SQL Server.",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                    return;
                }

                // Deshabilitar botones durante la prueba
                btnAccept.IsEnabled = false;
                txtMessage.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray);
                txtMessage.Text = "Probando conexión...";

                var (success, message) = await ConfigurationService.TestConnectionAsync(testConfig);

                btnAccept.IsEnabled = true;
                txtMessage.Text = "";

                var resultDialog = new ContentDialog
                {
                    Title = success ? "? Conexión Exitosa" : "? Error de Conexión",
                    Content = message,
                    CloseButtonText = "Aceptar",
                    XamlRoot = this.Content.XamlRoot
                };

                await resultDialog.ShowAsync();

                if (success)
                {
                    // Ofrecer guardar
                    var saveDialog = new ContentDialog
                    {
                        Title = "Guardar Configuración",
                        Content = "¿Desea guardar esta configuración?\n\nLa aplicación se cerrará para aplicar los cambios.",
                        PrimaryButtonText = "Guardar y Cerrar",
                        CloseButtonText = "No",
                        XamlRoot = this.Content.XamlRoot
                    };

                    if (await saveDialog.ShowAsync() == ContentDialogResult.Primary)
                    {
                        var saved = await ConfigurationService.SaveConfigurationAsync(testConfig);
                        
                        if (saved)
                        {
                            var finalDialog = new ContentDialog
                            {
                                Title = "? Configuración Guardada",
                                Content = "La configuración se guardó correctamente.\n\nLa aplicación se cerrará ahora.\n\nPor favor, vuelva a abrir la aplicación para aplicar los cambios.",
                                CloseButtonText = "Cerrar Aplicación",
                                XamlRoot = this.Content.XamlRoot
                            };
                            await finalDialog.ShowAsync();
                            
                            // Cerrar la aplicación
                            Application.Current.Exit();
                        }
                        else
                        {
                            var errorDialog = new ContentDialog
                            {
                                Title = "Error",
                                Content = "No se pudo guardar la configuración.",
                                CloseButtonText = "Aceptar",
                                XamlRoot = this.Content.XamlRoot
                            };
                            await errorDialog.ShowAsync();
                        }
                    }
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // Guardar directamente sin probar
                var saveConfig = new ConfigurationService.DatabaseConfig
                {
                    Server = txtServidor.Text.Trim(),
                    Database = txtBD.Text.Trim(),
                    UseIntegratedSecurity = rbWindows.IsChecked == true,
                    UserId = rbWindows.IsChecked == true ? "" : txtUsuario.Text.Trim(),
                    Password = rbWindows.IsChecked == true ? "" : pwdPassword.Password,
                    TrustServerCertificate = true,
                    ConnectionTimeout = 30
                };

                // Validar campos
                if (string.IsNullOrWhiteSpace(saveConfig.Server) || string.IsNullOrWhiteSpace(saveConfig.Database))
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error de Validación",
                        Content = "El servidor y la base de datos son obligatorios.",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                    return;
                }

                if (!saveConfig.UseIntegratedSecurity && (string.IsNullOrWhiteSpace(saveConfig.UserId) || string.IsNullOrWhiteSpace(saveConfig.Password)))
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error de Validación",
                        Content = "El usuario y contraseña SQL son obligatorios para autenticación SQL Server.",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                    return;
                }

                var confirmDialog = new ContentDialog
                {
                    Title = "Confirmar Guardado",
                    Content = "¿Desea guardar esta configuración sin probar la conexión?\n\nLa aplicación se cerrará para aplicar los cambios.",
                    PrimaryButtonText = "Guardar y Cerrar",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.Content.XamlRoot
                };

                if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    var saved = await ConfigurationService.SaveConfigurationAsync(saveConfig);

                    if (saved)
                    {
                        var finalDialog = new ContentDialog
                        {
                            Title = "? Configuración Guardada",
                            Content = "La configuración se guardó correctamente.\n\nLa aplicación se cerrará ahora.\n\nPor favor, vuelva a abrir la aplicación para aplicar los cambios.",
                            CloseButtonText = "Cerrar Aplicación",
                            XamlRoot = this.Content.XamlRoot
                        };
                        await finalDialog.ShowAsync();
                        
                        // Cerrar la aplicación
                        Application.Current.Exit();
                    }
                    else
                    {
                        var errorDialog = new ContentDialog
                        {
                            Title = "Error",
                            Content = "No se pudo guardar la configuración.",
                            CloseButtonText = "Aceptar",
                            XamlRoot = this.Content.XamlRoot
                        };
                        await errorDialog.ShowAsync();
                    }
                }
            }
        }
    }
}
