using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RamaFemenina.Models;
using RamaFemenina.Data;
using RamaFemenina.Services;
using RamaFemenina.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml.Media;

namespace RamaFemenina;

public sealed partial class ReciboPage : Page, INotifyPropertyChanged
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DataCacheService _cacheService;
    private readonly NcfSequenceService _ncfSequenceService;
    private bool _isReciboSelected;
    private bool _isLoading;
    private Timer _searchDelayTimer;
    private bool _isPageActive = true;
    
    // Propiedades de paginación
    private int _currentPage = 1;
    private int _pageSize = 50;
    private int _totalCount = 0;
    private string _currentSearchTerm = "";
    private string _currentTipoFiltro = "Todos";
    
    // Toggle para tipo de documento
    public enum TipoDocumento
    {
        Recibos,
        Facturas
    }
    
    private TipoDocumento _tipoDocumentoActual = TipoDocumento.Recibos;

    // Clase auxiliar para datos de factura NCF
    public class DatosFacturaNcf
    {
        public string NCF { get; set; } = string.Empty;
        public DateTime ValidaHasta { get; set; }
        public string RncCliente { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string TelefonoCliente { get; set; } = string.Empty;
        public string DireccionCliente { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string MetodoPago { get; set; } = string.Empty;
        public string? NumeroCheque { get; set; }
        public string? Banco { get; set; }
        // Nuevo: indica si la factura será gravada (de lo contrario, exenta)
        public bool EsGravada { get; set; } = false;
    }

    // Clase extendida de Recibo para facturas NCF con datos adicionales de la BD
    public class ReciboFacturaNcf : Recibo
    {
        public decimal Exento { get; set; }
        public decimal Gravado { get; set; }
        public decimal Itbis { get; set; }
        public string NCFCompleto { get; set; } = string.Empty;
        public int? TCFNumerico { get; set; }
        public DateTime ValidaHasta { get; set; }
        public string DireccionCliente { get; set; } = string.Empty;
        public string TelefonoCliente { get; set; } = string.Empty;
    }
    
    // Configuración de posiciones de impresión (en milímetros)
    private float numeroReciboX = 150;
    private float numeroReciboY = 20;
    private float fechaX = 150;
    private float fechaY = 35;
    private float recibimosDex = 15;
    private float recibimosDeY = 50;
    private float montoX = 150;
    private float montoY = 65;
    private float montoLetrasX = 15;
    private float montoLetrasY = 80;
    private float conceptoX = 15;
    private float conceptoY = 95;
    private float tipoPagoX = 15;
    private float tipoPagoY = 110;
    
    public bool IsReciboSelected
    {
        get => _isReciboSelected;
        set
        {
            if (_isReciboSelected != value)
            {
                _isReciboSelected = value;
                OnPropertyChanged();
            }
        }
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
                
                // Actualizar visibilidad del indicador de carga
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (this.FindName("LoadingOverlay") is Grid loadingOverlay)
                        loadingOverlay.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                        
                    if (this.FindName("LoadingIndicator") is ProgressRing loadingIndicator)
                        loadingIndicator.IsActive = value;
                });
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

    public ObservableCollection<Recibo> RecibosCollection { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ReciboPage()
    {
        System.Diagnostics.Debug.WriteLine("[ReciboPage] ==================== INICIO CONSTRUCTOR ====================");
        
        // Inicializar servicios antes de cargar XAML para evitar accesos nulos
        var app = Application.Current as App;
        if (app?.Services == null)
        {
            System.Diagnostics.Debug.WriteLine("[ReciboPage] Error: ServiceProvider no inicializado");
            throw new InvalidOperationException("ServiceProvider no inicializado");
        }

        _serviceProvider = app.Services;
        _cacheService = app.Services.GetRequiredService<DataCacheService>();
        _ncfSequenceService = new NcfSequenceService();
        
        // Inicializar la colección
        RecibosCollection = new ObservableCollection<Recibo>();
        
        InitializeComponent();
        
        // Habilitar caché de navegación
        NavigationCacheMode = NavigationCacheMode.Enabled;
        
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
        
        System.Diagnostics.Debug.WriteLine("[ReciboPage] ==================== FIN CONSTRUCTOR ====================");
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _isPageActive = true;
        
        try
        {
            System.Diagnostics.Debug.WriteLine($"[RECIBOS-PAGE] OnNavigatedTo - Iniciando carga de datos");
            
            // SIEMPRE recargar desde caché (es rápido y garantiza datos frescos)
            System.Diagnostics.Debug.WriteLine($"[RECIBOS-PAGE] Cargando recibos desde caché...");
            await LoadPageAsync(CurrentPage > 0 ? CurrentPage : 1);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RECIBOS-PAGE] Error en OnNavigatedTo: {ex.Message}");
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        
        // Marcar página como inactiva
        _isPageActive = false;
    }

    private async Task LoadPageAsync(int page, bool updateStats = true)
    {
        if (!_isPageActive) return;
        
        try
        {
            // Verificar que la tabla existe con manejo de errores de red
            bool tableExists = false;
            int retries = 0;
            const int maxRetries = 3;
            
            while (retries < maxRetries && !tableExists)
            {
                try
                {
                    tableExists = await VerifyTableExistsAsync();
                    break;
                }
                catch (Exception ex) when (ex.Message.Contains("transient") || ex.Message.Contains("timeout") || ex.Message.Contains("connection"))
                {
                    retries++;
                    if (retries < maxRetries)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ReciboPage] Reintentando verificación de tabla ({retries}/{maxRetries})...");
                        await Task.Delay(1000 * retries); // Espera incremental
                    }
                    else
                    {
                        throw; // Falló después de todos los reintentos
                    }
                }
            }
            
            if (!tableExists)
            {
                await DispatcherQueue.EnqueueAsync(async () =>
                {
                    if (EmptyState != null)
                        EmptyState.Visibility = Visibility.Visible;
                    if (this.FindName("ListViewScroller") is UIElement listScrollerEmpty)
                        listScrollerEmpty.Visibility = Visibility.Collapsed;
                    
                    await ShowInfoDialog("Error de Conexión", 
                        "No se puede conectar al servidor de base de datos.\n\n" +
                        "Verifique:\n" +
                        "• Que el servidor SQL esté activo\n" +
                        "• Que la dirección IP sea correcta en appsettings.json\n" +
                        "• Que no haya firewall bloqueando la conexión\n\n" +
                        "Error técnico: Timeout de conexión TCP al servidor.");
                });
                return;
            }

            var recibos = await _cacheService.GetRecibosPaginatedAsync(page, _pageSize, _currentSearchTerm);
            var totalCount = await _cacheService.GetRecibosTotalCountAsync(_currentSearchTerm);

            if (!_isPageActive) return;
            
            await DispatcherQueue.EnqueueAsync(() =>
            {
                RecibosCollection.Clear();
                foreach (var recibo in recibos)
                {
                    CleanReciboData(recibo);
                    RecibosCollection.Add(recibo);
                }

                CurrentPage = page;
                TotalCount = totalCount;

                // Actualizar controles de UI
                if (RecibosListView != null)
                    RecibosListView.ItemsSource = RecibosCollection;
                
                var hayRecibos = RecibosCollection.Count > 0;
                if (this.FindName("ListViewScroller") is UIElement listScrollerMain)
                    listScrollerMain.Visibility = hayRecibos ? Visibility.Visible : Visibility.Collapsed;
                if (EmptyState != null)
                    EmptyState.Visibility = hayRecibos ? Visibility.Collapsed : Visibility.Visible;
                
                UpdatePaginationControls();
            });
            
            if (updateStats && _isPageActive)
            {
                _ = ActualizarEstadisticasAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar recibos: {ex.Message}");
            
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                await ShowInfoDialog("Error al Cargar Datos",
                    $"No se pudieron cargar los recibos.\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    $"Por favor verifique su conexión al servidor de base de datos.");
            });
        }
    }

    private async Task<bool> VerifyTableExistsAsync()
    {
        try
        {
            if (_serviceProvider == null)
            {
                System.Diagnostics.Debug.WriteLine("[ReciboPage] Error: ServiceProvider null al verificar tabla");
                return false;
            }

            System.Diagnostics.Debug.WriteLine("[ReciboPage] Verificando existencia de tabla inrecibo...");
            
            // Usar una instancia separada del contexto
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
            
            // Intentar directamente consultar la tabla - método más confiable
            var count = await context.Recibos.CountAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[ReciboPage] ? Tabla inrecibo existe. Total registros: {count}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReciboPage] ? Error verificando tabla inrecibo: {ex.Message}");
            return false;
        }
    }

    private static void CleanReciboData(Recibo recibo)
    {
        recibo.TipoRecibo ??= "Ingreso";
        recibo.RecibimosDe ??= "Sin especificar";
        recibo.MontoEnLetras ??= string.Empty;
        recibo.Concepto ??= string.Empty;
        recibo.Cedula ??= string.Empty;
        recibo.NumeroFacturaNCF ??= string.Empty;
        recibo.NumeroCheque ??= string.Empty;
        recibo.Banco ??= string.Empty;
    }

    private void UpdatePaginationControls()
    {
        // Actualizar botones de paginación
        if (this.FindName("btnPreviousPage") is Button prevBtn)
            prevBtn.IsEnabled = HasPreviousPage && !IsLoading;
            
        if (this.FindName("btnNextPage") is Button nextBtn)
            nextBtn.IsEnabled = HasNextPage && !IsLoading;
            
        if (this.FindName("btnFirstPage") is Button firstBtn)
            firstBtn.IsEnabled = HasPreviousPage && !IsLoading;
            
        if (this.FindName("btnLastPage") is Button lastBtn)
            lastBtn.IsEnabled = HasNextPage && !IsLoading;

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
            
            var totalRecibos = await context.Recibos.CountAsync().ConfigureAwait(false);
            var totalIngresos = await context.Recibos
                .Where(r => r.TipoRecibo == "Ingreso")
                .SumAsync(r => r.Monto).ConfigureAwait(false);
            var totalEgresos = await context.Recibos
                .Where(r => r.TipoRecibo == "Egreso")
                .SumAsync(r => r.Monto).ConfigureAwait(false);

            var balance = totalIngresos - totalEgresos;

            // Actualizar UI
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                try
                {
                    if (!_isPageActive) return;
                    
                    if (this.FindName("txtTotalRecibos") is TextBlock totalText)
                        totalText.Text = totalRecibos.ToString();
                        
                    if (this.FindName("txtContador") is Microsoft.UI.Xaml.Documents.Run contadorRun)
                        contadorRun.Text = TotalCount.ToString();
                    
                    if (this.FindName("txtTotalIngresos") is TextBlock ingresosText)
                        ingresosText.Text = $"RD$ {totalIngresos:N2}";
                    
                    if (this.FindName("txtTotalEgresos") is TextBlock egresosText)
                        egresosText.Text = $"RD$ {totalEgresos:N2}";
                    
                    if (this.FindName("txtBalance") is TextBlock balanceText)
                    {
                        balanceText.Text = $"RD$ {balance:N2}";
                        balanceText.Foreground = balance >= 0 
                            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green)
                            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                    }
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

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            // Cancelar timer anterior
            _searchDelayTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Configurar nuevo timer con delay de 500ms
            _currentSearchTerm = sender.Text?.Trim() ?? "";
            _searchDelayTimer?.Change(500, Timeout.Infinite);
        }
    }

    private void PerformSearch(object state)
    {
        if (!_isPageActive) return;
        
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, async () =>
        {
            CurrentPage = 1; // Resetear a página 1 en búsqueda
            
            if (_tipoDocumentoActual == TipoDocumento.Recibos)
            {
                _cacheService.InvalidateCache("recibos");
                await LoadPageAsync(1);
            }
            else
            {
                await CargarFacturas();
            }
        });
    }

    private async void TipoDocumento_Click(object sender, RoutedEventArgs e)
    {
        // Alternar entre Recibos y Facturas
        _tipoDocumentoActual = _tipoDocumentoActual == TipoDocumento.Recibos 
            ? TipoDocumento.Facturas 
            : TipoDocumento.Recibos;
        
        await ActualizarVistaDocumento();
    }

    private async Task ActualizarVistaDocumento()
    {
        // Actualizar UI del botón toggle
        if (_tipoDocumentoActual == TipoDocumento.Recibos)
        {
            TipoDocumentoText.Text = "?? Recibos";
            TipoDocumentoIcon.Glyph = "\uE8A1";
            SearchBox.PlaceholderText = "Buscar recibos...";
            
            // Ocultar botón de configurar secuencia NCF
            if (this.FindName("btnConfigurarSecuenciaNcf") is Button btnConfigNcf)
                btnConfigNcf.Visibility = Visibility.Collapsed;
            
            // Actualizar filtros para recibos
            TipoReciboCombo.Items.Clear();
            TipoReciboCombo.Items.Add(new ComboBoxItem { Content = "?? Todos" });
            TipoReciboCombo.Items.Add(new ComboBoxItem { Content = "?? Ingreso" });
            TipoReciboCombo.Items.Add(new ComboBoxItem { Content = "?? Egreso" });
            TipoReciboCombo.Header = "Filtro";
            TipoReciboCombo.SelectedIndex = 0;
            
            // Actualizar texto del botón imprimir
            if (this.FindName("btnImprimir") is Button btnImprimir && 
                btnImprimir.Content is StackPanel stackPanel &&
                stackPanel.Children.Count > 1 && 
                stackPanel.Children[1] is TextBlock textBlock)
            {
                textBlock.Text = "Imprimir";
            }
            
            await CargarRecibos();
        }
        else
        {
            TipoDocumentoText.Text = "?? Facturas NCF";
            TipoDocumentoIcon.Glyph = "\uE8C7";
            SearchBox.PlaceholderText = "Buscar facturas NCF...";
            
            // Mostrar botón de configurar secuencia NCF
            if (this.FindName("btnConfigurarSecuenciaNcf") is Button btnConfigNcf)
                btnConfigNcf.Visibility = Visibility.Visible;
            
            // Actualizar filtros para facturas
            TipoReciboCombo.Items.Clear();
            TipoReciboCombo.Items.Add(new ComboBoxItem { Content = "?? Todas" });
            TipoReciboCombo.Items.Add(new ComboBoxItem { Content = "? Pagadas" });
            TipoReciboCombo.Items.Add(new ComboBoxItem { Content = "? Pendientes" });
            TipoReciboCombo.Items.Add(new ComboBoxItem { Content = "? Anuladas" });
            TipoReciboCombo.Header = "Estado";
            TipoReciboCombo.SelectedIndex = 0;
            
            // Actualizar texto del botón imprimir
            if (this.FindName("btnImprimir") is Button btnImprimir && 
                btnImprimir.Content is StackPanel stackPanel &&
                stackPanel.Children.Count > 1 && 
                stackPanel.Children[1] is TextBlock textBlock)
            {
                textBlock.Text = "NCF PDF";
            }
            
            await CargarFacturas();
        }
    }

    private async Task CargarRecibos()
    {
        // Restaurar texto del contador para recibos
        DispatcherQueue.TryEnqueue(() =>
        {
            if (this.FindName("txtContadorTipo") is Microsoft.UI.Xaml.Documents.Run tipoRun)
                tipoRun.Text = "recibos";
        });
        
        // Cargar recibos (lógica existente)
        await LoadPageAsync(1);
    }

    private async Task CargarFacturas()
    {
        IsLoading = true;
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
            
            // Construir query base
            var query = context.Facturas
                .Include(f => f.Cliente)
                .Where(f => f.NCFNumerico.HasValue) // Solo facturas con NCF
                .AsQueryable();

            // Aplicar filtro de búsqueda si existe
            if (!string.IsNullOrWhiteSpace(_currentSearchTerm))
            {
                query = query.Where(f => 
                    f.NoFactura.ToString().Contains(_currentSearchTerm) ||
                    f.Cliente!.nombre.Contains(_currentSearchTerm) ||
                    f.NCFNumerico.ToString().Contains(_currentSearchTerm));
            }

            // Aplicar filtro de estado si no es "Todas"
            if (_currentTipoFiltro != "Todas")
            {
                query = _currentTipoFiltro switch
                {
                    "Pagadas" => query.Where(f => (f.NulaTexto != "SI") && f.Pago >= f.APagar),
                    "Pendientes" => query.Where(f => (f.NulaTexto != "SI") && (f.Pago == null || f.Pago < f.APagar)),
                    "Anuladas" => query.Where(f => f.NulaTexto == "SI" || f.NulaTexto == "S"),
                    _ => query
                };
            }

            // Obtener total de registros para paginación
            var totalFacturas = await query.CountAsync();

            // Cargar facturas paginadas
            var facturas = await query
                .OrderByDescending(f => f.Fecha)
                .Skip((CurrentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToListAsync();
            
            // Convertir facturas a formato de recibo para reutilizar la UI
            var recibosVirtuales = facturas.Select(f =>
            {
                // Log de valores para debug
                System.Diagnostics.Debug.WriteLine($"[CargarFacturas] Factura {f.IdFactura}: Exento={f.Exento}, Gravado={f.Gravado}, Itbis={f.Itbis}, APagar={f.APagar}");
                
                // Construir NCF completo
                var tipoComprobante = f.TCFNumerico == 14 ? "B14" : 
                                     f.TCFNumerico == 15 ? "B15" : "B01";
                var ncfCompleto = f.NCFNumerico.HasValue ? $"{tipoComprobante}{f.NCFNumerico:D8}" : "Sin NCF";
                
                return new ReciboFacturaNcf
                {
                    IdRecibo = f.IdFactura,
                    NumeroRecibo = f.NoFactura,
                    TipoRecibo = "Factura NCF",
                    Fecha = f.Fecha,
                    RecibimosDe = f.Cliente?.nombre ?? "Sin cliente",
                    Monto = f.APagar,
                    Concepto = "DONATIVO PARA PACIENTES ONCOLOGICOS DE ESCASOS RECURSOS.",
                    EsEfectivo = f.EsEfectivo,
                    EsCheque = f.EsCheque,
                    EsTransferencia = f.EsCredito,
                    NumeroCheque = f.NumeroCheque,
                    Banco = f.Banco,
                    Cedula = f.Cliente?.rnc ?? "",
                    // ? NUEVOS CAMPOS: Guardar valores de la BD
                    Exento = f.Exento,
                    Gravado = f.Gravado,
                    Itbis = f.Itbis,
                    NCFCompleto = ncfCompleto,
                    TCFNumerico = f.TCFNumerico,
                    ValidaHasta = f.FechaVencimiento ?? DateTime.Now.AddMonths(1),
                    DireccionCliente = f.Cliente?.direccion ?? "",
                    TelefonoCliente = f.Cliente?.telefono ?? ""
                };
            }).Cast<Recibo>().ToList();
            
            // Actualizar colección
            DispatcherQueue.TryEnqueue(() =>
            {
                RecibosCollection.Clear();
                foreach (var recibo in recibosVirtuales)
                {
                    RecibosCollection.Add(recibo);
                }
                
                TotalCount = totalFacturas; // Usar el total real de registros
                // CurrentPage ya está configurado
                
                // Actualizar contador
                if (this.FindName("txtContador") is Microsoft.UI.Xaml.Documents.Run contadorRun)
                    contadorRun.Text = TotalCount.ToString();
                if (this.FindName("txtContadorTipo") is Microsoft.UI.Xaml.Documents.Run tipoRun)
                    tipoRun.Text = "facturas NCF";
                
                // Actualizar visibilidad
                var hayFacturas = RecibosCollection.Count > 0;
                if (this.FindName("ListViewScroller") is UIElement listScroller)
                    listScroller.Visibility = hayFacturas ? Visibility.Visible : Visibility.Collapsed;
                if (EmptyState != null)
                {
                    EmptyState.Visibility = hayFacturas ? Visibility.Collapsed : Visibility.Visible;
                    // Actualizar texto del estado vacío para facturas
                    if (EmptyState.Children.Count > 2 && EmptyState.Children[2] is StackPanel textPanel &&
                        textPanel.Children.Count > 0 && textPanel.Children[0] is TextBlock titleText)
                    {
                        titleText.Text = "No hay facturas NCF registradas";
                    }
                }
                
                UpdatePaginationControls();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando facturas: {ex.Message}");
            await ShowInfoDialog("Error", $"Error al cargar facturas: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void TipoRecibo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedIndex >= 0)
        {
            if (_tipoDocumentoActual == TipoDocumento.Recibos)
            {
                _currentTipoFiltro = combo.SelectedIndex switch
                {
                    0 => "Todos",
                    1 => "Ingreso", 
                    2 => "Egreso",
                    _ => "Todos"
                };
            }
            else
            {
                // Filtros para facturas (implementar lógica específica si es necesario)
                _currentTipoFiltro = combo.SelectedIndex switch
                {
                    0 => "Todas",
                    1 => "Pagadas",
                    2 => "Pendientes",
                    3 => "Anuladas",
                    _ => "Todas"
                };
            }
            
            _ = Task.Run(async () => 
            {
                CurrentPage = 1; // Resetear a página 1 cuando cambia el filtro
                if (_tipoDocumentoActual == TipoDocumento.Recibos)
                    await LoadPageAsync(1).ConfigureAwait(false);
                else
                    await CargarFacturas().ConfigureAwait(false);
            });
        }
    }

    // Eventos de paginación
    private async void BtnFirstPage_Click(object sender, RoutedEventArgs e)
    {
        if (HasPreviousPage && !IsLoading)
        {
            CurrentPage = 1;
            await CargarDatosPorTipo();
        }
    }

    private async void BtnPreviousPage_Click(object sender, RoutedEventArgs e)
    {
        if (HasPreviousPage && !IsLoading)
        {
            CurrentPage--;
            await CargarDatosPorTipo();
        }
    }

    private async void BtnNextPage_Click(object sender, RoutedEventArgs e)
    {
        if (HasNextPage && !IsLoading)
        {
            CurrentPage++;
            await CargarDatosPorTipo();
        }
    }

    private async void BtnLastPage_Click(object sender, RoutedEventArgs e)
    {
        if (HasNextPage && !IsLoading)
        {
            CurrentPage = TotalPages;
            await CargarDatosPorTipo();
        }
    }

    // Método auxiliar para cargar datos según el tipo actual
    private async Task CargarDatosPorTipo()
    {
        if (_tipoDocumentoActual == TipoDocumento.Recibos)
            await LoadPageAsync(CurrentPage);
        else
            await CargarFacturas();
    }

    private void RecibosListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsReciboSelected = (sender as ListView)?.SelectedItem != null;
        
        // Actualizar estado de botones directamente
        var haySeleccion = IsReciboSelected;
        
        if (this.FindName("btnEditar") is Button editBtn)
            editBtn.IsEnabled = haySeleccion;
            
        if (this.FindName("btnEliminar") is Button delBtn)
            delBtn.IsEnabled = haySeleccion;
            
        if (this.FindName("btnGenerarFacturaNcf") is Button facturaBtn)
            facturaBtn.IsEnabled = haySeleccion;
            
        if (this.FindName("btnImprimir") is Button printBtn)
            printBtn.IsEnabled = haySeleccion;
    }

    private async void BtnNuevoRecibo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Si estamos en modo Facturas NCF, crear una nueva factura NCF desde cero
            if (_tipoDocumentoActual == TipoDocumento.Facturas)
            {
                var baseRecibo = new Recibo
                {
                    TipoRecibo = "Factura NCF",
                    Fecha = DateTime.Now,
                    RecibimosDe = string.Empty,
                    Cedula = string.Empty,
                    Monto = 0m,
                    Concepto = "DONATIVO PARA PACIENTES ONCOLOGICOS DE ESCASOS RECURSOS."
                };

                await MostrarDialogoFacturaNcf(baseRecibo);
                return;
            }

            var resultado = await MostrarDialogoRecibo(null);
            if (resultado != null)
            {
                // Normalizar flags de método de pago (garantizar un 0/1)
                if (!(resultado.EsEfectivo == true || resultado.EsTransferencia == true || resultado.EsCheque == true))
                {
                    resultado.EsEfectivo = true; // Por defecto efectivo
                }
                else
                {
                    // Asegurar exclusividad
                    resultado.EsEfectivo = resultado.EsEfectivo;
                    resultado.EsTransferencia = (resultado.EsTransferencia == true) && !(resultado.EsEfectivo == true) && !(resultado.EsCheque == true);
                    resultado.EsCheque = (resultado.EsCheque == true) && !(resultado.EsEfectivo == true) && !(resultado.EsTransferencia == true);
                }
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                    
                    // Calcular el siguiente IdRecibo y NumeroRecibo (MAX + 1)
                    var maxIdRecibo = await context.Recibos.AnyAsync()
                        ? await context.Recibos.MaxAsync(r => r.IdRecibo)
                        : 0;
                    var maxNumeroRecibo = await context.Recibos.AnyAsync()
                        ? await context.Recibos.MaxAsync(r => (int?)r.NumeroRecibo) ?? 0
                        : 0;
                    
                    resultado.IdRecibo = maxIdRecibo + 1;
                    resultado.NumeroRecibo = maxNumeroRecibo + 1;
                    
                    System.Diagnostics.Debug.WriteLine($"[BTN-NUEVO] Asignando IdRecibo={resultado.IdRecibo}, NumeroRecibo={resultado.NumeroRecibo}");
                    
                    context.Recibos.Add(resultado);
                    await context.SaveChangesAsync().ConfigureAwait(false);

                    _cacheService.InvalidateCache("recibos");
                    await LoadPageAsync(CurrentPage);
                    
                    await DispatcherQueue.EnqueueAsync(async () =>
                    {
                        await ShowInfoDialog("Éxito", $"Recibo creado correctamente.\nNo. Recibo: {resultado.NumeroRecibo}\nMonto: ${resultado.Monto:N2}");
                    });
                }
                catch (Exception ex)
                {
                    await DispatcherQueue.EnqueueAsync(async () =>
                    {
                        await ShowInfoDialog("Error", $"Error al guardar recibo: {ex.Message}");
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BTN-NUEVO] ERROR: {ex.Message}");
            await ShowInfoDialog("Error", $"Error inesperado: {ex.Message}");
        }
    }

    private async void BtnEditarRecibo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var reciboSeleccionado = RecibosListView?.SelectedItem as Recibo;
            if (reciboSeleccionado == null)
            {
                await ShowInfoDialog("Error", "Debe seleccionar un recibo");
                return;
            }
            // En modo Facturas NCF, reutilizar el diálogo de Factura NCF con los datos del elemento seleccionado
            if (_tipoDocumentoActual == TipoDocumento.Facturas)
            {
                await MostrarDialogoFacturaNcf(reciboSeleccionado);
                return;
            }

            var resultado = await MostrarDialogoRecibo(reciboSeleccionado);
            if (resultado != null)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                    
                    var recibo = await context.Recibos.FindAsync(reciboSeleccionado.IdRecibo).ConfigureAwait(false);
                    if (recibo != null)
                    {
                        // SOLO actualizar propiedades que existen en la base de datos
                        // TipoRecibo, MontoEnLetras y NumeroCheque son [NotMapped] - NO se deben actualizar
                        recibo.Fecha = resultado.Fecha;
                        recibo.RecibimosDe = resultado.RecibimosDe;
                        recibo.Cedula = resultado.Cedula; // Incluye el número de cheque si aplica
                        recibo.Monto = resultado.Monto;
                        recibo.Concepto = resultado.Concepto;
                        recibo.EsEfectivo = resultado.EsEfectivo;
                        recibo.EsTransferencia = resultado.EsTransferencia;
                        recibo.EsCheque = resultado.EsCheque;
                        recibo.NumeroFacturaNCF = resultado.NumeroFacturaNCF;
                        recibo.Banco = resultado.Banco;

                        // Marcar SOLO las propiedades que existen en BD como modificadas
                        var entry = context.Entry(recibo);
                        entry.Property(e => e.Fecha).IsModified = true;
                        entry.Property(e => e.RecibimosDe).IsModified = true;
                        entry.Property(e => e.Cedula).IsModified = true;
                        entry.Property(e => e.Monto).IsModified = true;
                        entry.Property(e => e.Concepto).IsModified = true;
                        entry.Property(e => e.EsEfectivo).IsModified = true;
                        entry.Property(e => e.EsTransferencia).IsModified = true;
                        entry.Property(e => e.EsCheque).IsModified = true;
                        entry.Property(e => e.NumeroFacturaNCF).IsModified = true;
                        entry.Property(e => e.Banco).IsModified = true;

                        await context.SaveChangesAsync().ConfigureAwait(false);
                        
                        _cacheService.InvalidateCache("recibos");
                        
                        // Recargar la página en el UI thread
                        await DispatcherQueue.EnqueueAsync(async () =>
                        {
                            await LoadPageAsync(CurrentPage);
                            
                            // Pequeño delay para asegurar que la carga se completó
                            await Task.Delay(100);
                            
                            await ShowInfoDialog("Éxito", "Recibo actualizado correctamente");
                        });
                    }
                }
                catch (Exception ex)
                {
                    await DispatcherQueue.EnqueueAsync(async () =>
                    {
                        await ShowInfoDialog("Error", $"Error al actualizar recibo: {ex.Message}");
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BTN-EDITAR] ERROR: {ex.Message}");
            await ShowInfoDialog("Error", $"Error inesperado: {ex.Message}");
        }
    }

    private async void BtnEliminarRecibo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var reciboSeleccionado = RecibosListView?.SelectedItem as Recibo;
            if (reciboSeleccionado == null)
            {
                await ShowInfoDialog("Error", "Debe seleccionar un recibo");
                return;
            }

            var messagePanel = new StackPanel { Spacing = 12 };
            
            messagePanel.Children.Add(new TextBlock
            {
                Text = "¿Está seguro que desea eliminar este recibo?",
                TextWrapping = TextWrapping.Wrap
            });

            messagePanel.Children.Add(new TextBlock
            {
                Text = $"No. Recibo: {reciboSeleccionado.NumeroRecibo}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            messagePanel.Children.Add(new TextBlock
            {
                Text = $"De: {reciboSeleccionado.RecibimosDe}",
                TextWrapping = TextWrapping.Wrap
            });

            messagePanel.Children.Add(new TextBlock
            {
                Text = $"Monto: ${reciboSeleccionado.Monto:N2}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            messagePanel.Children.Add(new TextBlock
            {
                Text = "\nEsta acción no se puede deshacer.",
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
                TextWrapping = TextWrapping.Wrap
            });

            var confirmDialog = new ContentDialog
            {
                Title = _tipoDocumentoActual == TipoDocumento.Facturas ? "Confirmar Eliminación de Factura NCF" : "Confirmar Eliminación",
                Content = messagePanel,
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();

                    if (_tipoDocumentoActual == TipoDocumento.Facturas)
                    {
                        // Borrar factura NCF por IdFactura
                        var factura = await context.Facturas.FindAsync(reciboSeleccionado.IdRecibo).ConfigureAwait(false);
                        if (factura != null)
                        {
                            context.Facturas.Remove(factura);
                            await context.SaveChangesAsync().ConfigureAwait(false);

                            // Recargar lista de facturas
                            await DispatcherQueue.EnqueueAsync(async () =>
                            {
                                await CargarFacturas();
                                await Task.Delay(100);
                                await ShowInfoDialog("Éxito", "Factura NCF eliminada correctamente");
                            });
                        }
                    }
                    else
                    {
                        // Comportamiento original: borrar recibo
                        var recibo = await context.Recibos.FindAsync(reciboSeleccionado.IdRecibo).ConfigureAwait(false);
                        if (recibo != null)
                        {
                            context.Recibos.Remove(recibo);
                            await context.SaveChangesAsync().ConfigureAwait(false);

                            _cacheService.InvalidateCache("recibos");

                            await DispatcherQueue.EnqueueAsync(async () =>
                            {
                                await LoadPageAsync(CurrentPage);
                                await Task.Delay(100);
                                await ShowInfoDialog("Éxito", "Recibo eliminado correctamente");
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    await DispatcherQueue.EnqueueAsync(async () =>
                    {
                        await ShowInfoDialog("Error", $"Error al eliminar: {ex.Message}");
                    });
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

            await Task.Delay(100);

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
        catch (System.Runtime.InteropServices.COMException comEx)
        {
            System.Diagnostics.Debug.WriteLine($"[DIALOG] Error COM: {comEx.Message}");
            await Task.Delay(500);
            try
            {
                var simpleDialog = new ContentDialog
                {
                    Title = title,
                    Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    CloseButtonText = "Aceptar",
                    XamlRoot = this.XamlRoot
                };
                await simpleDialog.ShowAsync();
            }
            catch (Exception retryEx)
            {
                System.Diagnostics.Debug.WriteLine($"[DIALOG] Error en reintento: {retryEx.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DIALOG] ERROR en ShowInfoDialogInternal: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[DIALOG] Stack trace: {ex.StackTrace}");
        }
    }

    private void LimpiarFormularioRapido()
    {
        var txtReciboRapidoNumero = this.FindName("txtReciboRapidoNumero") as TextBox;
        var txtReciboRapidoNombre = this.FindName("txtReciboRapidoNombre") as TextBox;
        var txtReciboRapidoMonto = this.FindName("txtReciboRapidoMonto") as TextBox;
        var txtReciboRapidoConcepto = this.FindName("txtReciboRapidoConcepto") as TextBox;
        var cmbReciboRapidoTipo = this.FindName("cmbReciboRapidoTipo") as ComboBox;
        var cmbReciboRapidoPago = this.FindName("cmbReciboRapidoPago") as ComboBox;

        if (txtReciboRapidoNumero != null) txtReciboRapidoNumero.Text = "";
        if (txtReciboRapidoNombre != null) txtReciboRapidoNombre.Text = "";
        if (txtReciboRapidoMonto != null) txtReciboRapidoMonto.Text = "";
        if (txtReciboRapidoConcepto != null) txtReciboRapidoConcepto.Text = "";
        if (cmbReciboRapidoTipo != null) cmbReciboRapidoTipo.SelectedIndex = 0;
        if (cmbReciboRapidoPago != null) cmbReciboRapidoPago.SelectedIndex = 0;
    }

    private void TxtReciboRapidoMonto_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Validar formato de monto en tiempo real
    }

    private async void GenerarReciboRapido_Click(object sender, RoutedEventArgs e)
    {
        var txtReciboRapidoNombre = this.FindName("txtReciboRapidoNombre") as TextBox;
        var txtReciboRapidoMonto = this.FindName("txtReciboRapidoMonto") as TextBox;
        var txtReciboRapidoConcepto = this.FindName("txtReciboRapidoConcepto") as TextBox;
        var cmbReciboRapidoTipo = this.FindName("cmbReciboRapidoTipo") as ComboBox;
        var cmbReciboRapidoPago = this.FindName("cmbReciboRapidoPago") as ComboBox;

        if (txtReciboRapidoNombre == null || string.IsNullOrWhiteSpace(txtReciboRapidoNombre.Text))
        {
            await ShowInfoDialog("Error", "Debe ingresar el nombre del beneficiario/pagador");
            return;
        }

        if (txtReciboRapidoMonto == null || string.IsNullOrWhiteSpace(txtReciboRapidoMonto.Text) || !decimal.TryParse(txtReciboRapidoMonto.Text, out decimal monto) || monto <= 0)
        {
            await ShowInfoDialog("Error", "Debe ingresar un monto válido");
            return;
        }

        if (txtReciboRapidoConcepto == null || string.IsNullOrWhiteSpace(txtReciboRapidoConcepto.Text))
        {
            await ShowInfoDialog("Error", "Debe ingresar el concepto del recibo");
            return;
        }

        var tipoSeleccionado = (cmbReciboRapidoTipo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Ingreso";
        var metodoPagoSeleccionado = (cmbReciboRapidoPago?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Efectivo";

        var nuevoRecibo = new Recibo
        {
            TipoRecibo = tipoSeleccionado,
            Fecha = DateTime.Now,
            RecibimosDe = txtReciboRapidoNombre.Text.Trim(),
            Monto = monto,
            MontoEnLetras = ConvertirNumeroALetras(monto),
            Concepto = txtReciboRapidoConcepto.Text.Trim(),
            EsEfectivo = metodoPagoSeleccionado == "Efectivo",
            EsCheque = metodoPagoSeleccionado == "Cheque",
            EsTransferencia = metodoPagoSeleccionado == "Transferencia"
        };

        // Normalizar flags (garantizar un 0/1 y exclusividad)
        if (!(nuevoRecibo.EsEfectivo == true || nuevoRecibo.EsTransferencia == true || nuevoRecibo.EsCheque == true))
        {
            nuevoRecibo.EsEfectivo = true;
        }
        else
        {
            nuevoRecibo.EsTransferencia = (nuevoRecibo.EsTransferencia == true) && !(nuevoRecibo.EsEfectivo == true) && !(nuevoRecibo.EsCheque == true);
            nuevoRecibo.EsCheque = (nuevoRecibo.EsCheque == true) && !(nuevoRecibo.EsEfectivo == true) && !(nuevoRecibo.EsTransferencia == true);
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
            
            // Calcular el siguiente IdRecibo y NumeroRecibo (MAX + 1)
            var maxIdRecibo = await context.Recibos.AnyAsync()
                ? await context.Recibos.MaxAsync(r => r.IdRecibo)
                : 0;
            var maxNumeroRecibo = await context.Recibos.AnyAsync()
                ? await context.Recibos.MaxAsync(r => (int?)r.NumeroRecibo) ?? 0
                : 0;
            
            nuevoRecibo.IdRecibo = maxIdRecibo + 1;
            nuevoRecibo.NumeroRecibo = maxNumeroRecibo + 1;
            
            System.Diagnostics.Debug.WriteLine($"[RECIBO-RAPIDO] Asignando IdRecibo={nuevoRecibo.IdRecibo}, NumeroRecibo={nuevoRecibo.NumeroRecibo}");
            
            context.Recibos.Add(nuevoRecibo);
            await context.SaveChangesAsync().ConfigureAwait(false);

            LimpiarFormularioRapido();
            _cacheService.InvalidateCache("recibos");
            await LoadPageAsync(CurrentPage);
            
            await ShowInfoDialog("Éxito", $"Recibo rápido generado correctamente.\nNo. Recibo: {nuevoRecibo.NumeroRecibo}\nMonto: RD$ {nuevoRecibo.Monto:N2}");
        }
        catch (Exception ex)
        {
            await ShowInfoDialog("Error", $"Error al guardar recibo rápido: {ex.Message}");
        }
    }

    private string ConvertirNumeroALetras(decimal numero)
    {
        if (numero == 0) return "Cero pesos 00/100";
        if (numero < 0) return "Número inválido";

        int parteEntera = (int)numero;
        int centavos = (int)Math.Round((numero - parteEntera) * 100);

        string resultado = ConvertirEnteroALetras(parteEntera);
        return $"{resultado} pesos {centavos:00}/100";
    }

    private string ConvertirEnteroALetras(int numero)
    {
        if (numero == 0) return "Cero";
        if (numero < 0) return "Número inválido";

        if (numero >= 1000000)
        {
            int millones = numero / 1000000;
            int resto = numero % 1000000;
            string textoMillones = millones == 1 ? "Un millón" : ConvertirEnteroALetras(millones) + " millones";
            if (resto > 0) return textoMillones + " " + ConvertirEnteroALetras(resto);
            return textoMillones;
        }

        if (numero >= 1000)
        {
            int miles = numero / 1000;
            int resto = numero % 1000;
            string textoMiles = miles == 1 ? "Mil" : ConvertirEnteroALetras(miles) + " mil";
            if (resto > 0) return textoMiles + " " + ConvertirEnteroALetras(resto);
            return textoMiles;
        }

        if (numero >= 100) return ConvertirCentenas(numero);
        return ConvertirDecenas(numero);
    }

    private string ConvertirCentenas(int numero)
    {
        string[] cientos = {
            "", "Ciento", "Doscientos", "Trescientos", "Cuatrocientos",
            "Quinientos", "Seiscientos", "Setecientos", "Ochocientos", "Novecientos"
        };

        int c = numero / 100;
        int resto = numero % 100;

        if (numero == 100) return "Cien";
        string resultado = cientos[c];
        if (resto > 0) resultado += " " + ConvertirDecenas(resto);
        return resultado;
    }

    private string ConvertirDecenas(int numero)
    {
        string[] unidades = { "", "Uno", "Dos", "Tres", "Cuatro", "Cinco", "Seis", "Siete", "Ocho", "Nueve" };
        string[] decenas = { "", "Diez", "Veinte", "Treinta", "Cuarenta", "Cincuenta", "Sesenta", "Setenta", "Ochenta", "Noventa" };
        string[] especiales = { "Diez", "Once", "Doce", "Trece", "Catorce", "Quince", "Dieciséis", "Diecisiete", "Dieciocho", "Diecinueve" };

        if (numero < 10) return unidades[numero];
        if (numero < 20) return especiales[numero - 10];
        if (numero < 30)
        {
            int u = numero % 10;
            return u == 0 ? "Veinte" : "Veinti" + unidades[u].ToLower();
        }
        if (numero < 100)
        {
            int d = numero / 10;
            int u = numero % 10;
            return u == 0 ? decenas[d] : decenas[d] + " y " + unidades[u];
        }
        return "";
    }

    private async Task<Recibo> MostrarDialogoRecibo(Recibo reciboExistente)
    {
        var formPanel = new StackPanel { Spacing = 16 };

        var txtNumero = new TextBox
        {
            Header = "Número de Recibo",
            PlaceholderText = "Calculando...",
            IsReadOnly = true
        };

        // Mostrar el próximo número de recibo al crear uno nuevo
        if (reciboExistente == null)
        {
            try
            {
                using var scopeNum = _serviceProvider.CreateScope();
                using var contextNum = scopeNum.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                var maxNumero = await contextNum.Recibos.AnyAsync()
                    ? await contextNum.Recibos.MaxAsync(r => (int?)r.NumeroRecibo) ?? 0
                    : 0;
                txtNumero.Text = (maxNumero + 1).ToString();
            }
            catch
            {
                txtNumero.PlaceholderText = "Se genera automáticamente";
            }
        }

        var txtTipo = new ComboBox
        {
            Header = "Tipo",
            MinWidth = 120
        };
        txtTipo.Items.Add(new ComboBoxItem { Content = "?? Ingreso", Tag = "Ingreso" });
        txtTipo.Items.Add(new ComboBoxItem { Content = "?? Egreso", Tag = "Egreso" });
        txtTipo.SelectedIndex = 0;

        var txtFecha = new CalendarDatePicker
        {
            Header = "Fecha"
        };

        var txtNombre = new TextBox
        {
            Header = "De (Beneficiario/Pagador) *",
            PlaceholderText = "Nombre completo"
        };

        var txtCedula = new TextBox
        {
            Header = "Cédula/RNC",
            PlaceholderText = "000-00000-0"
        };

        var txtMonto = new TextBox
        {
            Header = "Monto (RD$) *",
            PlaceholderText = "0.00"
        };

        var txtConcepto = new TextBox
        {
            Header = "Concepto *",
            PlaceholderText = "Descripción del recibo",
            TextWrapping = TextWrapping.Wrap,
            Height = 80
        };

        var cmbPago = new ComboBox
        {
            Header = "Método de Pago",
            MinWidth = 150
        };
        cmbPago.Items.Add(new ComboBoxItem { Content = "?? Efectivo", Tag = "Efectivo" });
        cmbPago.Items.Add(new ComboBoxItem { Content = "?? Transferencia", Tag = "Transferencia" });
        cmbPago.Items.Add(new ComboBoxItem { Content = "?? Cheque", Tag = "Cheque" });
        cmbPago.SelectedIndex = 0;

        // Campo de referencia/factura siempre visible
        var txtNumeroFactura = new TextBox { Header = "Número de Referencia/Factura", PlaceholderText = "Referencia de transferencia / NCF" };

        // Campos adicionales según método de pago
        var panelCheque = new StackPanel { Spacing = 8, Visibility = Visibility.Collapsed };
        var txtNumeroCheque = new TextBox { Header = "Número de Cheque", PlaceholderText = "000000" };
        var txtBanco = new TextBox { Header = "Banco", PlaceholderText = "Nombre del banco" };
        panelCheque.Children.Add(txtNumeroCheque);
        panelCheque.Children.Add(txtBanco);

        cmbPago.SelectionChanged += (s, e) =>
        {
            var metodo = (cmbPago.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            panelCheque.Visibility = metodo == "Cheque" ? Visibility.Visible : Visibility.Collapsed;
        };
        // Inicializar visibilidad al cargar
        var metodoInicial = (cmbPago.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        panelCheque.Visibility = metodoInicial == "Cheque" ? Visibility.Visible : Visibility.Collapsed;

        // Prefill valores si es edición
        if (reciboExistente != null)
        {
            txtNumero.Text = reciboExistente.NumeroRecibo.ToString();
            txtFecha.Date = new DateTimeOffset(reciboExistente.Fecha);
            txtNombre.Text = reciboExistente.RecibimosDe;
            txtCedula.Text = reciboExistente.Cedula ?? string.Empty;
            txtMonto.Text = reciboExistente.Monto.ToString("N2");
            txtConcepto.Text = reciboExistente.Concepto ?? string.Empty;

            // Método de pago
            if (reciboExistente.EsCheque == true)
                cmbPago.SelectedIndex = 2;
            else if (reciboExistente.EsTransferencia == true)
                cmbPago.SelectedIndex = 1;
            else
                cmbPago.SelectedIndex = 0;

            txtNumeroCheque.Text = reciboExistente.NumeroCheque ?? string.Empty;
            txtBanco.Text = reciboExistente.Banco ?? string.Empty;
            txtNumeroFactura.Text = reciboExistente.NumeroFacturaNCF ?? string.Empty;
        }

        // Agregar controles al panel del formulario para que se muestren
        formPanel.Children.Add(txtNumero);
        formPanel.Children.Add(txtTipo);
        formPanel.Children.Add(txtFecha);
        formPanel.Children.Add(txtNombre);
        formPanel.Children.Add(txtCedula);
        formPanel.Children.Add(txtMonto);
        formPanel.Children.Add(txtConcepto);
        formPanel.Children.Add(cmbPago);
        formPanel.Children.Add(txtNumeroFactura);
        formPanel.Children.Add(panelCheque);

        var scrollViewer = new ScrollViewer
        {
            Content = formPanel,
            MaxHeight = 500,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var dialog = new ContentDialog
        {
            Title = reciboExistente == null ? "Nuevo Recibo" : "Editar Recibo",
            Content = scrollViewer,
            PrimaryButtonText = reciboExistente == null ? "Crear" : "Guardar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                await ShowInfoDialog("Error", "Debe ingresar el nombre del beneficiario/pagador");
                return null;
            }

            if (!decimal.TryParse(txtMonto.Text, out decimal monto) || monto <= 0)
            {
                await ShowInfoDialog("Error", "Debe ingresar un monto válido");
                return null;
            }

            if (string.IsNullOrWhiteSpace(txtConcepto.Text))
            {
                await ShowInfoDialog("Error", "Debe ingresar el concepto");
                return null;
            }

            var tipoSeleccionado = (txtTipo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Ingreso";
            var pagoSeleccionado = (cmbPago.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Efectivo";

            // Normalizar flags de método de pago para DB (0/1). Default: Efectivo
            bool esEfectivo = false, esTransferencia = false, esCheque = false;
            switch (pagoSeleccionado)
            {
                case "Cheque":
                    esCheque = true;
                    break;
                case "Transferencia":
                    esTransferencia = true;
                    break;
                case "Efectivo":
                default:
                    esEfectivo = true;
                    break;
            }

            return new Recibo
            {
                IdRecibo = reciboExistente?.IdRecibo ?? 0,
                TipoRecibo = tipoSeleccionado,
                Fecha = txtFecha.Date?.DateTime ?? DateTime.Now,
                RecibimosDe = txtNombre.Text.Trim(),
                Cedula = txtCedula.Text.Trim(),
                Monto = monto,
                MontoEnLetras = ConvertirNumeroALetras(monto),
                Concepto = txtConcepto.Text.Trim(),
                EsEfectivo = esEfectivo,
                EsTransferencia = esTransferencia,
                EsCheque = esCheque,
                // Número de Referencia/Factura siempre se guarda si tiene valor
                NumeroFacturaNCF = !string.IsNullOrWhiteSpace(txtNumeroFactura.Text) ? txtNumeroFactura.Text.Trim() : null,
                NumeroCheque = pagoSeleccionado == "Cheque" ? txtNumeroCheque.Text.Trim() : null,
                Banco = pagoSeleccionado == "Cheque" ? txtBanco.Text.Trim() : null
            };
        }

        return null;
    }

    private void BtnActualizar_Click(object sender, RoutedEventArgs e)
    {
        if (_tipoDocumentoActual == TipoDocumento.Recibos)
        {
            _cacheService.InvalidateCache("recibos");
            _ = Task.Run(async () => await LoadPageAsync(CurrentPage).ConfigureAwait(false));
        }
        else
        {
            _ = Task.Run(async () => await CargarFacturas().ConfigureAwait(false));
        }
    }

    private void Ordenar_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Implementar lógica de ordenamiento si es necesario
    }

    private void BtnGenerarReporte_Click(object sender, RoutedEventArgs e)
    {
        // Optimizado: generar reporte asíncrono y no bloquear UI usando PdfReportService
        if (IsLoading) return;

        _ = DispatcherQueue.EnqueueAsync(async () =>
        {
            try
            {
                IsLoading = true;

                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();

                // Offload trabajo pesado (generación) al threadpool con contexto EF
                await Task.Run(async () =>
                {
                    var reportService = new PdfReportService(context);
                    // Usa un reporte ligero existente para abrir rápido (ej. por área)
                    await reportService.GenerarReporteAreaAsync();
                });

                await ShowInfoDialog("Éxito", "Reporte generado y abierto correctamente.");
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al generar reporte: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        });
    }

    private async void BtnImprimirRecibo_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG] BtnImprimirRecibo_Click - INICIO");
        
        var documentoSeleccionado = RecibosListView?.SelectedItem as Recibo;
        if (documentoSeleccionado == null)
        {
            string tipoDocumento = _tipoDocumentoActual == TipoDocumento.Recibos ? "recibo" : "factura";
            await ShowInfoDialog("Error", $"Debe seleccionar un {tipoDocumento}");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[DEBUG] Documento seleccionado: {documentoSeleccionado.NumeroRecibo}");
        
        // Mostrar indicador de carga
        MostrarLoadingConMensaje(
            _tipoDocumentoActual == TipoDocumento.Recibos ? "Generando recibo..." : "Generando factura NCF...",
            "Por favor espere mientras se genera el PDF"
        );
        
        try
        {
            if (_tipoDocumentoActual == TipoDocumento.Recibos)
            {
                await ImprimirReciboDirecto();
            }
            else
            {
                await ImprimirFacturaNcfDirecto(documentoSeleccionado);
            }
        }
        finally
        {
            // Ocultar indicador de carga
            OcultarLoading();
        }
        
        System.Diagnostics.Debug.WriteLine("[DEBUG] BtnImprimirRecibo_Click - FIN");
    }

    private async Task<bool> ImprimirReciboDirecto()
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG] ImprimirReciboDirecto - INICIO");
        
        var reciboSeleccionado = RecibosListView?.SelectedItem as Recibo;
        if (reciboSeleccionado == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar un recibo");
            return false;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] Generando PDF con iText7...");
            
            var pdfService = new ReciboPdfService();
            await pdfService.AbrirReciboPdfAsync(reciboSeleccionado);

            System.Diagnostics.Debug.WriteLine("[DEBUG] PDF generado y abierto exitosamente");

            await ShowInfoDialog("Éxito",
                "Recibo generado correctamente.\n\n" +
                "El PDF se ha abierto en el visor predeterminado.\n" +
                "Tamaño: Media Carta (8.5\" x 5.5\")\n\n" +
                "Desde ahí puede imprimirlo usando Ctrl+P.");

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG ERROR] {ex.Message}\n{ex.StackTrace}");
            await ShowInfoDialog("Error", $"Error al generar recibo: {ex.Message}");
            return false;
        }
        finally
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] ImprimirReciboDirecto - FIN");
        }
    }

    private async Task<bool> ImprimirFacturaNcfDirecto(Recibo facturaVirtual)
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG] ImprimirFacturaNcfDirecto - INICIO");

        try
        {
            // Verificar si es una ReciboFacturaNcf con datos ya cargados
            if (facturaVirtual is ReciboFacturaNcf facturaConDatos)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] ? Usando datos ya cargados desde CargarFacturas()");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] NCF: {facturaConDatos.NCFCompleto}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Exento: {facturaConDatos.Exento}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Gravado: {facturaConDatos.Gravado}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Itbis: {facturaConDatos.Itbis}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Monto: {facturaConDatos.Monto}");

                // Validación: si todos están en cero pero hay monto, asumir exento
                decimal exento = facturaConDatos.Exento;
                decimal gravado = facturaConDatos.Gravado;
                decimal itbis = facturaConDatos.Itbis;
                decimal monto = facturaConDatos.Monto;

                if (exento == 0 && gravado == 0 && itbis == 0 && monto > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ?? Valores en cero detectados, usando Monto como Exento");
                    exento = monto;
                }

                // Crear objeto FacturaNcf directamente con los datos ya cargados
                var facturaNcf = new FacturaNcf
                {
                    NCF = facturaConDatos.NCFCompleto,
                    Fecha = facturaConDatos.Fecha,
                    ValidaHasta = facturaConDatos.ValidaHasta,
                    RncCliente = facturaConDatos.Cedula ?? "Sin RNC",
                    NombreCliente = facturaConDatos.RecibimosDe?.ToUpper() ?? "CLIENTE SIN NOMBRE",
                    TelefonoCliente = facturaConDatos.TelefonoCliente,
                    DireccionCliente = facturaConDatos.DireccionCliente?.ToUpper() ?? "DIRECCIÓN NO ESPECIFICADA",
                    Concepto = facturaConDatos.Concepto ?? "DONATIVO PARA PACIENTES ONCOLOGICOS DE ESCASOS RECURSOS.",
                    Monto = monto,
                    Exento = exento,
                    Gravado = gravado,
                    Itbis = itbis,
                    EsEfectivo = facturaConDatos.EsEfectivo ?? false,
                    EsCheque = facturaConDatos.EsCheque ?? false,
                    EsCredito = facturaConDatos.EsTransferencia ?? false,
                    NumeroCheque = facturaConDatos.NumeroCheque,
                    Banco = facturaConDatos.Banco
                };

                // Generar y abrir PDF
                var pdfService = new FacturaNcfPdfService();
                await pdfService.AbrirFacturaPdfAsync(facturaNcf);

                System.Diagnostics.Debug.WriteLine("[DEBUG] ? Factura NCF PDF generado exitosamente con datos precargados");

                await ShowInfoDialog("Éxito",
                    "Factura NCF generada correctamente.\n\n" +
                    $"NCF: {facturaNcf.NCF}\n" +
                    $"Cliente: {facturaNcf.NombreCliente}\n" +
                    $"Exento: RD$ {facturaNcf.Exento:N2}\n" +
                    $"Gravado: RD$ {facturaNcf.Gravado:N2}\n" +
                    $"Itbis: RD$ {facturaNcf.Itbis:N2}\n" +
                    $"Total: RD$ {facturaNcf.Monto:N2}\n\n" +
                    "El PDF se ha abierto en el visor predeterminado.\n" +
                    "Desde ahí puede imprimirlo usando Ctrl+P.");

                return true;
            }

            // Fallback: obtener de BD si no es ReciboFacturaNcf (no debería pasar, pero por seguridad)
            System.Diagnostics.Debug.WriteLine("[DEBUG] ?? FALLBACK: Obteniendo datos desde BD...");
            
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
            
            var facturaReal = await context.Facturas
                .Include(f => f.Cliente)
                .FirstOrDefaultAsync(f => f.IdFactura == facturaVirtual.IdRecibo);

            if (facturaReal == null)
            {
                await ShowInfoDialog("Error", "No se encontró la factura en la base de datos");
                return false;
            }

            if (!facturaReal.NCFNumerico.HasValue)
            {
                await ShowInfoDialog("Error", "Esta factura no tiene NCF asignado");
                return false;
            }

            // Log para debug de valores desde BD
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Factura ID: {facturaReal.IdFactura}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Exento BD: {facturaReal.Exento}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Gravado BD: {facturaReal.Gravado}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Itbis BD: {facturaReal.Itbis}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] APagar BD: {facturaReal.APagar}");

            // Construir NCF completo
            var tipoComprobante = facturaReal.TCFNumerico == 14 ? "B14" : 
                                 facturaReal.TCFNumerico == 15 ? "B15" : "B01";
            var ncfCompleto = $"{tipoComprobante}{facturaReal.NCFNumerico:D8}";

            // Validar valores
            decimal exentoBD = facturaReal.Exento;
            decimal gravadoBD = facturaReal.Gravado;
            decimal itbisBD = facturaReal.Itbis;
            decimal montoBD = facturaReal.APagar;

            if (exentoBD == 0 && gravadoBD == 0 && itbisBD == 0 && montoBD > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Valores BD en cero, usando APagar como Exento");
                exentoBD = montoBD;
            }

            var facturaNcfBD = new FacturaNcf
            {
                NCF = ncfCompleto,
                Fecha = facturaReal.Fecha,
                ValidaHasta = facturaReal.FechaVencimiento ?? DateTime.Now.AddMonths(1),
                RncCliente = facturaReal.Cliente?.rnc ?? "Sin RNC",
                NombreCliente = facturaReal.Cliente?.nombre?.ToUpper() ?? "CLIENTE SIN NOMBRE",
                TelefonoCliente = facturaReal.Cliente?.telefono ?? "",
                DireccionCliente = facturaReal.Cliente?.direccion?.ToUpper() ?? "DIRECCIÓN NO ESPECIFICADA",
                Concepto = facturaVirtual.Concepto ?? "DONATIVO PARA PACIENTES ONCOLOGICOS DE ESCASOS RECURSOS.",
                Monto = montoBD,
                Exento = exentoBD,
                Gravado = gravadoBD,
                Itbis = itbisBD,
                EsEfectivo = facturaReal.EsEfectivo,
                EsCheque = facturaReal.EsCheque,
                EsCredito = facturaReal.EsCredito,
                NumeroCheque = facturaReal.NumeroCheque,
                Banco = facturaReal.Banco
            };

            var pdfServiceBD = new FacturaNcfPdfService();
            await pdfServiceBD.AbrirFacturaPdfAsync(facturaNcfBD);

            System.Diagnostics.Debug.WriteLine("[DEBUG] ? Factura NCF PDF generado desde BD fallback");

            await ShowInfoDialog("Éxito",
                "Factura NCF generada correctamente.\n\n" +
                $"NCF: {facturaNcfBD.NCF}\n" +
                $"Cliente: {facturaNcfBD.NombreCliente}\n" +
                $"Exento: RD$ {facturaNcfBD.Exento:N2}\n" +
                $"Gravado: RD$ {facturaNcfBD.Gravado:N2}\n" +
                $"Itbis: RD$ {facturaNcfBD.Itbis:N2}\n" +
                $"Total: RD$ {facturaNcfBD.Monto:N2}\n\n" +
                "El PDF se ha abierto en el visor predeterminado.\n" +
                "Desde ahí puede imprimirlo usando Ctrl+P.");

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG ERROR] {ex.Message}\n{ex.StackTrace}");
            await ShowInfoDialog("Error", $"Error al generar factura NCF: {ex.Message}");
            return false;
        }
        finally
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] ImprimirFacturaNcfDirecto - FIN");
        }
    }

    private async void BtnGenerarFacturaNcf_Click(object sender, RoutedEventArgs e)
    {
        var reciboSeleccionado = RecibosListView?.SelectedItem as Recibo;
        if (reciboSeleccionado == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar un recibo");
            return;
        }

        await MostrarDialogoFacturaNcf(reciboSeleccionado);
    }

    private async void BtnConfigurarSecuenciaNcf_Click(object sender, RoutedEventArgs e)
    {
        await MostrarDialogoConfiguracionSecuencia();
    }

    private async Task MostrarDialogoFacturaNcf(Recibo reciboSeleccionado)
    {
        var formPanel = new StackPanel { Spacing = 12 };

        // Tipo de comprobante NCF
        var cmbTipoComprobante = new ComboBox
        {
            Header = "Tipo de Comprobante NCF",
            MinWidth = 150
        };
        cmbTipoComprobante.Items.Add(new ComboBoxItem { Content = "B01 - Crédito Fiscal", Tag = "B01" });
        cmbTipoComprobante.Items.Add(new ComboBoxItem { Content = "B14 - Regímenes Especiales", Tag = "B14" });
        cmbTipoComprobante.Items.Add(new ComboBoxItem { Content = "B15 - Factura Gubernamental", Tag = "B15" });
        cmbTipoComprobante.SelectedIndex = 0;

        var txtNCFNumero = new TextBox
        {
            Header = "Número NCF (sin prefijo) *",
            PlaceholderText = "Ingrese el número correlativo",
            MaxLength = 15
        };

        // Panel para botones de secuencia NCF
        var panelSecuenciaBotones = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        
        var btnUsarSecuencia = new Button
        {
            Content = "?? Usar Secuencia Auto",
            Height = 32
        };

        var txtEstadoSecuencia = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
            FontSize = 11,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
        };

        // Actualizar estado de la secuencia
        void ActualizarEstadoSecuencia()
        {
            var (activa, actual, inicio, fin, restantes) = _ncfSequenceService.ObtenerEstado();
            if (activa)
            {
                txtEstadoSecuencia.Text = $"?? Siguiente: {actual} (Restantes: {restantes})";
                txtEstadoSecuencia.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                btnUsarSecuencia.IsEnabled = true;
            }
            else
            {
                txtEstadoSecuencia.Text = "?? Secuencia inactiva - Use el botón 'Config. NCF' en la barra superior";
                txtEstadoSecuencia.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange);
                btnUsarSecuencia.IsEnabled = false;
            }
        }

        // Botón: Usar secuencia
        btnUsarSecuencia.Click += (s, e) =>
        {
            var siguiente = _ncfSequenceService.ObtenerSiguienteNumero();
            if ( siguiente.HasValue)
            {
                txtNCFNumero.Text = siguiente.Value.ToString();
                ActualizarEstadoSecuencia();
            }
            else
            {
                _ = ShowInfoDialog("Secuencia Agotada", 
                    "La secuencia de NCF ha llegado a su fin.\n\n" +
                    "Use el botón 'Config. NCF' en la barra superior para configurar una nueva secuencia.");
            }
        };

        panelSecuenciaBotones.Children.Add(btnUsarSecuencia);
        panelSecuenciaBotones.Children.Add(txtEstadoSecuencia);

        // Actualizar estado inicial
        ActualizarEstadoSecuencia();

        var txtNCF = new TextBox
        {
            Header = "NCF Completo",
            PlaceholderText = "Se construye automáticamente",
            IsReadOnly = true
        };

        // Actualizar NCF completo al cambiar tipo o número
        void UpdateNcfPreview()
        {
            var tipo = (cmbTipoComprobante.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "B01";
            var numero = new string((txtNCFNumero.Text ?? string.Empty).Where(char.IsDigit).ToArray());
            txtNCF.Text = string.IsNullOrWhiteSpace(numero) ? string.Empty : $"{tipo}0000{numero}";
        }
        cmbTipoComprobante.SelectionChanged += (s, e) => UpdateNcfPreview();
        txtNCFNumero.TextChanged += (s, e) => UpdateNcfPreview();

        var dpValidaHasta = new CalendarDatePicker
        {
            Header = "Válida hasta *",
            Date = DateTime.Now.AddMonths(1)
        };

        var txtRncCliente = new TextBox
        {
            Header = "RNC Cliente *",
            PlaceholderText = "000-00000-0",
            Text = reciboSeleccionado.Cedula ?? ""
        };

        var txtNombreCliente = new TextBox
        {
            Header = "Nombre Cliente *",
            PlaceholderText = "Nombre completo o razón social",
            Text = reciboSeleccionado.RecibimosDe
        };

        var txtTelefono = new TextBox
        {
            Header = "Teléfono",
            PlaceholderText = "809-000-0000"
        };

        var txtDireccion = new TextBox
        {
            Header = "Dirección *",
            PlaceholderText = "Dirección completa del cliente",
            TextWrapping = TextWrapping.Wrap,
            Height = 60
        };

        var txtConcepto = new TextBox
        {
            Header = "Concepto/Descripción *",
            Text = reciboSeleccionado.Concepto ?? "DONATIVO PARA PACIENTES ONCOLOGICOS DE ESCASOS RECURSOS.",
            TextWrapping = TextWrapping.Wrap,
            Height = 80
        };

        // Selección Exento/Gravado
        var grdImpuestoPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        var rbExento = new RadioButton { Content = "Exento", IsChecked = true };
        var rbGravado = new RadioButton { Content = "Gravado" };
        grdImpuestoPanel.Children.Add(new TextBlock { Text = "Tipo de Impuesto:", VerticalAlignment = VerticalAlignment.Center });
        grdImpuestoPanel.Children.Add(rbExento);
        grdImpuestoPanel.Children.Add(rbGravado);

        var txtMonto = new TextBox
        {
            Header = "Monto (RD$) *",
            Text = reciboSeleccionado.Monto.ToString("N2"),
            IsReadOnly = false
        };

        // Vista previa de totales: Exento, Gravado, Itbis
        var previewPanel = new StackPanel { Spacing = 4 };
        var lblExento = new TextBlock();
        var lblGravado = new TextBlock();
        var lblItbis = new TextBlock();
        previewPanel.Children.Add(new TextBlock { Text = "Vista previa de totales:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        previewPanel.Children.Add(lblExento);
        previewPanel.Children.Add(lblGravado);
        previewPanel.Children.Add(lblItbis);

        void UpdateTotalsPreview()
        {
            // Tomar el monto desde el textbox editable
            decimal monto = 0m;
            decimal.TryParse(txtMonto.Text, out monto);
            const decimal itbisRate = 0.18m;
            if (rbGravado.IsChecked == true)
            {
                var gravado = monto;
                var itbis = Math.Round(gravado * itbisRate, 2);
                var exento = 0.00m;
                lblExento.Text = $"Exento: RD$ {exento:N2}";
                lblGravado.Text = $"Gravado: RD$ {gravado:N2}";
                lblItbis.Text = $"Itbis (18%): RD$ {itbis:N2}";
            }
            else
            {
                var exento = monto;
                lblExento.Text = $"Exento: RD$ {exento:N2}";
                lblGravado.Text = "Gravado: RD$ 0.00";
                lblItbis.Text = "Itbis: RD$ 0.00";
            }
        }
        txtMonto.TextChanged += (s, e) => UpdateTotalsPreview();
        rbExento.Checked += (s, e) => UpdateTotalsPreview();
        rbGravado.Checked += (s, e) => UpdateTotalsPreview();
        UpdateTotalsPreview();

        var cmbMetodoPago = new ComboBox
        {
            Header = "Método de Pago",
            MinWidth = 150
        };
        cmbMetodoPago.Items.Add(new ComboBoxItem { Content = "?? Efectivo", Tag = "Efectivo" });
        cmbMetodoPago.Items.Add(new ComboBoxItem { Content = "?? Cheque", Tag = "Cheque" });
        cmbMetodoPago.Items.Add(new ComboBoxItem { Content = "?? Crédito", Tag = "Credito" });
        
        if (reciboSeleccionado.EsEfectivo == true)
            cmbMetodoPago.SelectedIndex = 0;
        else if (reciboSeleccionado.EsCheque == true)
            cmbMetodoPago.SelectedIndex = 1;
        else
            cmbMetodoPago.SelectedIndex = 2;

        StackPanel panelCheque = new StackPanel { Spacing = 8, Visibility = Visibility.Collapsed };
        TextBox txtNumeroCheque = new TextBox { Header = "Número de Cheque", PlaceholderText = "000000" };
        TextBox txtBanco = new TextBox { Header = "Banco", PlaceholderText = "Nombre del banco" };
        panelCheque.Children.Add(txtNumeroCheque);
        panelCheque.Children.Add(txtBanco);

        StackPanel panelTransferencia = new StackPanel { Spacing = 8, Visibility = Visibility.Collapsed };
        TextBox txtNumeroFactura = new TextBox { Header = "Número de Referencia/Factura", PlaceholderText = "Referencia de transferencia / NCF" };
        panelTransferencia.Children.Add(txtNumeroFactura);

        cmbMetodoPago.SelectionChanged += (s, e) =>
        {
            var metodo = (cmbMetodoPago.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            panelCheque.Visibility = metodo == "Cheque" ? Visibility.Visible : Visibility.Collapsed;
            panelTransferencia.Visibility = metodo == "Transferencia" ? Visibility.Visible : Visibility.Collapsed;
        };

        // Si viene desde edición de Facturas NCF, precargar NCF desde la BD
        try
        {
            using var scopePrefill = _serviceProvider.CreateScope();
            using var contextPrefill = scopePrefill.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
            var facturaExistente = await contextPrefill.Facturas.Include(f => f.Cliente)
                .FirstOrDefaultAsync(f => f.IdFactura == reciboSeleccionado.IdRecibo);
            if (facturaExistente != null && facturaExistente.NCFNumerico.HasValue)
            {
                var prefijo = facturaExistente.TCFNumerico == 14 ? "B14" : facturaExistente.TCFNumerico == 15 ? "B15" : "B01";
                var numeroSolo = facturaExistente.NoFactura.ToString();
                txtNCFNumero.Text = numeroSolo;
                // seleccionar prefijo en combo
                for (int i = 0; i < cmbTipoComprobante.Items.Count; i++)
                {
                    if (cmbTipoComprobante.Items[i] is ComboBoxItem cbi && (cbi.Tag?.ToString() ?? "") == prefijo)
                    {
                        cmbTipoComprobante.SelectedIndex = i;
                        break;
                    }
                }
                UpdateNcfPreview();
            }
        }
        catch { /* ignorar prefill errores */ }

        // Añadir controles
        formPanel.Children.Add(cmbTipoComprobante);
        formPanel.Children.Add(txtNCFNumero);
        formPanel.Children.Add(panelSecuenciaBotones);
        formPanel.Children.Add(txtNCF);
        formPanel.Children.Add(dpValidaHasta);
        formPanel.Children.Add(txtRncCliente);
        formPanel.Children.Add(txtNombreCliente);
        formPanel.Children.Add(txtTelefono);
        formPanel.Children.Add(txtDireccion);
        formPanel.Children.Add(txtConcepto);
        formPanel.Children.Add(grdImpuestoPanel);
        formPanel.Children.Add(previewPanel);
        formPanel.Children.Add(txtMonto);
        formPanel.Children.Add(cmbMetodoPago);
        formPanel.Children.Add(panelTransferencia);
        formPanel.Children.Add(panelCheque);

        var scrollViewer = new ScrollViewer
        {
            Content = formPanel,
            MaxHeight = 600,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var dialog = new ContentDialog
        {
            Title = "Crear Factura NCF",
            Content = scrollViewer,
            PrimaryButtonText = "?? Crear y Generar PDF",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var tipo = (cmbTipoComprobante.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "B01";
            var numeroSolo = new string((txtNCFNumero.Text ?? string.Empty).Where(char.IsDigit).ToArray());
            var ncfCompleto = $"{tipo}0000{numeroSolo}";

            if (string.IsNullOrWhiteSpace(numeroSolo))
            {
                await ShowInfoDialog("Error", "Debe ingresar el número NCF");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtRncCliente.Text))
            {
                await ShowInfoDialog("Error", "Debe ingresar el RNC del cliente");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtNombreCliente.Text))
            {
                await ShowInfoDialog("Error", "Debe ingresar el nombre del cliente");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtDireccion.Text))
            {
                await ShowInfoDialog("Error", "Debe ingresar la dirección del cliente");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtConcepto.Text))
            {
                await ShowInfoDialog("Error", "Debe ingresar el concepto");
                return;
            }

            try
            {
                IsLoading = true;

                var metodoPago = (cmbMetodoPago.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Efectivo";

                // Validar monto y asignarlo al recibo seleccionado para crear la factura
                if (!decimal.TryParse(txtMonto.Text, out var montoIngresado) || montoIngresado <= 0)
                {
                    await ShowInfoDialog("Error", "Debe ingresar un monto válido");
                    return;
                }
                reciboSeleccionado.Monto = montoIngresado;

                var datosFacturaNcf = new DatosFacturaNcf
                {
                    NCF = ncfCompleto,
                    ValidaHasta = dpValidaHasta.Date?.DateTime ?? DateTime.Now.AddMonths(1),
                    RncCliente = txtRncCliente.Text.Trim(),
                    NombreCliente = txtNombreCliente.Text.Trim().ToUpper(),
                    TelefonoCliente = txtTelefono.Text.Trim(),
                    DireccionCliente = txtDireccion.Text.Trim().ToUpper(),
                    Concepto = txtConcepto.Text.Trim().ToUpper(),
                    MetodoPago = metodoPago,
                    NumeroCheque = metodoPago == "Cheque" ? txtNumeroCheque.Text.Trim() : null,
                    Banco = metodoPago == "Cheque" ? txtBanco.Text.Trim() : null,
                    EsGravada = rbGravado.IsChecked == true
                };

                // Si existe una factura (cuando estamos editando), actualizarla; si no, crear nueva
                using (var scopeUpd = _serviceProvider.CreateScope())
                using (var contextUpd = scopeUpd.ServiceProvider.GetRequiredService<RamaFemeninaContext>())
                {
                    var facturaExistente = await contextUpd.Facturas.Include(f => f.Cliente)
                        .FirstOrDefaultAsync(f => f.IdFactura == reciboSeleccionado.IdRecibo);

                    var itbisRate = 0.18m;
                    var monto = reciboSeleccionado.Monto;
                    decimal exento = 0.00m, gravado = 0.00m, itbis = 0.00m;
                    if (datosFacturaNcf.EsGravada)
                    {
                        gravado = monto;
                        itbis = Math.Round(gravado * itbisRate, 2);
                        exento = 0.00m;
                    }
                    else
                    {
                        exento = monto;
                        gravado = 0.00m;
                        itbis = 0.00m;
                    }

                    if (facturaExistente != null)
                    {
                        // Actualizar datos en factura existente
                        facturaExistente.NoFactura = int.TryParse(numeroSolo, out var nf) ? nf : facturaExistente.NoFactura;
                        facturaExistente.NCFNumerico = long.TryParse("0000" + numeroSolo, out var ncfNum) ? ncfNum : facturaExistente.NCFNumerico;
                        facturaExistente.TCFNumerico = tipo == "B01" ? 1 : tipo == "B14" ? 14 : tipo == "B15" ? 15 : null;
                        facturaExistente.Exento = exento;
                        facturaExistente.Gravado = gravado;
                        facturaExistente.Itbis = itbis;
                        facturaExistente.APagar = exento + gravado + itbis;
                        facturaExistente.Pago = facturaExistente.APagar;
                        facturaExistente.EsEfectivo = metodoPago == "Efectivo";
                        facturaExistente.EsCheque = metodoPago == "Cheque";
                        facturaExistente.EsCredito = metodoPago == "Credito";
                        facturaExistente.NumeroCheque = datosFacturaNcf.NumeroCheque;
                        facturaExistente.Banco = datosFacturaNcf.Banco;
                        facturaExistente.FechaVencimientoTexto = datosFacturaNcf.ValidaHasta.ToString("dd/MM/yyyy");

                        // Cliente
                        var cliente = await contextUpd.Clientes.FirstOrDefaultAsync(c => c.rnc == datosFacturaNcf.RncCliente);
                        if (cliente == null)
                        {
                            cliente = new Clientes
                            {
                                rnc = datosFacturaNcf.RncCliente,
                                nombre = datosFacturaNcf.NombreCliente,
                                telefono = datosFacturaNcf.TelefonoCliente,
                                direccion = datosFacturaNcf.DireccionCliente
                            };
                            contextUpd.Clientes.Add(cliente);
                            await contextUpd.SaveChangesAsync();
                        }
                        else
                        {
                            cliente.nombre = datosFacturaNcf.NombreCliente;
                            cliente.telefono = datosFacturaNcf.TelefonoCliente;
                            cliente.direccion = datosFacturaNcf.DireccionCliente;
                            await contextUpd.SaveChangesAsync();
                        }
                        facturaExistente.IdCliente = cliente.idCliente;

                        await contextUpd.SaveChangesAsync();

                        // Generar PDF con datos actualizados
                        await GenerarPdfDesdeFactura(facturaExistente, datosFacturaNcf.Concepto, tipo);

                        await contextUpd.Entry(facturaExistente).Reference(f => f.Cliente).LoadAsync();

                        // Construir NCF completo para la UI
                        var tipoComprobanteActualizado = tipo == "B14" ? "B14" : tipo == "B15" ? "B15" : "B01";
                        var ncfCompletoActualizado = $"{tipoComprobanteActualizado}{facturaExistente.NCFNumerico:D8}";

                        // Actualizar la fila seleccionada en la UI con un ReciboFacturaNcf completo
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            var idx = RecibosCollection.IndexOf(reciboSeleccionado);
                            if (idx >= 0)
                            {
                                var actualizado = new ReciboFacturaNcf
                                {
                                    IdRecibo = facturaExistente.IdFactura,
                                    NumeroRecibo = facturaExistente.NoFactura,
                                    TipoRecibo = "Factura NCF",
                                    Fecha = facturaExistente.Fecha,
                                    RecibimosDe = facturaExistente.Cliente?.nombre ?? reciboSeleccionado.RecibimosDe,
                                    Monto = facturaExistente.APagar,
                                    Concepto = datosFacturaNcf.Concepto,
                                    EsEfectivo = facturaExistente.EsEfectivo,
                                    EsCheque = facturaExistente.EsCheque,
                                    EsTransferencia = facturaExistente.EsCredito,
                                    NumeroCheque = facturaExistente.NumeroCheque,
                                    Banco = facturaExistente.Banco,
                                    Cedula = facturaExistente.Cliente?.rnc ?? reciboSeleccionado.Cedula,
                                    // ? IMPORTANTE: Incluir los valores actualizados
                                    Exento = facturaExistente.Exento,
                                    Gravado = facturaExistente.Gravado,
                                    Itbis = facturaExistente.Itbis,
                                    NCFCompleto = ncfCompletoActualizado,
                                    TCFNumerico = facturaExistente.TCFNumerico,
                                    ValidaHasta = facturaExistente.FechaVencimiento ?? DateTime.Now.AddMonths(1),
                                    DireccionCliente = facturaExistente.Cliente?.direccion ?? "",
                                    TelefonoCliente = facturaExistente.Cliente?.telefono ?? ""
                                };
                                RecibosCollection[idx] = actualizado;
                                
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] ? Fila actualizada en UI con valores: Exento={actualizado.Exento}, Gravado={actualizado.Gravado}, Itbis={actualizado.Itbis}");
                            }
                            UpdatePaginationControls();
                        });

                        await ShowInfoDialog("Éxito", 
                            $"Factura NCF actualizada correctamente.\n\n" +
                            $"NCF: {ncfCompletoActualizado}\n" +
                            $"Exento: RD$ {exento:N2}\n" +
                            $"Gravado: RD$ {gravado:N2}\n" +
                            $"Itbis: RD$ {itbis:N2}\n" +
                            $"Total: RD$ {facturaExistente.APagar:N2}");
                    }
                    else
                    {
                        var facturaCreada = await CrearFacturaEnBaseDatos(reciboSeleccionado, datosFacturaNcf, tipo, numeroSolo);
                        await GenerarPdfDesdeFactura(facturaCreada, datosFacturaNcf.Concepto, tipo);

                        await ShowInfoDialog("Éxito", "Factura NCF creada y guardada correctamente.");
                    }
                }

                if (_tipoDocumentoActual == TipoDocumento.Facturas)
                {
                    await CargarFacturas();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG ERROR] Error al crear factura NCF: {ex.Message}");
                await ShowInfoDialog("? Error", $"Error al crear factura NCF: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async Task<Factura> CrearFacturaEnBaseDatos(Recibo recibo, DatosFacturaNcf datos, string tipoComprobante = "B01", string numeroSolo = null)
    {
        // Ajustar creación usando tipo y número
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
        try
        {
            // Obtener o crear cliente (inline)
            var cliente = await context.Clientes.FirstOrDefaultAsync(c => c.rnc == datos.RncCliente);
            if (cliente == null)
            {
                cliente = new Clientes
                {
                    rnc = datos.RncCliente,
                    nombre = datos.NombreCliente,
                    telefono = datos.TelefonoCliente,
                    direccion = datos.DireccionCliente
                };
                context.Clientes.Add(cliente);
                await context.SaveChangesAsync();
            }
            else
            {
                // actualizar datos básicos
                cliente.nombre = datos.NombreCliente;
                cliente.telefono = datos.TelefonoCliente;
                cliente.direccion = datos.DireccionCliente;
                await context.SaveChangesAsync();
            }

            // Construir valores numéricos a partir del número ingresado (sin prefijo)
            var ncfSoloNumeros = new string((numeroSolo ?? string.Empty).Where(char.IsDigit).ToArray());
            var ncfNumerico = long.TryParse("0000" + ncfSoloNumeros, out long ncf) ? ncf : 0; // mantiene los 4 ceros
            var noFactura = int.TryParse(ncfSoloNumeros, out int noFact) ? noFact : 0;

            var montoRecibo = Math.Max(0m, recibo.Monto);

            // Calcular impuestos según tipo (Exento vs Gravado). ITBIS 18% si gravado
            var itbisRate = 0.18m;
            decimal exento = 0.00m, gravado = 0.00m, itbis = 0.00m;
            if (datos.EsGravada)
            {
                gravado = montoRecibo;
                itbis = Math.Round(gravado * itbisRate, 2);
                exento = 0.00m;
            }
            else
            {
                exento = montoRecibo;
                gravado = 0.00m;
                itbis = 0.00m;
            }

            var factura = new Factura
            {
                NoFactura = noFactura,
                Fecha = recibo.Fecha,
                IdCliente = cliente.idCliente,
                Exento = exento,
                Gravado = gravado,
                Itbis = itbis,
                APagar = exento + gravado + itbis,
                Pago = exento + gravado + itbis,
                Cambio = 0.00m,
                EsEfectivo = datos.MetodoPago == "Efectivo",
                EsCheque = datos.MetodoPago == "Cheque",
                EsCredito = datos.MetodoPago == "Credito",
                NumeroCheque = string.IsNullOrEmpty(datos.NumeroCheque) ? null : datos.NumeroCheque,
                Banco = string.IsNullOrEmpty(datos.Banco) ? null : datos.Banco,
                NCFNumerico = ncfNumerico,
                TCFNumerico = tipoComprobante == "B01" ? 1 : tipoComprobante == "B14" ? 14 : tipoComprobante == "B15" ? 15 : null,
                NulaTexto = "NO",
                FechaPago = recibo.Fecha,
                FechaVencimientoTexto = datos.ValidaHasta.ToString("dd/MM/yyyy")
            };

            // Validación mínima inline
            factura.Exento = factura.Exento == 0 ? 0.00m : factura.Exento;
            factura.Gravado = 0.00m;
            factura.Itbis = 0.00m;
            factura.APagar = factura.APagar == 0 ? 0.00m : factura.APagar;
            factura.Pago = factura.Pago ?? 0.00m;
            factura.Cambio = factura.Cambio ?? 0.00m;
            if (string.IsNullOrEmpty(factura.NulaTexto)) factura.NulaTexto = "NO";
            if (factura.Fecha == default) factura.Fecha = DateTime.Now;

            context.ChangeTracker.DetectChanges();
            context.Facturas.Add(factura);
            await context.SaveChangesAsync();

            await context.Entry(factura).Reference(f => f.Cliente).LoadAsync();
            return factura;
        }
        catch
        {
            throw;
        }
    }

    private async Task GenerarPdfDesdeFactura(Factura factura, string conceptoPersonalizado = null, string tipoComprobante = "B01")
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG] GenerarPdfDesdeFactura - INICIO");
        try
        {
            var numeroSolo = factura.NoFactura.ToString();
            var prefijo = tipoComprobante;
            if (string.IsNullOrEmpty(prefijo))
            {
                // reconstruir desde TCFNumerico
                prefijo = factura.TCFNumerico == 14 ? "B14" : factura.TCFNumerico == 15 ? "B15" : "B01";
            }
            var ncfCompleto = $"{prefijo}0000{numeroSolo}";

            var facturaNcf = new FacturaNcf
            {
                NCF = ncfCompleto,
                Fecha = factura.Fecha,
                ValidaHasta = factura.FechaVencimiento ?? DateTime.Now.AddMonths(1),
                RncCliente = factura.Cliente?.rnc ?? "",
                NombreCliente = factura.Cliente?.nombre?.ToUpper() ?? "CLIENTE SIN NOMBRE",
                TelefonoCliente = factura.Cliente?.telefono ?? "",
                DireccionCliente = factura.Cliente?.direccion?.ToUpper() ?? "DIRECCIÓN NO ESPECIFICADA",
                Concepto = conceptoPersonalizado ?? "DONATIVO PARA PACIENTES ONCOLOGICOS DE ESCASOS RECURSOS.",
                Monto = factura.APagar,
                Exento = factura.Exento,
                Gravado = factura.Gravado,
                Itbis = factura.Itbis,
                EsEfectivo = factura.EsEfectivo,
                EsCheque = factura.EsCheque,
                EsCredito = factura.EsCredito,
                NumeroCheque = factura.NumeroCheque,
                Banco = factura.Banco
            };

            // Generar PDF
            var pdfService = new FacturaNcfPdfService();
            await pdfService.AbrirFacturaPdfAsync(facturaNcf);
            
            System.Diagnostics.Debug.WriteLine("[DEBUG] PDF generado exitosamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG ERROR] Error generando PDF: {ex.Message}");
            throw;
        }
    }

    private async Task MostrarDialogoConfiguracionSecuencia()
    {
        var panelConfig = new StackPanel { Spacing = 16 };

        var txtInicio = new TextBox
        {
            Header = "Número de Inicio *",
            PlaceholderText = "Ejemplo: 500",
            InputScope = new InputScope
            {
                Names = { new InputScopeName { NameValue = InputScopeNameValue.Number } }
            }
        };

        var txtFin = new TextBox
        {
            Header = "Número Final *",
            PlaceholderText = "Ejemplo: 1000",
            InputScope = new InputScope
            {
                Names = { new InputScopeName { NameValue = InputScopeNameValue.Number } }
            }
        };

        // Mostrar estado actual
        var (activa, actual, inicio, fin, restantes) = _ncfSequenceService.ObtenerEstado();
        if (activa)
        {
            txtInicio.Text = inicio.ToString();
            txtFin.Text = fin.ToString();

            var infoActual = new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(30, 76, 175, 80)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var stackInfo = new StackPanel { Spacing = 4 };
            stackInfo.Children.Add(new TextBlock
            {
                Text = "?? Secuencia Actual",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkGreen)
            });
            stackInfo.Children.Add(new TextBlock
            {
                Text = $"Rango: {inicio} - {fin}",
                FontSize = 12
            });
            stackInfo.Children.Add(new TextBlock
            {
                Text = $"Siguiente número: {actual}",
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            stackInfo.Children.Add(new TextBlock
            {
                Text = $"Números restantes: {restantes}",
                FontSize = 12
            });

            infoActual.Child = stackInfo;
            panelConfig.Children.Add(infoActual);
        }

        var infoPanel = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(30, 33, 150, 243)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var infoStack = new StackPanel { Spacing = 4 };
        infoStack.Children.Add(new TextBlock
        {
            Text = "?? Información",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        infoStack.Children.Add(new TextBlock
        {
            Text = "Configure un rango de números NCF que se auto-incrementarán cada vez que use el botón 'Usar Secuencia'.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        });
        infoStack.Children.Add(new TextBlock
        {
            Text = "Ejemplo: Si configura del 500 al 1000, cada factura usará: 500, 501, 502... hasta 1000.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            FontStyle = Windows.UI.Text.FontStyle.Italic,
            Margin = new Thickness(0, 4, 0, 0)
        });

        infoPanel.Child = infoStack;
        panelConfig.Children.Add(infoPanel);

        panelConfig.Children.Add(txtInicio);
        panelConfig.Children.Add(txtFin);

        // Botones adicionales
        var botonesExtra = new StackPanel { Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };

        if (activa)
        {
            var btnReiniciar = new Button
            {
                Content = "?? Reiniciar Secuencia",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            btnReiniciar.Click += (s, e) =>
            {
                _ncfSequenceService.ReiniciarSecuencia();
                _ = ShowInfoDialog("Reinicio Exitoso", 
                    $"La secuencia ha sido reiniciada.\n\n" +
                    $"El próximo número será: {_ncfSequenceService.ObtenerNumeroActual()}");
            };

            var btnDesactivar = new Button
            {
                Content = "?? Desactivar Secuencia",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            btnDesactivar.Click += (s, e) =>
            {
                _ncfSequenceService.DesactivarSecuencia();
                _ = ShowInfoDialog("Desactivación Exitosa", "La secuencia ha sido desactivada.");
            };

            botonesExtra.Children.Add(btnReiniciar);
            botonesExtra.Children.Add(btnDesactivar);
            panelConfig.Children.Add(botonesExtra);
        }

        var dialog = new ContentDialog
        {
            Title = "?? Configurar Secuencia NCF",
            Content = new ScrollViewer
            {
                Content = panelConfig,
                MaxHeight = 500,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            },
            PrimaryButtonText = "?? Guardar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (!int.TryParse(txtInicio.Text, out int numeroInicio) || numeroInicio <= 0)
            {
                await ShowInfoDialog("Error", "Debe ingresar un número de inicio válido (mayor a 0)");
                return;
            }

            if (!int.TryParse(txtFin.Text, out int numeroFin) || numeroFin <= 0)
            {
                await ShowInfoDialog("Error", "Debe ingresar un número final válido (mayor a 0)");
                return;
            }

            if (numeroFin <= numeroInicio)
            {
                await ShowInfoDialog("Error", "El número final debe ser mayor al número de inicio");
                return;
            }

            try
            {
                _ncfSequenceService.ConfigurarSecuencia(numeroInicio, numeroFin);

                await ShowInfoDialog("? Configuración Exitosa",
                    $"La secuencia NCF ha sido configurada correctamente.\n\n" +
                    $"?? Rango: {numeroInicio} - {numeroFin}\n" +
                    $"?? Total de números: {numeroFin - numeroInicio + 1}\n" +
                    $"?? Próximo número: {numeroInicio}\n\n" +
                    $"Use el botón 'Usar Secuencia' para aplicar el siguiente número automáticamente.");
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al configurar la secuencia: {ex.Message}");
            }
        }
    }

    private void MostrarLoadingConMensaje(string mensaje, string submensaje = null)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (this.FindName("LoadingOverlay") is Grid loadingOverlay)
                loadingOverlay.Visibility = Visibility.Visible;
                
            if (this.FindName("LoadingIndicator") is ProgressRing loadingIndicator)
                loadingIndicator.IsActive = true;
                
            if (this.FindName("LoadingText") is TextBlock loadingText)
                loadingText.Text = mensaje;
                
            if (this.FindName("LoadingSubtext") is TextBlock loadingSubtext)
            {
                if (!string.IsNullOrEmpty(submensaje))
                {
                    loadingSubtext.Text = submensaje;
                    loadingSubtext.Visibility = Visibility.Visible;
                }
                else
                {
                    loadingSubtext.Visibility = Visibility.Collapsed;
                }
            }
        });
    }

    private void OcultarLoading()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (this.FindName("LoadingOverlay") is Grid loadingOverlay)
                loadingOverlay.Visibility = Visibility.Collapsed;
                
            if (this.FindName("LoadingIndicator") is ProgressRing loadingIndicator)
                loadingIndicator.IsActive = false;
        });
    }

    public void Dispose()
    {
        try
        {
            _isPageActive = false;
            _searchDelayTimer?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in Dispose: {ex.Message}");
        }
    }
}

