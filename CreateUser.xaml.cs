using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using RamaFemenina.Services;
using RamaFemenina.Data;
using Microsoft.EntityFrameworkCore;

namespace RamaFemenina
{
    public sealed partial class CreateUser : Window, INotifyPropertyChanged
    {
        private readonly AuthenticationService _authService;
        private readonly RamaFemeninaContext _context;

        public ObservableCollection<UsuarioViewModel> Usuarios { get; set; }
        public ObservableCollection<UsuarioViewModel> UsuariosFiltrados { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CreateUser()
        {
            InitializeComponent();

            var app = Application.Current as App;
            _authService = app!.Services.GetRequiredService<AuthenticationService>();
            _context = app.Services.GetRequiredService<RamaFemeninaContext>();

            Usuarios = new ObservableCollection<UsuarioViewModel>();
            UsuariosFiltrados = new ObservableCollection<UsuarioViewModel>();

            _ = CargarUsuariosAsync();
        }

        private async Task CargarUsuariosAsync()
        {
            try
            {
                Usuarios.Clear();
                var accesos = await _context.Accesos.ToListAsync();

                foreach (var acceso in accesos)
                {
                    bool esBCrypt = acceso.Contraseña.StartsWith("$2");
                    Usuarios.Add(new UsuarioViewModel
                    {
                        Usuario = acceso.Usuario,
                        EsBCrypt = esBCrypt,
                        FechaCreacion = "Usuario registrado",
                        EstadoSeguridad = esBCrypt ? "Seguro" : "Inseguro",
                        IconoSeguridad = esBCrypt ? "\uE72E" : "\uE730",
                        ColorSeguridad = esBCrypt ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Orange)
                    });
                }

                ActualizarListaFiltrada();
            }
            catch (Exception ex)
            {
                await MostrarMensajeAsync("Error", $"Error al cargar usuarios: {ex.Message}");
            }
        }

        private void ActualizarListaFiltrada(string searchText = "")
        {
            UsuariosFiltrados.Clear();
            var usuariosFiltrados = string.IsNullOrWhiteSpace(searchText) ? Usuarios : Usuarios.Where(u => u.Usuario.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            foreach (var usuario in usuariosFiltrados.OrderBy(u => u.Usuario))
            {
                UsuariosFiltrados.Add(usuario);
            }
            UsuariosListView.ItemsSource = UsuariosFiltrados;
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ActualizarListaFiltrada(sender.Text);
            }
        }

        private void ValidarFormulario(object sender, RoutedEventArgs e)
        {
            bool usuarioValido = !string.IsNullOrWhiteSpace(txtNuevoUsuario.Text) && txtNuevoUsuario.Text.Length >= 3;
            txtUsuarioError.Visibility = !usuarioValido && !string.IsNullOrEmpty(txtNuevoUsuario.Text) ? Visibility.Visible : Visibility.Collapsed;

            bool contraseñaValida = !string.IsNullOrWhiteSpace(pwdNuevaContraseña.Password) && pwdNuevaContraseña.Password.Length >= 6;
            txtContraseñaError.Visibility = !contraseñaValida && !string.IsNullOrEmpty(pwdNuevaContraseña.Password) ? Visibility.Visible : Visibility.Collapsed;

            bool confirmacionValida = pwdNuevaContraseña.Password == pwdConfirmarContraseña.Password;
            txtConfirmarError.Visibility = !confirmacionValida && !string.IsNullOrEmpty(pwdConfirmarContraseña.Password) ? Visibility.Visible : Visibility.Collapsed;

            btnCrearUsuario.IsEnabled = usuarioValido && contraseñaValida && confirmacionValida;
        }

