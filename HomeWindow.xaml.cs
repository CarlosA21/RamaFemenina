using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RamaFemenina.Services;

namespace RamaFemenina;

/// <summary>
/// Ventana principal con menú de navegación lateral.
/// </summary>
public sealed partial class HomeWindow : Window
{
    private string? _currentUserName;

    public HomeWindow()
    {
        InitializeComponent();

        try
        {
            // Establecer el icono de la ventana (aparece en la barra de tareas)
            // Requiere un archivo .ico. Asegúrese de que "Assets/icono2.ico" exista en el proyecto.
            this.AppWindow.SetIcon("Assets/icono2.ico");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomeWindow] No se pudo establecer el icono: {ex.Message}");
        }

        // Seleccionar el primer item por defecto (dispara SelectionChanged y navega)
        if (NavView != null && NavView.MenuItems.Count > 0)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        // Registrar una sola vez el manejador de navegación para logging y futuros ajustes
        ContentFrame.Navigated += (s, e) =>
        {
            if (ContentFrame.Content is FrameworkElement page)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[HomeWindow] Navigated to page: {e.SourcePageType.Name}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[HomeWindow] Error in Navigated handler: {ex.Message}");
                }
            }
        };
    }

    public void SetUserName(string userName)
    {
        _currentUserName = userName;
        
        if (!string.IsNullOrEmpty(userName) && txtUserName != null)
        {
            txtUserName.Text = userName;
        }

        // Aplicar restricciones según el rol del usuario
        ConfigurarPermisosPorRol();
    }

    /// <summary>
    /// Configura los permisos basados en el rol del usuario actual
    /// </summary>
    private async void ConfigurarPermisosPorRol()
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserName))
            {
                System.Diagnostics.Debug.WriteLine($"[HomeWindow] No hay usuario, asumiendo Moderador por defecto");
                OcultarMenuCheques();
                return;
            }

            var app = Application.Current as App;
            var authService = app?.Services.GetService<AuthenticationService>();

            if (authService != null)
            {
                // Obtener el usuario de la base de datos
                using var scope = app!.Services.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<Data.RamaFemeninaContext>();
                
                var usuario = await context.Accesos
                    .FirstOrDefaultAsync(a => a.Usuario == _currentUserName);

                if (usuario != null)
                {
                    var rol = usuario.Rol ?? "Moderador";
                    System.Diagnostics.Debug.WriteLine($"[HomeWindow] Configurando permisos para usuario '{_currentUserName}' con rol: {rol}");

                    // Si es Moderador, ocultar el menú de Cheques
                    if (rol == "Moderador")
                    {
                        OcultarMenuCheques();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[HomeWindow] Usuario es Admin - se muestra menú completo");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[HomeWindow] Usuario no encontrado, asumiendo Moderador");
                    OcultarMenuCheques();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomeWindow] Error al configurar permisos: {ex.Message}");
            // Por seguridad, ocultar cheques si hay error
            OcultarMenuCheques();
        }
    }

    /// <summary>
    /// Oculta el menú de Cheques del NavigationView
    /// </summary>
    private void OcultarMenuCheques()
    {
        try
        {
            if (NavView == null || NavView.MenuItems == null)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeWindow] NavView o MenuItems es null");
                return;
            }

            // Buscar el item de Cheques por el Tag
            NavigationViewItem? itemCheques = null;
            foreach (var item in NavView.MenuItems)
            {
                if (item is NavigationViewItem nvi && nvi.Tag?.ToString() == "Cheques")
                {
                    itemCheques = nvi;
                    break;
                }
            }

            if (itemCheques != null)
            {
                NavView.MenuItems.Remove(itemCheques);
                System.Diagnostics.Debug.WriteLine($"[HomeWindow] ✅ Menú de Cheques ocultado para Moderador");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[HomeWindow] ⚠️ No se encontró el menú de Cheques");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomeWindow] Error al ocultar menú de Cheques: {ex.Message}");
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        // Ignorar selección del botón de Configuración
        if (args.IsSettingsSelected)
        {
            return;
        }

        // Obtener el Tag de forma robusta (SelectedItemContainer puede ser null)
        string? tag = null;
        if (args.SelectedItemContainer is NavigationViewItem nvi1)
        {
            tag = nvi1.Tag?.ToString();
        }
        else if (args.SelectedItem is NavigationViewItem nvi2)
        {
            tag = nvi2.Tag?.ToString();
        }

        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        // Evitar procesar acciones que no son navegación de páginas
        if (string.Equals(tag, "Logout", StringComparison.OrdinalIgnoreCase))
        {
            return; // El tap handler de Logout se encarga
        }

        Type? pageType = null;

        switch (tag)
        {
            case "Pacientes":
                pageType = typeof(PacientesPage);
                break;
            case "Clientes":
                pageType = typeof(ClientesPage);
                break;
            case "Donaciones":
                pageType = typeof(DonacionesPage);
                break;
            case "Cheques":
                pageType = typeof(ChequesPage);
                break;
            case "Recibos":
                pageType = typeof(ReciboPage);
                break;
            case "Reportes":
                pageType = typeof(ReportPage);
                break;
        }

        if (pageType != null)
        {
            // Solo navegar si no estamos ya en esa página
            if (ContentFrame.CurrentSourcePageType != pageType)
            {
                // Pasar parámetro "Reload" para forzar la carga de datos en OnNavigatedTo
                ContentFrame.Navigate(pageType, "Reload");
            }
            else
            {
                // Si estamos en la misma página, enviar un Reload explícito para refrescar datos
                ContentFrame.Navigate(pageType, "Reload");
            }
        }
    }

    private void ShowPlaceholder(string section, string icon)
    {
        // Crear una vista temporal mientras se crean las páginas específicas
        var stackPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 16
        };

        var iconText = new TextBlock
        {
            Text = icon,
            FontSize = 72,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var titleText = new TextBlock
        {
            Text = section,
            FontSize = 32,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
        };

        var descriptionText = new TextBlock
        {
            Text = "Esta sección está en desarrollo.",
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            Margin = new Thickness(0, 8, 0, 0)
        };

        stackPanel.Children.Add(iconText);
        stackPanel.Children.Add(titleText);
        stackPanel.Children.Add(descriptionText);

        var grid = new Grid
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"]
        };
        grid.Children.Add(stackPanel);

        ContentFrame.Content = grid;
    }

    private void LogoutButton_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Crear y mostrar la ventana de login
        var loginWindow = new MainWindow();
        loginWindow.Activate();

        // Cerrar la ventana actual
        this.Close();
    }
}
