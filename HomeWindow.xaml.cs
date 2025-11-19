using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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

            switch (tag)
            {
                case "Pacientes":
                    ShowPlaceholder("Pacientes", "👥");
                    break;
                case "Donaciones":
                    ShowPlaceholder("Donaciones", "💰");
                    break;
                case "Cheques":
                    ShowPlaceholder("Cheques", "📄");
                    break;
                case "Recibos":
                    ShowPlaceholder("Recibos", "🧾");
                    break;
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
