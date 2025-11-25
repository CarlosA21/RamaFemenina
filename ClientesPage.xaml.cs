using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RamaFemenina.Data;
using RamaFemenina.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace RamaFemenina
{
    public sealed partial class ClientesPage : Page
    {
        private readonly RamaFemeninaContext _context;
        private ObservableCollection<ClienteViewModel> _clientes;
        private ClienteViewModel? _clienteEditando;
        private bool _datosYaCargados = false;

        public ClientesPage()
        {
            this.InitializeComponent();
            
            // Habilitar caché de navegación
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            
            var app = (App)Application.Current;
            _context = app.Services.GetRequiredService<RamaFemeninaContext>();
            _clientes = new ObservableCollection<ClienteViewModel>();
            
            Loaded += ClientesPage_Loaded;
            
            // Iniciar animación de entrada
            this.Loaded += (s, e) => 
            {
                try 
                { 
                    if (this.FindName("FadeInStoryboard") is Storyboard storyboard)
                    {
                        storyboard.Begin();
                    }
                } 
                catch { }
            };
        }

        private async void ClientesPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Solo cargar datos si no se han cargado antes
            if (!_datosYaCargados)
            {
                await CargarClientesAsync();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Solo recargar si se fuerza explícitamente
            if (e.Parameter?.ToString() == "Reload")
            {
                _ = CargarClientesAsync();
            }
        }

        private async Task CargarClientesAsync()
        {
            try
            {
                txtEstado.Text = "Cargando clientes...";
                
                var clientes = await _context.Clientes
                    .OrderBy(c => c.nombre)
                    .ToListAsync();

                _clientes.Clear();
                
                foreach (var cliente in clientes)
                {
                    _clientes.Add(new ClienteViewModel
                    {
                        IdCliente = cliente.idCliente,
                        Nombre = cliente.nombre,
                        Rnc = cliente.rnc,
                        Telefono = cliente.telefono,
                        Direccion = cliente.direccion
                    });
                }

                lvClientes.ItemsSource = _clientes;
                ActualizarEstadisticas();
                
                // Controlar visibilidad de ListView y Empty State
                var hayClientes = _clientes.Count > 0;
                
                if (this.FindName("ListViewScroller") is UIElement listScroller)
                    listScroller.Visibility = hayClientes ? Visibility.Visible : Visibility.Collapsed;
                
                emptyState.Visibility = hayClientes ? Visibility.Collapsed : Visibility.Visible;
                
                txtEstado.Text = hayClientes
                    ? $"Mostrando {_clientes.Count} cliente(s)" 
                    : "No hay clientes registrados";
                
                // Marcar que los datos ya fueron cargados
                _datosYaCargados = true;
            }
            catch (Exception ex)
            {
                txtEstado.Text = $"Error al cargar clientes: {ex.Message}";
                await MostrarDialogoAsync("Error", $"No se pudieron cargar los clientes: {ex.Message}");
            }
        }

        private void ActualizarEstadisticas()
        {
            try
            {
                // Total de clientes
                if (this.FindName("txtTotalClientes") is TextBlock totalText)
                    totalText.Text = _clientes.Count.ToString();
                
                // Clientes con RNC
                var conRnc = _clientes.Count(c => !string.IsNullOrEmpty(c.Rnc));
                if (this.FindName("txtConRnc") is TextBlock rncText)
                    rncText.Text = conRnc.ToString();
                
                // Contactos completos (con todos los datos)
                var completos = _clientes.Count(c => 
                    !string.IsNullOrEmpty(c.Rnc) && 
                    !string.IsNullOrEmpty(c.Telefono) && 
                    !string.IsNullOrEmpty(c.Direccion));
                if (this.FindName("txtContactosCompletos") is TextBlock completosText)
                    completosText.Text = completos.ToString();
                
                // Nuevos (placeholder - en producción sería basado en fecha de creación)
                if (this.FindName("txtNuevos") is TextBlock nuevosText)
                    nuevosText.Text = "0";
            }
            catch
            {
                // Ignorar errores de estadísticas
            }
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            _clienteEditando = null;
            LimpiarFormulario();
            txtModalTitle.Text = "Nuevo Cliente";
            modalOverlay.Visibility = Visibility.Visible;
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            await CargarClientesAsync();
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int idCliente)
            {
                var cliente = _clientes.FirstOrDefault(c => c.IdCliente == idCliente);
                if (cliente != null)
                {
                    _clienteEditando = cliente;
                    txtModalTitle.Text = "Editar Cliente";
                    txtNombre.Text = cliente.Nombre;
                    txtRnc.Text = cliente.Rnc ?? "";
                    txtTelefono.Text = cliente.Telefono ?? "";
                    txtDireccion.Text = cliente.Direccion ?? "";
                    modalOverlay.Visibility = Visibility.Visible;
                }
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int idCliente)
            {
                var cliente = _clientes.FirstOrDefault(c => c.IdCliente == idCliente);
                if (cliente != null)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Confirmar eliminación",
                        Content = $"¿Está seguro que desea eliminar al cliente '{cliente.Nombre}'?\n\nEsta acción no se puede deshacer.",
                        PrimaryButtonText = "Eliminar",
                        CloseButtonText = "Cancelar",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.XamlRoot
                    };

                    var resultado = await dialog.ShowAsync();
                    
                    if (resultado == ContentDialogResult.Primary)
                    {
                        await EliminarClienteAsync(idCliente);
                    }
                }
            }
        }

        private async Task EliminarClienteAsync(int idCliente)
        {
            try
            {
                txtEstado.Text = "Eliminando cliente...";
                
                var cliente = await _context.Clientes.FindAsync(idCliente);
                if (cliente != null)
                {
                    _context.Clientes.Remove(cliente);
                    await _context.SaveChangesAsync();
                    
                    await CargarClientesAsync();
                    await MostrarDialogoAsync("Éxito", "Cliente eliminado correctamente.");
                }
            }
            catch (Exception ex)
            {
                txtEstado.Text = $"Error al eliminar: {ex.Message}";
                await MostrarDialogoAsync("Error", $"No se pudo eliminar el cliente: {ex.Message}");
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario())
                return;

            try
            {
                txtEstado.Text = "Guardando...";
                btnGuardar.IsEnabled = false;

                if (_clienteEditando == null)
                {
                    // Nuevo cliente
                    var nuevoCliente = new Clientes
                    {
                        nombre = txtNombre.Text.Trim(),
                        rnc = txtRnc.Text.Trim(),
                        telefono = txtTelefono.Text.Trim(),
                        direccion = txtDireccion.Text.Trim()
                    };

                    _context.Clientes.Add(nuevoCliente);
                    await _context.SaveChangesAsync();
                    
                    await MostrarDialogoAsync("Éxito", "Cliente agregado correctamente.");
                }
                else
                {
                    // Editar cliente existente
                    var cliente = await _context.Clientes.FindAsync(_clienteEditando.IdCliente);
                    if (cliente != null)
                    {
                        cliente.nombre = txtNombre.Text.Trim();
                        cliente.rnc = txtRnc.Text.Trim();
                        cliente.telefono = txtTelefono.Text.Trim();
                        cliente.direccion = txtDireccion.Text.Trim();
                        
                        await _context.SaveChangesAsync();
                        
                        await MostrarDialogoAsync("Éxito", "Cliente actualizado correctamente.");
                    }
                }

                modalOverlay.Visibility = Visibility.Collapsed;
                await CargarClientesAsync();
            }
            catch (Exception ex)
            {
                txtEstado.Text = $"Error: {ex.Message}";
                await MostrarDialogoAsync("Error", $"No se pudo guardar el cliente: {ex.Message}");
            }
            finally
            {
                btnGuardar.IsEnabled = true;
            }
        }

        private void BtnCerrarModal_Click(object sender, RoutedEventArgs e)
        {
            modalOverlay.Visibility = Visibility.Collapsed;
            LimpiarFormulario();
        }

        private void LvClientes_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Implementar si se desea alguna acción al hacer clic en un item
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textoBusqueda = txtBuscar.Text.ToLower().Trim();
            
            if (string.IsNullOrEmpty(textoBusqueda))
            {
                // Mostrar todos los clientes
                lvClientes.ItemsSource = _clientes;
                
                var hayClientes = _clientes.Count > 0;
                if (this.FindName("ListViewScroller") is UIElement listScroller)
                    listScroller.Visibility = hayClientes ? Visibility.Visible : Visibility.Collapsed;
                emptyState.Visibility = hayClientes ? Visibility.Collapsed : Visibility.Visible;
                
                txtEstado.Text = hayClientes
                    ? $"Mostrando {_clientes.Count} cliente(s)"
                    : "No hay clientes registrados";
            }
            else
            {
                // Filtrar clientes
                var clientesFiltrados = _clientes.Where(c =>
                    c.Nombre.ToLower().Contains(textoBusqueda) ||
                    (c.Rnc != null && c.Rnc.ToLower().Contains(textoBusqueda)) ||
                    (c.Telefono != null && c.Telefono.ToLower().Contains(textoBusqueda))
                ).ToList();
                
                lvClientes.ItemsSource = clientesFiltrados;
                
                var hayResultados = clientesFiltrados.Count > 0;
                if (this.FindName("ListViewScroller") is UIElement listScroller)
                    listScroller.Visibility = hayResultados ? Visibility.Visible : Visibility.Collapsed;
                emptyState.Visibility = Visibility.Collapsed; // No mostrar empty state durante búsqueda
                
                txtEstado.Text = $"Mostrando {clientesFiltrados.Count} de {_clientes.Count} clientes";
            }
        }

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MostrarDialogoAsync("Validación", "El nombre o razón social es obligatorio.").GetAwaiter();
                txtNombre.Focus(FocusState.Programmatic);
                return false;
            }

            if (txtNombre.Text.Trim().Length < 3)
            {
                MostrarDialogoAsync("Validación", "El nombre debe tener al menos 3 caracteres.").GetAwaiter();
                txtNombre.Focus(FocusState.Programmatic);
                return false;
            }

            return true;
        }

        private void LimpiarFormulario()
        {
            txtNombre.Text = "";
            txtRnc.Text = "";
            txtTelefono.Text = "";
            txtDireccion.Text = "";
            _clienteEditando = null;
        }

        private async Task MostrarDialogoAsync(string titulo, string mensaje)
        {
            var dialog = new ContentDialog
            {
                Title = titulo,
                Content = mensaje,
                CloseButtonText = "Aceptar",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    // ViewModel para la lista de clientes
    public class ClienteViewModel
    {
        public int IdCliente { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Rnc { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }

        // Propiedades para display
        public string RncDisplay => string.IsNullOrEmpty(Rnc) ? "N/A" : Rnc;
        public string TelefonoDisplay => string.IsNullOrEmpty(Telefono) ? "N/A" : Telefono;
        public string DireccionDisplay => string.IsNullOrEmpty(Direccion) ? "Sin dirección" : Direccion;
    }
}
