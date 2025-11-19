using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace RamaFemenina
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            txtMessage.Text = string.Empty;

            var user = txtUsername.Text?.Trim();
            var pass = pwdPassword.Password ?? string.Empty;

            if (string.IsNullOrEmpty(user))
            {
                txtMessage.Text = "Por favor ingrese el nombre de usuario.";
                return;
            }

            if (string.IsNullOrEmpty(pass))
            {
                txtMessage.Text = "Por favor ingrese la contraseña.";
                return;
            }

            // TODO: Replace this with real authentication logic
            if (user == "admin" && pass == "password")
            {
                // Login exitoso - navegar a la página principal
                var app = Application.Current as App;
                app?.NavigateToHome(user);
            }
            else
            {
                txtMessage.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                txtMessage.Text = "Usuario o contraseña incorrectos.";
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
