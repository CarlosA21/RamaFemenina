using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RamaFemenina.Data;
using RamaFemenina.Models;
using RamaFemenina.Services;
using RamaFemenina.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RamaFemenina
{
    public class ClienteViewModel
    {
        public int IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Rnc { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }

        // Propiedades formateadas para la UI
        public string RncDisplay => string.IsNullOrEmpty(Rnc) ? "Sin RNC" : Rnc;
        public string TelefonoDisplay => string.IsNullOrEmpty(Telefono) ? "Sin teléfono" : Telefono;
        public string DireccionDisplay => string.IsNullOrEmpty(Direccion) ? "Sin dirección" : Direccion;
    }

    public sealed partial class ClientesPage : Page, INotifyPropertyChanged
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DataCacheService _cacheService;
        private bool _isClienteSelected;
        private bool _isLoading;
        private Timer _searchDelayTimer;
        private bool _isPageActive = true;
        
        // Propiedades de paginación
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalCount = 0;
        private string _currentSearchTerm = "";
        
        // Cliente para edición
        private ClienteViewModel _clienteEditando;

        public bool IsClienteSelected
        {
            get => _isClienteSelected;
            set
            {
                if (_isClienteSelected != value)
                {
                    _isClienteSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(HasPreviousPage));
                    OnPropertyChanged(nameof(HasNextPage));
                    OnPropertyChanged(nameof(PageInfo));
                }
            }
        }

        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(PageInfo));
                }
            }
        }

        public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / _pageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public string PageInfo => $"Página {CurrentPage} de {TotalPages} ({TotalCount} registros)";

        public ObservableCollection<ClienteViewModel> ClientesCollection { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    
                    if (this.FindName("LoadingIndicator") is ProgressRing loadingIndicator)
                        loadingIndicator.IsActive = value;
                    if (this.FindName("LoadingOverlay") is Grid overlay)
                        overlay.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        public ClientesPage()
        {
            InitializeComponent();

            // Habilitar caché de navegación
            NavigationCacheMode = NavigationCacheMode.Enabled;

            var app = Application.Current as App;
            _serviceProvider = app!.Services;
            _cacheService = app.Services.GetRequiredService<DataCacheService>();

            ClientesCollection = new ObservableCollection<ClienteViewModel>();

            // Inicialización de timer para búsqueda con delay
            _searchDelayTimer = new Timer(PerformSearch, null, Timeout.Infinite, Timeout.Infinite);

        // La carga inicial se maneja en OnNavigatedTo

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
                catch { /* Ignorar errores de animación */ }
            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _isPageActive = true;
            
            System.Diagnostics.Debug.WriteLine($"[CLIENTES-PAGE] OnNavigatedTo - Iniciando carga de datos");
            
            // SIEMPRE recargar desde caché (es rápido y garantiza datos frescos)
            System.Diagnostics.Debug.WriteLine($"[CLIENTES-PAGE] Cargando clientes desde caché...");
            await LoadPageAsync(CurrentPage > 0 ? CurrentPage : 1);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            _isPageActive = false;
        }

        private async Task LoadPageAsync(int page, bool updateStats = true)
        {
            if (!_isPageActive) return;
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CLIENTES] LoadPageAsync iniciado - Page: {page}");
                IsLoading = true;
                
                // Ejecutar consultas en paralelo para reducir tiempo total
                var clientesTask = _cacheService.GetClientesPaginatedAsync(page, _pageSize, _currentSearchTerm);
                var totalCountTask = _cacheService.GetClientesTotalCountAsync(_currentSearchTerm);
                await Task.WhenAll(clientesTask, totalCountTask);
                var clientes = clientesTask.Result;
                var totalCount = totalCountTask.Result;

                System.Diagnostics.Debug.WriteLine($"[CLIENTES] Datos obtenidos - Clientes: {clientes?.Count() ?? 0}, Total: {totalCount}");

                if (!_isPageActive) return;

                ClientesCollection.Clear();
                foreach (var cliente in clientes)
                {
                    // Limpiar datos NULL problemáticos
                    if (cliente.nombre == null) cliente.nombre = "Sin especificar";
                    if (cliente.rnc == null) cliente.rnc = string.Empty;
                    if (cliente.telefono == null) cliente.telefono = string.Empty;
                    if (cliente.direccion == null) cliente.direccion = string.Empty;

                    ClientesCollection.Add(new ClienteViewModel
                    {
                        IdCliente = cliente.idCliente,
                        Nombre = cliente.nombre,
                        Rnc = cliente.rnc,
                        Telefono = cliente.telefono,
                        Direccion = cliente.direccion
                    });
                }

                CurrentPage = page;
                TotalCount = totalCount;

                System.Diagnostics.Debug.WriteLine($"[CLIENTES] Colección actualizada - Count: {ClientesCollection.Count}");

                // Actualizar controles de UI
                if (lvClientes != null)
                    lvClientes.ItemsSource = ClientesCollection;

                var hayClientes = ClientesCollection.Count > 0;
                if (this.FindName("ListViewScroller") is UIElement listScroller)
                    listScroller.Visibility = hayClientes ? Visibility.Visible : Visibility.Collapsed;
                if (emptyState != null)
                    emptyState.Visibility = hayClientes ? Visibility.Collapsed : Visibility.Visible;

                UpdatePaginationControls();

                if (updateStats && _isPageActive)
                {
                    _ = ActualizarEstadisticasAsync();
                }

                System.Diagnostics.Debug.WriteLine($"[CLIENTES] LoadPageAsync completado exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CLIENTES] Error al cargar clientes: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CLIENTES] StackTrace: {ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdatePaginationControls()
        {
            // Actualizar botones de paginación
            if (this.FindName("btnPreviousPage") is Button prevBtn)
                prevBtn.IsEnabled = HasPreviousPage;

            if (this.FindName("btnNextPage") is Button nextBtn)
                nextBtn.IsEnabled = HasNextPage;

            if (this.FindName("btnFirstPage") is Button firstBtn)
                firstBtn.IsEnabled = HasPreviousPage;

            if (this.FindName("btnLastPage") is Button lastBtn)
                lastBtn.IsEnabled = HasNextPage;

            // Actualizar información de página
            if (this.FindName("txtPageInfo") is TextBlock pageInfoText)
                pageInfoText.Text = PageInfo;
        }

        private async Task ActualizarEstadisticasAsync()
        {
            try
            {
                if (!_isPageActive) return;

                // Usar una instancia separada del contexto para estadísticas
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();

                var totalClientes = await context.Clientes.CountAsync();

                if (!_isPageActive) return;

                var conRnc = await context.Clientes
                    .CountAsync(c => !string.IsNullOrEmpty(c.rnc));

                var datosCompletos = await context.Clientes
                    .CountAsync(c => !string.IsNullOrEmpty(c.nombre) && 
                                    !string.IsNullOrEmpty(c.telefono) && 
                                    !string.IsNullOrEmpty(c.direccion));

                // Clientes nuevos hoy (simulado para ejemplo)
                var nuevosHoy = 0;

                // Actualizar en UI Thread
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    try
                    {
                        if (!_isPageActive) return;

                        if (this.FindName("txtTotalClientes") is TextBlock totalText)
                            totalText.Text = totalClientes.ToString();

                        if (this.FindName("txtContador") is Microsoft.UI.Xaml.Documents.Run contadorRun)
                            contadorRun.Text = TotalCount.ToString();

                        if (this.FindName("txtConRnc") is TextBlock rncText)
                            rncText.Text = conRnc.ToString();

                        if (this.FindName("txtContactosCompletos") is TextBlock completosText)
                            completosText.Text = datosCompletos.ToString();

                        if (this.FindName("txtNuevos") is TextBlock nuevosText)
                            nuevosText.Text = nuevosHoy.ToString();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating stats UI: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating statistics: {ex.Message}");
            }
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Cancelar timer anterior
            _searchDelayTimer?.Change(Timeout.Infinite, Timeout.Infinite);

            // Configurar nuevo timer con delay de 500ms
            _currentSearchTerm = ((TextBox)sender).Text?.Trim() ?? "";
            _searchDelayTimer?.Change(500, Timeout.Infinite);
        }

        private void PerformSearch(object state)
        {
            if (!_isPageActive) return;

            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, async () =>
            {
                // Invalidar cache para nueva búsqueda
                _cacheService.InvalidateCache("clientes");
                await LoadPageAsync(1);
            });
        }

        // Eventos de paginación
        private async void BtnFirstPage_Click(object sender, RoutedEventArgs e)
        {
            if (HasPreviousPage)
                await LoadPageAsync(1);
        }

        private async void BtnPreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (HasPreviousPage)
                await LoadPageAsync(CurrentPage - 1);
        }

        private async void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (HasNextPage)
                await LoadPageAsync(CurrentPage + 1);
        }

        private async void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            if (HasNextPage)
                await LoadPageAsync(TotalPages);
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            // Limpiar cache y recargar
            _cacheService.InvalidateCache("clientes");
            await LoadPageAsync(CurrentPage);
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            _clienteEditando = null;
            LimpiarFormulario();
            txtModalTitle.Text = "Nuevo Cliente";
            modalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                int clienteId;
                if (button.Tag is int id)
                {
                    clienteId = id;
                }
                else if (button.Tag is string strId && int.TryParse(strId, out clienteId))
                {
                    // Tag es string, convertir a int
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Tag del botón no es válido: {button.Tag}");
                    return;
                }

                var cliente = ClientesCollection.FirstOrDefault(c => c.IdCliente == clienteId);
                if (cliente != null)
                {
                    _clienteEditando = cliente;
                    txtModalTitle.Text = "Editar Cliente";
                    txtNombre.Text = cliente.Nombre;
                    txtRnc.Text = cliente.Rnc;
                    txtTelefono.Text = cliente.Telefono;
                    txtDireccion.Text = cliente.Direccion;
                    modalOverlay.Visibility = Visibility.Visible;
                }
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag != null)
                {
                    int clienteId;
                    if (button.Tag is int id)
                    {
                        clienteId = id;
                    }
                    else if (button.Tag is string strId && int.TryParse(strId, out clienteId))
                    {
                        // Tag es string, convertir a int
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Tag del botón no es válido: {button.Tag}");
                        await ShowInfoDialog("Error", "Error interno: ID de cliente no válido");
                        return;
                    }

                    var cliente = ClientesCollection.FirstOrDefault(c => c.IdCliente == clienteId);
                    if (cliente == null)
                    {
                        await ShowInfoDialog("Error", "No se encontró el cliente seleccionado");
                        return;
                    }

                    // VERIFICAR SI TIENE FACTURAS ASOCIADAS
                    using var scope = _serviceProvider.CreateScope();
                    using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                    
                    // Verificar si existe la tabla factura y si hay registros
                    int totalFacturas = 0;
                    try
                    {
                        totalFacturas = await context.Database.SqlQueryRaw<int>(
                            "SELECT COUNT(*) FROM factura WHERE idcliente = {0}", clienteId)
                            .FirstOrDefaultAsync();
                    }
                    catch
                    {
                        // Si la tabla no existe, continuar sin validación
                        System.Diagnostics.Debug.WriteLine("Tabla factura no existe, continuando...");
                    }

                    ContentDialog confirmDialog;

                    if (totalFacturas > 0)
                    {
                        // Cliente tiene facturas asociadas
                        var messagePanel = new StackPanel { Spacing = 12 };
                        
                        messagePanel.Children.Add(new TextBlock
                        {
                            Text = "?? ADVERTENCIA: Este cliente tiene registros asociados",
                            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed),
                            TextWrapping = TextWrapping.Wrap
                        });

                        messagePanel.Children.Add(new TextBlock
                        {
                            Text = $"\nCliente: {cliente.Nombre}\nRNC: {cliente.Rnc}",
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                            TextWrapping = TextWrapping.Wrap
                        });

                        messagePanel.Children.Add(new TextBlock
                        {
                            Text = $"\n?? Facturas asociadas: {totalFacturas}",
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
                            TextWrapping = TextWrapping.Wrap
                        });

                        messagePanel.Children.Add(new TextBlock
                        {
                            Text = "\n¿Qué desea hacer?",
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                            Margin = new Thickness(0, 8, 0, 0),
                            TextWrapping = TextWrapping.Wrap
                        });

                        messagePanel.Children.Add(new TextBlock
                        {
                            Text = "• ELIMINAR TODO: Eliminará el cliente Y todas sus facturas\n• CANCELAR: No se eliminará nada",
                            FontStyle = Windows.UI.Text.FontStyle.Italic,
                            FontSize = 12,
                            Margin = new Thickness(0, 4, 0, 0),
                            TextWrapping = TextWrapping.Wrap
                        });

                        confirmDialog = new ContentDialog
                        {
                            Title = "?? Cliente con Facturas Asociadas",
                            Content = messagePanel,
                            PrimaryButtonText = "??? Eliminar TODO (Cliente + Facturas)",
                            CloseButtonText = "Cancelar",
                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = this.XamlRoot
                        };
                    }
                    else
                    {
                        // Cliente sin facturas
                        confirmDialog = new ContentDialog
                        {
                            Title = "Confirmar Eliminación",
                            Content = $"¿Está seguro que desea eliminar al cliente?\n\n" +
                                      $"ID: {cliente.IdCliente}\n" +
                                      $"Nombre: {cliente.Nombre}\n" +
                                      $"RNC: {cliente.Rnc}\n\n" +
                                      $"Esta acción no se puede deshacer.",
                            PrimaryButtonText = "Eliminar",
                            CloseButtonText = "Cancelar",
                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = this.XamlRoot
                        };
                    }

                    var result = await confirmDialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        try
                        {
                            using var scope2 = _serviceProvider.CreateScope();
                            using var context2 = scope2.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                            
                            // Si tiene facturas, eliminarlas primero
                            if (totalFacturas > 0)
                            {
                                try
                                {
                                    await context2.Database.ExecuteSqlRawAsync(
                                        "DELETE FROM factura WHERE idcliente = {0}", clienteId);
                                    
                                    System.Diagnostics.Debug.WriteLine($"Eliminadas {totalFacturas} facturas del cliente {clienteId}");
                                }
                                catch (Exception facturaEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error al eliminar facturas: {facturaEx.Message}");
                                    throw new Exception($"Error al eliminar facturas asociadas: {facturaEx.Message}");
                                }
                            }

                            // Ahora eliminar el cliente
                            var clienteDb = await context2.Clientes.FindAsync(clienteId);
                            if (clienteDb != null)
                            {
                                context2.Clientes.Remove(clienteDb);
                                await context2.SaveChangesAsync();

                                _cacheService.InvalidateCache("clientes");
                                await LoadPageAsync(CurrentPage);
                                
                                await DispatcherQueue.EnqueueAsync(async () =>
                                {
                                    var mensaje = totalFacturas > 0 
                                        ? $"Cliente eliminado correctamente.\n\n? Cliente eliminado\n? {totalFacturas} factura(s) eliminada(s)"
                                        : "Cliente eliminado correctamente";
                                    
                                    await ShowInfoDialog("Éxito", mensaje);
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error al eliminar: {ex.Message}");
                            await DispatcherQueue.EnqueueAsync(async () =>
                            {
                                await ShowInfoDialog("Error", 
                                    $"Error al eliminar cliente:\n\n{ex.Message}\n\n" +
                                    "Si el problema persiste, contacte al administrador del sistema.");
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BTN-ELIMINAR] ERROR: {ex.Message}");
                await ShowInfoDialog("Error", $"Error inesperado: {ex.Message}");
            }
        }

        private async Task ShowInfoDialog(string title, string message)
        {
            if (!DispatcherQueue.HasThreadAccess)
            {
                await DispatcherQueue.EnqueueAsync(async () =>
                {
                    await ShowInfoDialogInternal(title, message);
                });
                return;
            }

            await ShowInfoDialogInternal(title, message);
        }

        private async Task ShowInfoDialogInternal(string title, string message)
        {
            try
            {
                if (this.XamlRoot == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DIALOG] ERROR: XamlRoot es null");
                    return;
                }

                var contentStack = new StackPanel
                {
                    Spacing = 12,
                    MaxWidth = 450
                };

                string iconGlyph = "\uE946";
                Windows.UI.Color iconColor;

                if (title.Contains("Error"))
                {
                    iconGlyph = "\uE783";
                    iconColor = Windows.UI.Color.FromArgb(255, 196, 43, 28);
                }
                else if (title.Contains("Éxito"))
                {
                    iconGlyph = "\uE73E";
                    iconColor = Windows.UI.Color.FromArgb(255, 16, 124, 16);
                }
                else if (title.Contains("Información") || title.Contains("Advertencia"))
                {
                    iconGlyph = "\uE946";
                    iconColor = Windows.UI.Color.FromArgb(255, 255, 185, 0);
                }
                else
                {
                    iconColor = Windows.UI.Color.FromArgb(255, 0, 120, 212);
                }

                var iconBorder = new Border
                {
                    Width = 56,
                    Height = 56,
                    CornerRadius = new CornerRadius(28),
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Windows.UI.Color.FromArgb(20, iconColor.R, iconColor.G, iconColor.B)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 12)
                };

                var icon = new FontIcon
                {
                    Glyph = iconGlyph,
                    FontSize = 28,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(iconColor),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                iconBorder.Child = icon;

                var messageText = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 14
                };

                contentStack.Children.Add(iconBorder);
                contentStack.Children.Add(messageText);

                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = contentStack,
                    CloseButtonText = "Aceptar",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DIALOG] ERROR en ShowInfoDialogInternal: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DIALOG] Stack trace: {ex.StackTrace}");
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario()) return;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();

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

                    context.Clientes.Add(nuevoCliente);
                    await context.SaveChangesAsync();
                    await MostrarDialogoAsync("Éxito", "Cliente creado correctamente");
                }
                else
                {
                    // Editar cliente con marcado explícito de propiedades modificadas
                    var clienteDb = await context.Clientes.FindAsync(_clienteEditando.IdCliente);
                    if (clienteDb != null)
                    {
                        clienteDb.nombre = txtNombre.Text.Trim();
                        clienteDb.rnc = txtRnc.Text.Trim();
                        clienteDb.telefono = txtTelefono.Text.Trim();
                        clienteDb.direccion = txtDireccion.Text.Trim();

                        var entry = context.Entry(clienteDb);
                        entry.Property(e => e.nombre).IsModified = true;
                        entry.Property(e => e.rnc).IsModified = true;
                        entry.Property(e => e.telefono).IsModified = true;
                        entry.Property(e => e.direccion).IsModified = true;

                        await context.SaveChangesAsync();
                        await MostrarDialogoAsync("Éxito", "Cliente actualizado correctamente");
                    }
                }

                // Limpiar cache y recargar
                _cacheService.InvalidateCache("clientes");
                await LoadPageAsync(CurrentPage);
                BtnCerrarModal_Click(sender, e);
            }
            catch (Exception ex)
            {
                await MostrarDialogoAsync("Error", $"Error al guardar cliente: {ex.Message}");
            }
        }

        private void BtnCerrarModal_Click(object sender, RoutedEventArgs e)
        {
            modalOverlay.Visibility = Visibility.Collapsed;
            LimpiarFormulario();
        }

        private void LvClientes_ItemClick(object sender, ItemClickEventArgs e)
        {
            IsClienteSelected = e.ClickedItem != null;
        }

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                _ = MostrarDialogoAsync("Error", "El nombre es obligatorio");
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

        public void Dispose()
        {
            try
            {
                _searchDelayTimer?.Dispose();
            }
            catch
            {
                // Ignorar errores durante la limpieza
            }
        }
    }
}
