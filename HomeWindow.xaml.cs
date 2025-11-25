using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace RamaFemenina;

/// <summary>
/// Ventana principal con menú de navegación lateral.
/// </summary>
public sealed partial class HomeWindow : Window
{
    public HomeWindow()
    {
        InitializeComponent();

        // Seleccionar el primer item por defecto
        if (NavView != null && NavView.MenuItems.Count > 0)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
            // Navegar a la primera página
            ContentFrame.Navigate(typeof(PacientesPage));
        }
    }

    public void SetUserName(string userName)
    {
        if (!string.IsNullOrEmpty(userName) && txtUserName != null)
        {
            txtUserName.Text = userName;
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer != null)
        {
            var tag = args.SelectedItemContainer.Tag?.ToString();
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
                case "CajaChica":
                    pageType = typeof(CajaChicaPage);
                    break;
                case "Reportes":
                    pageType = typeof(ReportPage);
                    break;
                case "Facturacion":
                    pageType = typeof(BlankPage1); //facturacionPage
                    break;
            }

            if (pageType != null)
            {
                // Solo navegar si no estamos ya en esa página
                if (ContentFrame.CurrentSourcePageType != pageType)
                {
                    ContentFrame.Navigate(pageType);
                }
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