        private async void BtnCrearUsuario_Click(object sender, RoutedEventArgs e)
        {
            var usuario = txtNuevoUsuario.Text.Trim();
            var contraseña = pwdNuevaContraseña.Password;

            if (Usuarios.Any(u => u.Usuario.Equals(usuario, StringComparison.OrdinalIgnoreCase)))
            {
                await MostrarMensajeAsync("Error", $"El usuario '{usuario}' ya existe");
                return;
            }

            btnCrearUsuario.IsEnabled = false;
            txtMensajeCreacion.Text = "? Creando usuario...";
            txtMensajeCreacion.Foreground = new SolidColorBrush(Colors.Gray);
            txtMensajeCreacion.Visibility = Visibility.Visible;

            try
            {
                bool creado = await _authService.CrearUsuarioAsync(usuario, contraseña);
                if (creado)
                {
                    txtMensajeCreacion.Text = "? Usuario creado exitosamente";
                    txtMensajeCreacion.Foreground = new SolidColorBrush(Colors.Green);
                    txtNuevoUsuario.Text = string.Empty;
                    pwdNuevaContraseña.Password = string.Empty;
                    pwdConfirmarContraseña.Password = string.Empty;
                    await CargarUsuariosAsync();
                    await Task.Delay(2000);
                    txtMensajeCreacion.Visibility = Visibility.Collapsed;
                }
                else
                {
                    txtMensajeCreacion.Text = "? Error al crear el usuario";
                    txtMensajeCreacion.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
            catch (Exception ex)
            {
                txtMensajeCreacion.Text = $"? Error: {ex.Message}";
                txtMensajeCreacion.Foreground = new SolidColorBrush(Colors.Red);
            }
            finally
            {
                btnCrearUsuario.IsEnabled = true;
            }
        }

        private async void BtnCambiarContraseña_Click(object sender, RoutedEventArgs e)
        {
            var usuarioSeleccionado = UsuariosListView.SelectedItem as UsuarioViewModel;
            if (usuarioSeleccionado == null) return;

            var contraseñaActualBox = new PasswordBox { Header = "Contraseña Actual", PlaceholderText = "Ingrese la contraseña actual", Margin = new Thickness(0, 0, 0, 12) };
            var nuevaContraseñaBox = new PasswordBox { Header = "Nueva Contraseña", PlaceholderText = "Ingrese la nueva contraseña", Margin = new Thickness(0, 0, 0, 12) };
            var confirmarNuevaBox = new PasswordBox { Header = "Confirmar Nueva Contraseña", PlaceholderText = "Confirme la nueva contraseña" };

            var panel = new StackPanel
            {
                Spacing = 8,
                Children = { new TextBlock { Text = $"Usuario: {usuarioSeleccionado.Usuario}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 16) }, contraseñaActualBox, nuevaContraseñaBox, confirmarNuevaBox }
            };

            var dialog = new ContentDialog { Title = "Cambiar Contraseña", Content = panel, PrimaryButtonText = "Cambiar", CloseButtonText = "Cancelar", DefaultButton = ContentDialogButton.Primary, XamlRoot = this.Content.XamlRoot };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (nuevaContraseñaBox.Password != confirmarNuevaBox.Password)
                {
                    await MostrarMensajeAsync("Error", "Las contraseñas nuevas no coinciden");
                    return;
                }

                bool cambiada = await _authService.CambiarContraseñaAsync(usuarioSeleccionado.Usuario, contraseñaActualBox.Password, nuevaContraseñaBox.Password);
                if (cambiada)
                {
                    await MostrarMensajeAsync("Éxito", "Contraseña cambiada correctamente");
                    await CargarUsuariosAsync();
                }
                else
                {
                    await MostrarMensajeAsync("Error", "La contraseña actual es incorrecta");
                }
            }
        }

        private async void BtnEliminarUsuario_Click(object sender, RoutedEventArgs e)
        {
            var usuarioSeleccionado = UsuariosListView.SelectedItem as UsuarioViewModel;
            if (usuarioSeleccionado == null) return;

            var confirmDialog = new ContentDialog { Title = "Confirmar Eliminación", Content = $"¿Está seguro que desea eliminar el usuario '{usuarioSeleccionado.Usuario}'?\n\nEsta acción no se puede deshacer.", PrimaryButtonText = "Eliminar", CloseButtonText = "Cancelar", DefaultButton = ContentDialogButton.Close, XamlRoot = this.Content.XamlRoot };
            var result = await confirmDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var acceso = await _context.Accesos.FirstOrDefaultAsync(a => a.Usuario == usuarioSeleccionado.Usuario);
                    if (acceso != null)
                    {
                        _context.Accesos.Remove(acceso);
                        await _context.SaveChangesAsync();
                        await MostrarMensajeAsync("Éxito", "Usuario eliminado correctamente");
                        await CargarUsuariosAsync();
                    }
                }
                catch (Exception ex)
                {
                    await MostrarMensajeAsync("Error", $"Error al eliminar usuario: {ex.Message}");
                }
            }
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            await CargarUsuariosAsync();
        }

        private void UsuariosListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool seleccionado = UsuariosListView.SelectedItem != null;
            btnCambiarContraseña.IsEnabled = seleccionado;
            btnEliminarUsuario.IsEnabled = seleccionado;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async Task MostrarMensajeAsync(string titulo, string mensaje)
        {
            var dialog = new ContentDialog { Title = titulo, Content = mensaje, CloseButtonText = "Ok", XamlRoot = this.Content.XamlRoot };
            await dialog.ShowAsync();
        }
    }

    public class UsuarioViewModel : INotifyPropertyChanged
    {
        private string _usuario;
        private bool _esBCrypt;
        private string _fechaCreacion;
        private string _estadoSeguridad;
        private string _iconoSeguridad;
        private SolidColorBrush _colorSeguridad;

        public string Usuario { get => _usuario; set { _usuario = value; OnPropertyChanged(); } }
        public bool EsBCrypt { get => _esBCrypt; set { _esBCrypt = value; OnPropertyChanged(); } }
        public string FechaCreacion { get => _fechaCreacion; set { _fechaCreacion = value; OnPropertyChanged(); } }
        public string EstadoSeguridad { get => _estadoSeguridad; set { _estadoSeguridad = value; OnPropertyChanged(); } }
        public string IconoSeguridad { get => _iconoSeguridad; set { _iconoSeguridad = value; OnPropertyChanged(); } }
        public SolidColorBrush ColorSeguridad { get => _colorSeguridad; set { _colorSeguridad = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
