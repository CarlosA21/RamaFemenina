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
using RamaFemenina.Services;

namespace RamaFemenina;

public sealed partial class ReciboPage : Page, INotifyPropertyChanged
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DataCacheService _cacheService;
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
                    if (this.FindName("LoadingIndicator") is ProgressRing loadingIndicator)
                        loadingIndicator.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
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
        
        // Inicializar la colección
        RecibosCollection = new ObservableCollection<Recibo>();
        
        InitializeComponent();
        
        // Habilitar caché de navegación
        NavigationCacheMode = NavigationCacheMode.Enabled;
        
        var app = Application.Current as App;
        _serviceProvider = app!.Services;
        _cacheService = app.Services.GetRequiredService<DataCacheService>();
        
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
            // Verificar que la tabla existe
            bool tableExists = await VerifyTableExistsAsync();
            
            if (!tableExists)
            {
                if (EmptyState != null)
                    EmptyState.Visibility = Visibility.Visible;
                if (this.FindName("ListViewScroller") is UIElement listScrollerEmpty)
                    listScrollerEmpty.Visibility = Visibility.Collapsed;
                
                await ShowInfoDialog("Tabla No Encontrada", 
                    "La tabla 'Recibo' no existe en la base de datos.\n\n" +
                    "Por favor, ejecute el script 'FixDatabaseErrors.sql' para crear la tabla.");
                return;
            }

            var recibos = await _cacheService.GetRecibosPaginatedAsync(page, _pageSize, _currentSearchTerm);
            var totalCount = await _cacheService.GetRecibosTotalCountAsync(_currentSearchTerm);

            if (!_isPageActive) return;
            
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
            
            if (updateStats && _isPageActive)
            {
                _ = ActualizarEstadisticasAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar recibos: {ex.Message}");
        }
    }

    private async Task<bool> VerifyTableExistsAsync()
    {
        try
        {
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
            var recibosVirtuales = facturas.Select(f => new Recibo
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
                Cedula = f.Cliente?.rnc ?? ""
            }).ToList();
            
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
            var resultado = await MostrarDialogoRecibo(null);
            if (resultado != null)
            {
                // Normalizar flags de método de pago (garantizar un 0/1)
                if (!(resultado.EsEfectivo || resultado.EsTransferencia || resultado.EsCheque))
                {
                    resultado.EsEfectivo = true; // Por defecto efectivo
                }
                else
                {
                    // Asegurar exclusividad
                    resultado.EsEfectivo = resultado.EsEfectivo;
                    resultado.EsTransferencia = resultado.EsTransferencia && !resultado.EsEfectivo && !resultado.EsCheque;
                    resultado.EsCheque = resultado.EsCheque && !resultado.EsEfectivo && !resultado.EsTransferencia;
                }
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                    
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
                Title = "Confirmar Eliminación",
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
                    
                    var recibo = await context.Recibos.FindAsync(reciboSeleccionado.IdRecibo).ConfigureAwait(false);
                    if (recibo != null)
                    {
                        context.Recibos.Remove(recibo);
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        
                        _cacheService.InvalidateCache("recibos");
                        
                        // Recargar la página en el UI thread
                        await DispatcherQueue.EnqueueAsync(async () =>
                        {
                            await LoadPageAsync(CurrentPage);
                            
                            // Pequeño delay para asegurar que la carga se completó
                            await Task.Delay(100);
                            
                            await ShowInfoDialog("Éxito", "Recibo eliminado correctamente");
                        });
                    }
                }
                catch (Exception ex)
                {
                    await DispatcherQueue.EnqueueAsync(async () =>
                    {
                        await ShowInfoDialog("Error", $"Error al eliminar recibo: {ex.Message}");
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
            EsTransferencia = metodoPagoSeleccionado == "Transferencia",
            EsCheque = metodoPagoSeleccionado == "Cheque"
        };

        // Normalizar flags (garantizar un 0/1 y exclusividad)
        if (!(nuevoRecibo.EsEfectivo || nuevoRecibo.EsTransferencia || nuevoRecibo.EsCheque))
        {
            nuevoRecibo.EsEfectivo = true;
        }
        else
        {
            nuevoRecibo.EsTransferencia = nuevoRecibo.EsTransferencia && !nuevoRecibo.EsEfectivo && !nuevoRecibo.EsCheque;
            nuevoRecibo.EsCheque = nuevoRecibo.EsCheque && !nuevoRecibo.EsEfectivo && !nuevoRecibo.EsTransferencia;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
            
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
            PlaceholderText = "Se genera automáticamente",
            IsReadOnly = true
        };

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

        // Campos adicionales según método de pago
        var panelCheque = new StackPanel { Spacing = 8, Visibility = Visibility.Collapsed };
        var txtNumeroCheque = new TextBox { Header = "Número de Cheque", PlaceholderText = "000000" };
        var txtBanco = new TextBox { Header = "Banco", PlaceholderText = "Nombre del banco" };
        panelCheque.Children.Add(txtNumeroCheque);
        panelCheque.Children.Add(txtBanco);

        var panelTransferencia = new StackPanel { Spacing = 8, Visibility = Visibility.Collapsed };
        var txtNumeroFactura = new TextBox { Header = "Número de Referencia/Factura", PlaceholderText = "Referencia de transferencia / NCF" };
        panelTransferencia.Children.Add(txtNumeroFactura);

        cmbPago.SelectionChanged += (s, e) =>
        {
            var metodo = (cmbPago.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            panelCheque.Visibility = metodo == "Cheque" ? Visibility.Visible : Visibility.Collapsed;
            panelTransferencia.Visibility = metodo == "Transferencia" ? Visibility.Visible : Visibility.Collapsed;
        };
        // Inicializar visibilidad al cargar
        var metodoInicial = (cmbPago.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        panelCheque.Visibility = metodoInicial == "Cheque" ? Visibility.Visible : Visibility.Collapsed;
        panelTransferencia.Visibility = metodoInicial == "Transferencia" ? Visibility.Visible : Visibility.Collapsed;

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
            if (reciboExistente.EsCheque)
                cmbPago.SelectedIndex = 2;
            else if (reciboExistente.EsTransferencia)
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
        formPanel.Children.Add(panelTransferencia);
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
                // Campos adicionales seg?n m?todo de pago
                NumeroFacturaNCF = pagoSeleccionado == "Transferencia" ? txtNumeroFactura.Text.Trim() : null,
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
        
        if (_tipoDocumentoActual == TipoDocumento.Recibos)
        {
            await ImprimirReciboDirecto();
        }
        else
        {
            await ImprimirFacturaNcfDirecto(documentoSeleccionado);
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
            IsLoading = true;

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
            IsLoading = false;
            System.Diagnostics.Debug.WriteLine("[DEBUG] ImprimirReciboDirecto - FIN");
        }
    }

    private async Task<bool> ImprimirFacturaNcfDirecto(Recibo facturaVirtual)
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG] ImprimirFacturaNcfDirecto - INICIO");

        try
        {
            IsLoading = true;

            // Obtener la factura real de la base de datos usando el IdRecibo que mapea a IdFactura
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

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Generando PDF para Factura NCF: {facturaReal.NCF}");

            // Crear objeto FacturaNcf con datos reales
            var facturaNcf = new FacturaNcf
            {
                NCF = facturaReal.NCF ?? "Sin NCF",
                Fecha = facturaReal.Fecha,
                ValidaHasta = facturaReal.FechaVencimiento ?? DateTime.Now.AddMonths(1),
                RncCliente = facturaReal.Cliente?.rnc ?? "Sin RNC",
                NombreCliente = facturaReal.Cliente?.nombre?.ToUpper() ?? "CLIENTE SIN NOMBRE",
                TelefonoCliente = facturaReal.Cliente?.telefono ?? "",
                DireccionCliente = facturaReal.Cliente?.direccion?.ToUpper() ?? "DIRECCIÓN NO ESPECIFICADA",
                Concepto = facturaVirtual.Concepto ?? "DONATIVO PARA PACIENTES ONCOLOGICOS DE ESCASOS RECURSOS.",
                Monto = facturaReal.APagar,
                EsEfectivo = facturaReal.EsEfectivo,
                EsCheque = facturaReal.EsCheque,
                EsCredito = facturaReal.EsCredito,
                NumeroCheque = facturaReal.NumeroCheque,
                Banco = facturaReal.Banco
            };

            // Generar y abrir PDF usando FacturaNcfPdfService
            var pdfService = new FacturaNcfPdfService();
            await pdfService.AbrirFacturaPdfAsync(facturaNcf);

            System.Diagnostics.Debug.WriteLine("[DEBUG] Factura NCF PDF generado y abierto exitosamente");

            await ShowInfoDialog("Éxito",
                "Factura NCF generada correctamente.\n\n" +
                $"NCF: {facturaNcf.NCF}\n" +
                $"Cliente: {facturaNcf.NombreCliente}\n" +
                $"Monto: RD$ {facturaNcf.Monto:N2}\n\n" +
                "El PDF se ha abierto en el visor predeterminado.\n" +
                "Tamaño: Media Carta (8.5\" x 5.5\")\n\n" +
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
            IsLoading = false;
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

    private async Task MostrarDialogoFacturaNcf(Recibo reciboSeleccionado)
    {
        var formPanel = new StackPanel { Spacing = 12 };

        var txtNCF = new TextBox
        {
            Header = "NCF *",
            PlaceholderText = "B0100001899",
            MaxLength = 19
        };

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

        var txtMonto = new TextBox
        {
            Header = "Monto (RD$) *",
            Text = reciboSeleccionado.Monto.ToString("N2"),
            IsReadOnly = true
        };

        var cmbMetodoPago = new ComboBox
        {
            Header = "Método de Pago",
            MinWidth = 150
        };
        cmbMetodoPago.Items.Add(new ComboBoxItem { Content = "?? Efectivo", Tag = "Efectivo" });
        cmbMetodoPago.Items.Add(new ComboBoxItem { Content = "?? Cheque", Tag = "Cheque" });
        cmbMetodoPago.Items.Add(new ComboBoxItem { Content = "?? Crédito", Tag = "Credito" });
        
        if (reciboSeleccionado.EsEfectivo)
            cmbMetodoPago.SelectedIndex = 0;
        else if (reciboSeleccionado.EsCheque)
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

        formPanel.Children.Add(txtNCF);
        formPanel.Children.Add(dpValidaHasta);
        formPanel.Children.Add(txtRncCliente);
        formPanel.Children.Add(txtNombreCliente);
        formPanel.Children.Add(txtTelefono);
        formPanel.Children.Add(txtDireccion);
        formPanel.Children.Add(txtConcepto);
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
            if (string.IsNullOrWhiteSpace(txtNCF.Text))
            {
                await ShowInfoDialog("Error", "Debe ingresar el NCF");
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

                // Preparar datos de la factura NCF
                var datosFacturaNcf = new DatosFacturaNcf
                {
                    NCF = txtNCF.Text.Trim(),
                    ValidaHasta = dpValidaHasta.Date?.DateTime ?? DateTime.Now.AddMonths(1),
                    RncCliente = txtRncCliente.Text.Trim(),
                    NombreCliente = txtNombreCliente.Text.Trim().ToUpper(),
                    TelefonoCliente = txtTelefono.Text.Trim(),
                    DireccionCliente = txtDireccion.Text.Trim().ToUpper(),
                    Concepto = txtConcepto.Text.Trim().ToUpper(),
                    MetodoPago = metodoPago,
                    NumeroCheque = metodoPago == "Cheque" ? txtNumeroCheque.Text.Trim() : null,
                    Banco = metodoPago == "Cheque" ? txtBanco.Text.Trim() : null
                };

                // 1. CREAR Y GUARDAR FACTURA EN BASE DE DATOS
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Creando factura NCF a partir del recibo {reciboSeleccionado.NumeroRecibo}");
                
                var facturaCreada = await CrearFacturaEnBaseDatos(reciboSeleccionado, datosFacturaNcf);

                // 2. GENERAR PDF DESDE LA FACTURA GUARDADA
                await GenerarPdfDesdeFactura(facturaCreada, datosFacturaNcf.Concepto);

                // 3. ACTUALIZAR VISTA SI ESTAMOS EN MODO FACTURAS
                if (_tipoDocumentoActual == TipoDocumento.Facturas)
                {
                    await CargarFacturas();
                }

                await ShowInfoDialog("? Éxito",
                    "Factura NCF creada y guardada correctamente.\n\n" +
                    $"?? ID Factura (PK): {facturaCreada.IdFactura}\n" +
                    $"?? No.Factura = NCF: {facturaCreada.NoFactura}\n" +
                    $"?? Cliente: {facturaCreada.Cliente?.nombre}\n" +
                    $"?? Monto: RD$ {facturaCreada.APagar:N2}\n\n" +
                    "? Factura guardada en base de datos\n" +
                    "?? PDF generado y abierto automáticamente\n" +
                    "?? Ahora disponible en lista de Facturas NCF\n\n" +
                    "?? NCF y NoFactura contienen la misma información");
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

    private async Task<Factura> CrearFacturaEnBaseDatos(Recibo recibo, DatosFacturaNcf datos)
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG] CrearFacturaEnBaseDatos - INICIO");
        
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
        
        try
        {
            // 1. Obtener o crear cliente
            var cliente = await ObtainOrCreateClient(context, datos);
            
            // 2. Extraer solo números del NCF para NCFNumerico y NoFactura
            var ncfSoloNumeros = string.Concat(datos.NCF.Where(char.IsDigit));
            var ncfNumerico = long.TryParse(ncfSoloNumeros, out long ncf) ? ncf : 0;
            var noFactura = int.TryParse(ncfSoloNumeros, out int noFact) ? noFact : 0;
            
            // 2.1 Validar y limpiar valores monetarios
            var montoRecibo = Math.Max(0m, recibo.Monto);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Monto original del recibo: {recibo.Monto}, Monto procesado: {montoRecibo}");
            
            // 3. Crear la factura con valores explícitos ABSOLUTAMENTE SEGUROS
            var factura = new Factura();
            
            // Asignar cada campo individualmente con valores garantizados
            factura.NoFactura = noFactura;
            factura.Fecha = recibo.Fecha;
            factura.IdCliente = cliente.idCliente;
            
            // CRÍTICO: Campos decimales con valores absolutamente explícitos
            factura.Exento = decimal.Parse(montoRecibo.ToString("F2"));        // Forzar formato correcto
            factura.Gravado = decimal.Parse("0.00");                          // Absolutamente explícito
            factura.Itbis = decimal.Parse("0.00");                            // Absolutamente explícito  
            factura.APagar = decimal.Parse(montoRecibo.ToString("F2"));        // Forzar formato correcto
            factura.Pago = decimal.Parse(montoRecibo.ToString("F2"));          // Forzar formato correcto
            factura.Cambio = decimal.Parse("0.00");                           // Absolutamente explícito
            
            // Método de pago
            factura.EsEfectivo = datos.MetodoPago == "Efectivo";
            factura.EsCheque = datos.MetodoPago == "Cheque"; 
            factura.EsCredito = datos.MetodoPago == "Credito";
            factura.NumeroCheque = string.IsNullOrEmpty(datos.NumeroCheque) ? null : datos.NumeroCheque;
            factura.Banco = string.IsNullOrEmpty(datos.Banco) ? null : datos.Banco;
            
            // NCF
            factura.NCFNumerico = ncfNumerico;
            
            // Estado
            factura.NulaTexto = "NO";
            factura.FechaPago = recibo.Fecha;
            factura.FechaVencimientoTexto = datos.ValidaHasta.ToString("dd/MM/yyyy");

            // 4. VALIDACIÓN FINAL: Asegurar que no hay valores NULL
            ValidarFacturaAntesDeGuardar(factura);

            // Debug: Verificar valores antes de guardar
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Valores antes de guardar:");
            System.Diagnostics.Debug.WriteLine($"  NoFactura: {factura.NoFactura}");
            System.Diagnostics.Debug.WriteLine($"  Exento: {factura.Exento}");
            System.Diagnostics.Debug.WriteLine($"  Gravado: {factura.Gravado}");
            System.Diagnostics.Debug.WriteLine($"  Itbis: {factura.Itbis}");
            System.Diagnostics.Debug.WriteLine($"  APagar: {factura.APagar}");
            System.Diagnostics.Debug.WriteLine($"  Pago: {factura.Pago}");
            System.Diagnostics.Debug.WriteLine($"  Cambio: {factura.Cambio}");
            System.Diagnostics.Debug.WriteLine($"  NCFNumerico: {factura.NCFNumerico}");

            // 5. Guardar en la base de datos con múltiples estrategias
            try
            {
                // ESTRATEGIA 1: EF Core normal
                System.Diagnostics.Debug.WriteLine("[DEBUG] Intentando guardar con EF Core...");
                
                // Habilitar detección de cambios antes de guardar
                context.ChangeTracker.DetectChanges();
                
                // Verificar el estado de la entidad
                var entry = context.Entry(factura);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Estado de entidad antes de Add: {entry.State}");
                
                context.Facturas.Add(factura);
                
                // Verificar estado después de Add
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Estado de entidad después de Add: {entry.State}");
                
                // Verificar valores que se van a insertar
                foreach (var property in entry.Properties)
                {
                    if (property.CurrentValue == null && !property.Metadata.IsNullable)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG WARNING] Propiedad {property.Metadata.Name} es NULL pero no nullable!");
                    }
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] {property.Metadata.Name}: {property.CurrentValue}");
                }
                
                await context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ? Factura guardada exitosamente con ID: {factura.IdFactura}");
            }
            catch (Exception efError)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG ERROR] ? EF Core falló: {efError.Message}");
                
                // ESTRATEGIA 2: SQL directo como alternativa
                System.Diagnostics.Debug.WriteLine("[DEBUG] Intentando con SQL directo...");
                
                try
                {
                    var sqlQuery = @"
                        INSERT INTO factura (
                            nofactura, fecha, idcliente, exento, gravado, itbis, apagar,
                            cred, efec, cheq, cheque, banco, fechapago, pago, ncf, cambio, nula, fechav2
                        ) VALUES (
                            @NoFactura, @Fecha, @IdCliente, @Exento, @Gravado, @Itbis, @APagar,
                            @EsCredito, @EsEfectivo, @EsCheque, @NumeroCheque, @Banco, @FechaPago, 
                            @Pago, @NCFNumerico, @Cambio, @NulaTexto, @FechaVencimientoTexto
                        );
                        SELECT CAST(SCOPE_IDENTITY() as int);";
                    
                    var parametros = new[]
                    {
                        new Microsoft.Data.SqlClient.SqlParameter("@NoFactura", factura.NoFactura),
                        new Microsoft.Data.SqlClient.SqlParameter("@Fecha", factura.Fecha),
                        new Microsoft.Data.SqlClient.SqlParameter("@IdCliente", (object)factura.IdCliente ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@Exento", factura.Exento),
                        new Microsoft.Data.SqlClient.SqlParameter("@Gravado", factura.Gravado),
                        new Microsoft.Data.SqlClient.SqlParameter("@Itbis", factura.Itbis),
                        new Microsoft.Data.SqlClient.SqlParameter("@APagar", factura.APagar),
                        new Microsoft.Data.SqlClient.SqlParameter("@EsCredito", factura.EsCredito),
                        new Microsoft.Data.SqlClient.SqlParameter("@EsEfectivo", factura.EsEfectivo),
                        new Microsoft.Data.SqlClient.SqlParameter("@EsCheque", factura.EsCheque),
                        new Microsoft.Data.SqlClient.SqlParameter("@NumeroCheque", (object)factura.NumeroCheque ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@Banco", (object)factura.Banco ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@FechaPago", (object)factura.FechaPago ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@Pago", (object)factura.Pago ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@NCFNumerico", (object)factura.NCFNumerico ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@Cambio", (object)factura.Cambio ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@NulaTexto", factura.NulaTexto ?? "NO"),
                        new Microsoft.Data.SqlClient.SqlParameter("@FechaVencimientoTexto", (object)factura.FechaVencimientoTexto ?? DBNull.Value)
                    };
                    
                    // Ejecutar el INSERT y obtener el ID
                    using var connection = context.Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                        await connection.OpenAsync();
                    
                    using var command = connection.CreateCommand();
                    command.CommandText = sqlQuery;
                    command.Parameters.AddRange(parametros);
                    
                    var result = await command.ExecuteScalarAsync();
                    factura.IdFactura = Convert.ToInt32(result);
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ? SQL directo exitoso - ID: {factura.IdFactura}");
                }
                catch (Exception sqlError)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG ERROR] ? SQL directo también falló: {sqlError.Message}");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG ERROR] InnerException: {sqlError.InnerException?.Message}");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG ERROR] StackTrace: {sqlError.StackTrace}");
                    throw new Exception($"Error al guardar factura tanto con EF como con SQL directo. EF Error: {efError.Message}, SQL Error: {sqlError.Message}", sqlError);
                }
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Factura creada - IdFactura (PK): {factura.IdFactura}, NoFactura=NCF: {factura.NoFactura}, NCF Original: {datos.NCF}");
            
            // 6. Recargar con cliente para tener datos completos
            await context.Entry(factura).Reference(f => f.Cliente).LoadAsync();
            
            return factura;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG ERROR] Error creando factura: {ex.Message}");
            throw;
        }
    }

    private async Task<Clientes> ObtainOrCreateClient(RamaFemeninaContext context, DatosFacturaNcf datos)
    {
        // Buscar cliente existente por RNC
        var clienteExistente = await context.Clientes
            .FirstOrDefaultAsync(c => c.rnc == datos.RncCliente);

        if (clienteExistente != null)
        {
            // Actualizar datos si es necesario
            clienteExistente.nombre = datos.NombreCliente;
            clienteExistente.telefono = datos.TelefonoCliente;
            clienteExistente.direccion = datos.DireccionCliente;
            await context.SaveChangesAsync();
            return clienteExistente;
        }

        // Crear nuevo cliente
        var nuevoCliente = new Clientes
        {
            rnc = datos.RncCliente,
            nombre = datos.NombreCliente,
            telefono = datos.TelefonoCliente,
            direccion = datos.DireccionCliente
        };

        context.Clientes.Add(nuevoCliente);
        await context.SaveChangesAsync();
        return nuevoCliente;
    }

    private void ValidarFacturaAntesDeGuardar(Factura factura)
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG] ValidarFacturaAntesDeGuardar - INICIO");
        
        // Validar y corregir campos decimales críticos
        if (factura.Exento == null) factura.Exento = 0.00m;
        if (factura.Gravado == null) factura.Gravado = 0.00m;
        if (factura.Itbis == null) factura.Itbis = 0.00m;
        if (factura.APagar == null) factura.APagar = 0.00m;
        if (factura.Pago == null) factura.Pago = 0.00m;
        if (factura.Cambio == null) factura.Cambio = 0.00m;
        
        // Validar campos de texto
        if (string.IsNullOrEmpty(factura.NulaTexto)) factura.NulaTexto = "NO";
        
        // Validar fecha
        if (factura.Fecha == default(DateTime)) factura.Fecha = DateTime.Now;
        
        System.Diagnostics.Debug.WriteLine("[DEBUG] ValidarFacturaAntesDeGuardar - Validación completada");
    }

    private async Task GenerarPdfDesdeFactura(Factura factura, string conceptoPersonalizado = null)
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG] GenerarPdfDesdeFactura - INICIO");
        
        try
        {
            // Crear objeto FacturaNcf para el PDF
            var facturaNcf = new FacturaNcf
            {
                NCF = factura.NCF ?? factura.NoFactura.ToString(), // NCF y NoFactura tienen la misma información
                Fecha = factura.Fecha,
                ValidaHasta = factura.FechaVencimiento ?? DateTime.Now.AddMonths(1),
                RncCliente = factura.Cliente?.rnc ?? "",
                NombreCliente = factura.Cliente?.nombre?.ToUpper() ?? "CLIENTE SIN NOMBRE",
                TelefonoCliente = factura.Cliente?.telefono ?? "",
                DireccionCliente = factura.Cliente?.direccion?.ToUpper() ?? "DIRECCIÓN NO ESPECIFICADA",
                Concepto = conceptoPersonalizado ?? "DONATIVO PARA PACIENTES ONCOLOGICOS DE ESCASOS RECURSOS.",
                Monto = factura.APagar,
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
