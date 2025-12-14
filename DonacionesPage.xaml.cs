using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace RamaFemenina;

public sealed partial class DonacionesPage : Page, INotifyPropertyChanged
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DataCacheService _cacheService;
    private bool _isDonacionSelected;
    private bool _isLoading;
    private Timer _searchDelayTimer;
    private bool _isPageActive = true;
    
    // Propiedades de paginación
    private int _currentPage = 1;
    private int _pageSize = 50;
    private int _totalCount = 0;
    private string _currentSearchTerm = "";
    
    public bool IsDonacionSelected
    {
        get => _isDonacionSelected;
        set
        {
            if (_isDonacionSelected != value)
            {
                _isDonacionSelected = value;
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
                // Spinner/overlay removidos por solicitud; mantener UI responsiva sin overlay
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

    public ObservableCollection<Donaciones> DonacionesCollection { get; set; }
    public ObservableCollection<Paciente> Pacientes { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public DonacionesPage()
    {
        System.Diagnostics.Debug.WriteLine($"[PAGE-CTOR] Constructor DonacionesPage iniciado");
        
        InitializeComponent();
        
        // Habilitar caché de navegación
        NavigationCacheMode = NavigationCacheMode.Enabled;
        System.Diagnostics.Debug.WriteLine($"[PAGE-CTOR] NavigationCacheMode establecido");
        
        var app = Application.Current as App;
        _serviceProvider = app!.Services;
        _cacheService = app.Services.GetRequiredService<DataCacheService>();
        System.Diagnostics.Debug.WriteLine($"[PAGE-CTOR] ServiceProvider y CacheService obtenidos");
        
        DonacionesCollection = new ObservableCollection<Donaciones>();
        Pacientes = new ObservableCollection<Paciente>();
        System.Diagnostics.Debug.WriteLine($"[PAGE-CTOR] ObservableCollections creadas");
        
        // Inicialización de timer para búsqueda con delay
        _searchDelayTimer = new Timer(PerformSearch, null, Timeout.Infinite, Timeout.Infinite);
        System.Diagnostics.Debug.WriteLine($"[PAGE-CTOR] Timer de búsqueda creado");
        
        // La carga inicial se maneja en OnNavigatedTo
        
        // Iniciar animación de entrada
        this.Loaded += (s, e) => 
        {
            try 
            {
                System.Diagnostics.Debug.WriteLine($"[PAGE-LOADED] Evento Loaded disparado");
                if (this.FindName("FadeInStoryboard") is Storyboard storyboard)
                {
                    storyboard.Begin();
                    System.Diagnostics.Debug.WriteLine($"[PAGE-LOADED] Storyboard iniciado");
                }
            } 
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PAGE-LOADED] Error en animación: {ex.Message}");
            }
        };
        
        System.Diagnostics.Debug.WriteLine($"[PAGE-CTOR] Constructor DonacionesPage completado");
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _isPageActive = true;
        
        // Cargar pacientes primero
        await LoadPacientesAsync();
        
        // Cargar datos solo si es necesario
        if (e.Parameter?.ToString() == "Reload" || DonacionesCollection.Count == 0)
        {
            await LoadPageAsync(1);
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
            System.Diagnostics.Debug.WriteLine($"[DONACIONES] LoadPageAsync iniciado - Page: {page}");
            IsLoading = true;
            // Ejecutar consultas en paralelo para reducir tiempo total
            var donacionesTask = _cacheService.GetDonacionesPaginatedAsync(page, _pageSize, _currentSearchTerm);
            var totalCountTask = _cacheService.GetDonacionesTotalCountAsync(_currentSearchTerm);
            await Task.WhenAll(donacionesTask, totalCountTask);
            var donaciones = donacionesTask.Result;
            var totalCount = totalCountTask.Result;

            System.Diagnostics.Debug.WriteLine($"[DONACIONES] Datos obtenidos - Donaciones: {donaciones?.Count() ?? 0}, Total: {totalCount}");

            if (!_isPageActive) return;

            DonacionesCollection.Clear();
            foreach (var donacion in donaciones)
            {
                DonacionesCollection.Add(donacion);
            }

            CurrentPage = page;
            TotalCount = totalCount;

            System.Diagnostics.Debug.WriteLine($"[DONACIONES] Colección actualizada - Count: {DonacionesCollection.Count}");

            // Actualizar controles de UI
            if (DonacionesListView != null)
                DonacionesListView.ItemsSource = DonacionesCollection;
            
            var hayDonaciones = DonacionesCollection.Count > 0;
            if (this.FindName("ListViewScroller") is UIElement listScroller)
                listScroller.Visibility = hayDonaciones ? Visibility.Visible : Visibility.Collapsed;
            if (EmptyState != null)
                EmptyState.Visibility = hayDonaciones ? Visibility.Collapsed : Visibility.Visible;
            
            UpdatePaginationControls();
            
            if (updateStats && _isPageActive)
            {
                _ = ActualizarEstadisticasAsync();
            }

            System.Diagnostics.Debug.WriteLine($"[DONACIONES] LoadPageAsync completado exitosamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DONACIONES] Error al cargar donaciones: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[DONACIONES] StackTrace: {ex.StackTrace}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPacientesAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[DONACIONES] LoadPacientesAsync iniciado");
            
            if (!_isPageActive) return;
            
            // Cargar TODOS los pacientes para el dropdown (sin límite de paginación)
            // Usamos un pageSize muy grande para obtener todos los registros
            var pacientes = await _cacheService.GetPacientesPaginatedAsync(1, 10000, "", CancellationToken.None);
            
            System.Diagnostics.Debug.WriteLine($"[DONACIONES] Pacientes obtenidos: {pacientes?.Count() ?? 0}");
            
            if (!_isPageActive) return;
            
            Pacientes.Clear();
            foreach (var paciente in pacientes.OrderBy(p => p.nombre))
            {
                Pacientes.Add(paciente);
            }
            
            System.Diagnostics.Debug.WriteLine($"[DONACIONES] Pacientes cargados en colección: {Pacientes.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DONACIONES] Error al cargar pacientes: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[DONACIONES] StackTrace: {ex.StackTrace}");
        }
    }

    private async Task ActualizarEstadisticasAsync()
    {
        try
        {
            if (!_isPageActive) return;
            
            // Usar método optimizado del cache service para estadísticas
            var stats = await _cacheService.GetDonacionesStatsAsync(CancellationToken.None);

            // Actualizar en UI Thread
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                try
                {
                    if (!_isPageActive) return;
                    
                    // Total de donaciones
                    if (this.FindName("txtTotalDonaciones") is TextBlock totalText)
                        totalText.Text = stats.TotalDonaciones.ToString();
                        
                    if (this.FindName("txtContador") is Microsoft.UI.Xaml.Documents.Run contadorRun)
                        contadorRun.Text = TotalCount.ToString();
                    
                    if (this.FindName("txtTotalSolicitado") is TextBlock solicitadoText)
                        solicitadoText.Text = $"RD$ {stats.TotalSolicitado:N2}";
                    
                    if (this.FindName("txtTotalDonado") is TextBlock donadoText)
                        donadoText.Text = $"RD$ {stats.TotalDonado:N2}";
                    
                    if (this.FindName("txtDiferencia") is TextBlock diferenciaText)
                        diferenciaText.Text = $"RD$ {stats.Diferencia:N2}";
                    
                    if (this.FindName("txtPorcentaje") is TextBlock porcentajeText)
                        porcentajeText.Text = $"{stats.PorcentajeCompletado:F1}% completado";
                    
                    if (this.FindName("progressSolicitado") is ProgressBar progress)
                        progress.Value = Math.Min(Convert.ToDouble(stats.PorcentajeCompletado), 100);
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
            _cacheService.InvalidateCache("donaciones");
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
        _cacheService.InvalidateCache("donaciones");
        await LoadPageAsync(CurrentPage);
    }

    private async void BtnNuevaDonacion_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Cargar pacientes bajo demanda para reducir tiempo de carga de la página
            if (Pacientes.Count == 0)
            {
                await LoadPacientesAsync();
                if (Pacientes.Count == 0)
                {
                    await ShowInfoDialog("Advertencia", "No hay pacientes registrados. Por favor, registre un paciente primero.");
                    return;
                }
            }

            var resultado = await MostrarDialogoDonacion(null);
            if (resultado != null)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                    
                    context.Donaciones.Add(resultado);
                    await context.SaveChangesAsync();

                    // Invalidar cache y recargar
                    _cacheService.InvalidateCache("donaciones");
                    await LoadPageAsync(CurrentPage, true);
                    
                    await ShowInfoDialog("Éxito", $"Donación registrada correctamente.\nID: {resultado.idDonacion}\nTotal: RD$ {resultado.total:N2}");
                }
                catch (Exception ex)
                {
                    await ShowInfoDialog("Error", $"Error al guardar donación: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error inesperado: {ex.Message}");
            await ShowInfoDialog("Error", $"Error inesperado: {ex.Message}");
        }
    }

    private async void BtnEditarDonacion_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Obtener la donación del contexto del botón
            if (sender is Button button && button.Tag != null)
            {
                var idDonacion = Convert.ToInt32(button.Tag);
                var donacionSeleccionada = DonacionesCollection.FirstOrDefault(d => d.idDonacion == idDonacion);
                
                if (donacionSeleccionada == null)
                {
                    await ShowInfoDialog("Error", "No se encontró la donación seleccionada");
                    return;
                }

                var resultado = await MostrarDialogoDonacion(donacionSeleccionada);
                if (resultado != null)
                {
                    try
                    {
                        // Usar una instancia separada del contexto
                        using var scope = _serviceProvider.CreateScope();
                        using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                        
                        var donacion = await context.Donaciones.FindAsync(donacionSeleccionada.idDonacion);
                        if (donacion != null)
                        {
                            donacion.Fecha = resultado.Fecha;
                            donacion.idPaciente = resultado.idPaciente;
                            donacion.procedimiento = resultado.procedimiento;
                            donacion.observacion = resultado.observacion;
                            donacion.montoSolicitado = resultado.montoSolicitado;
                            donacion.valor = resultado.valor;
                            donacion.total = resultado.total;

                            // Marcar propiedades como modificadas explícitamente
                            var entry = context.Entry(donacion);
                            entry.Property(e => e.Fecha).IsModified = true;
                            entry.Property(e => e.idPaciente).IsModified = true;
                            entry.Property(e => e.procedimiento).IsModified = true;
                            entry.Property(e => e.observacion).IsModified = true;
                            entry.Property(e => e.montoSolicitado).IsModified = true;
                            entry.Property(e => e.valor).IsModified = true;
                            entry.Property(e => e.total).IsModified = true;

                            await context.SaveChangesAsync();
                            
                            // Invalidar cache y recargar
                            _cacheService.InvalidateCache("donaciones");
                            await LoadPageAsync(CurrentPage);
                            
                            await DispatcherQueue.EnqueueAsync(async () =>
                            {
                                await ShowInfoDialog("Éxito", "Donación actualizada correctamente");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        await DispatcherQueue.EnqueueAsync(async () =>
                        {
                            await ShowInfoDialog("Error", $"Error al actualizar donación: {ex.Message}");
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BTN-EDITAR] ERROR: {ex.Message}");
            await ShowInfoDialog("Error", $"Error inesperado: {ex.Message}");
        }
    }

    private async void BtnEliminarDonacion_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Obtener la donación del contexto del botón
            if (sender is Button button && button.Tag != null)
            {
                var idDonacion = Convert.ToInt32(button.Tag);
                var donacionSeleccionada = DonacionesCollection.FirstOrDefault(d => d.idDonacion == idDonacion);
                
                if (donacionSeleccionada == null)
                {
                    await ShowInfoDialog("Error", "No se encontró la donación seleccionada");
                    return;
                }

                var nombrePaciente = donacionSeleccionada.Paciente?.nombre ?? "Paciente no encontrado";
                
                var confirmDialog = new ContentDialog
                {
                    Title = "Confirmar Eliminación",
                    Content = $"¿Está seguro que desea eliminar esta donación?\n\n" +
                              $"ID Donación: {donacionSeleccionada.idDonacion}\n" +
                              $"Paciente: {nombrePaciente}\n" +
                              $"Procedimiento: {donacionSeleccionada.procedimiento}\n" +
                              $"Monto: RD$ {donacionSeleccionada.total:N2}\n\n" +
                              $"Esta acción no se puede deshacer.",
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
                        // Usar una instancia separada del contexto
                        using var scope = _serviceProvider.CreateScope();
                        using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                        
                        var donacion = await context.Donaciones.FindAsync(donacionSeleccionada.idDonacion);
                        if (donacion != null)
                        {
                            context.Donaciones.Remove(donacion);
                            await context.SaveChangesAsync();
                            
                            // Invalidar cache y recargar
                            _cacheService.InvalidateCache("donaciones");
                            await LoadPageAsync(CurrentPage);
                            
                            await DispatcherQueue.EnqueueAsync(async () =>
                            {
                                await ShowInfoDialog("Éxito", "Donación eliminada correctamente");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        await DispatcherQueue.EnqueueAsync(async () =>
                        {
                            await ShowInfoDialog("Error", $"Error al eliminar donación: {ex.Message}");
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

    private async void BtnVerPaciente_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Obtener la donación del contexto del botón
            if (sender is Button button && button.Tag != null)
            {
                var idDonacion = Convert.ToInt32(button.Tag);
                var donacionSeleccionada = DonacionesCollection.FirstOrDefault(d => d.idDonacion == idDonacion);
                
                if (donacionSeleccionada == null)
                {
                    await ShowInfoDialog("Error", "No se encontró la donación seleccionada");
                    return;
                }

                try
                {
                    // Usar una instancia separada del contexto
                    using var scope = _serviceProvider.CreateScope();
                    using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                    
                    var paciente = await context.Pacientes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.idpaciente == donacionSeleccionada.idPaciente);

                    if (paciente != null)
                    {
                        var donacionesPaciente = await context.Donaciones
                            .AsNoTracking()
                            .Where(d => d.idPaciente == paciente.idpaciente)
                            .ToListAsync();

                        var totalDonado = donacionesPaciente.Sum(d => d.total);
                        var totalSolicitado = donacionesPaciente.Sum(d => d.montoSolicitado);

                        await DispatcherQueue.EnqueueAsync(async () =>
                        {
                            await ShowInfoDialog("Información del Paciente",
                                $"ID: {paciente.idpaciente}\n" +
                                $"Cédula: {paciente.cedula}\n" +
                                $"Nombre: {paciente.nombre}\n" +
                                $"Teléfono: {paciente.telefono ?? "N/A"}\n" +
                                $"Celular: {paciente.celular ?? "N/A"}\n" +
                                $"Estado: {paciente.estado ?? "N/A"}\n" +
                                $"Área: {paciente.area ?? "N/A"}\n\n" +
                                $"DONACIONES:\n" +
                                $"Total de donaciones: {donacionesPaciente.Count}\n" +
                                $"Monto solicitado: RD$ {totalSolicitado:N2}\n" +
                                $"Monto donado: RD$ {totalDonado:N2}");
                        });
                    }
                    else
                    {
                        await ShowInfoDialog("Error", "No se encontró el paciente asociado");
                    }
                }
                catch (Exception ex)
                {
                    await DispatcherQueue.EnqueueAsync(async () =>
                    {
                        await ShowInfoDialog("Error", $"Error al cargar información del paciente: {ex.Message}");
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BTN-VER-PACIENTE] ERROR: {ex.Message}");
            await ShowInfoDialog("Error", $"Error inesperado: {ex.Message}");
        }
    }

    private async Task<Donaciones> MostrarDialogoDonacion(Donaciones donacionExistente)
    {
        System.Diagnostics.Debug.WriteLine($"[DIALOG] MostrarDialogoDonacion iniciado - Es edición: {donacionExistente != null}");
        
        bool esEdicion = donacionExistente != null;

        // Lazy load de pacientes si aún no están cargados
        if (Pacientes.Count == 0)
        {
            await LoadPacientesAsync();
        }

        var pacienteCombo = new ComboBox
        {
            Header = "Paciente *",
            PlaceholderText = "Seleccione un paciente",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            DisplayMemberPath = "nombre",
            SelectedValuePath = "idpaciente"
        };

        System.Diagnostics.Debug.WriteLine($"[DIALOG] ComboBox creado. Pacientes disponibles: {Pacientes.Count}");

        // Usar la colección de pacientes directamente
        pacienteCombo.ItemsSource = Pacientes;

        if (esEdicion)
        {
            System.Diagnostics.Debug.WriteLine($"[DIALOG] Buscando paciente con ID: {donacionExistente.idPaciente}");
            pacienteCombo.SelectedValue = donacionExistente.idPaciente;
            System.Diagnostics.Debug.WriteLine($"[DIALOG] Paciente seleccionado: {pacienteCombo.SelectedValue != null}");
        }

        var fechaPicker = new CalendarDatePicker
        {
            Header = "Fecha de Donación *",
            Date = donacionExistente?.Fecha != null ? new DateTimeOffset(donacionExistente.Fecha) : DateTimeOffset.Now,
            MaxDate = DateTimeOffset.Now.AddYears(1)
        };

        var procedimientoBox = new TextBox
        {
            Header = "Procedimiento Médico *",
            PlaceholderText = "Descripción del procedimiento",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            Text = donacionExistente?.procedimiento ?? ""
        };

        var montoSolicitadoBox = new NumberBox
        {
            Header = "Monto Solicitado (RD$) *",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0,
            SmallChange = 100.0,
            LargeChange = 1000.0,
            Value = donacionExistente?.montoSolicitado != null ? (double)donacionExistente.montoSolicitado : 0
        };

        var valorDonacionBox = new NumberBox
        {
            Header = "Valor de la Donación (RD$)",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0,
            SmallChange = 100.0,
            LargeChange = 1000.0,
            Value = donacionExistente?.valor != null ? (double)donacionExistente.valor : 0
        };

        var totalBox = new NumberBox
        {
            Header = "Total (RD$)",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0,
            IsEnabled = false,
            Value = donacionExistente?.total != null ? (double)donacionExistente.total : 0
        };

        var porcentajeText = new TextBlock
        {
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var progressBar = new ProgressBar
        {
            Margin = new Thickness(0, 8, 0, 0),
            Height = 20
        };

        void ActualizarTotal()
        {
            // Proteger contra NaN
            var valor = double.IsNaN(valorDonacionBox.Value) ? 0 : Math.Max(0, valorDonacionBox.Value);
            var solicitado = double.IsNaN(montoSolicitadoBox.Value) ? 0 : Math.Max(0, montoSolicitadoBox.Value);
            totalBox.Value = valor;
            
            if (solicitado > 0)
            {
                var porcentaje = (valor / solicitado) * 100;
                porcentajeText.Text = $"Porcentaje completado: {porcentaje:F1}%";
                progressBar.Value = Math.Min(porcentaje, 100);
                
                if (porcentaje >= 100)
                    porcentajeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                else if (porcentaje > 0)
                    porcentajeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange);
                else
                    porcentajeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            else
            {
                porcentajeText.Text = "Porcentaje completado: 0%";
                progressBar.Value = 0;
            }
        }

        valorDonacionBox.ValueChanged += (s, args) => ActualizarTotal();
        montoSolicitadoBox.ValueChanged += (s, args) => ActualizarTotal();

        ActualizarTotal();

        var observacionBox = new TextBox
        {
            Header = "Observaciones",
            PlaceholderText = "Notas adicionales (opcional)",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            Text = donacionExistente?.observacion ?? ""
        };

        var formPanel = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                pacienteCombo,
                fechaPicker,
                procedimientoBox,
                montoSolicitadoBox,
                valorDonacionBox,
                totalBox,
                porcentajeText,
                progressBar,
                observacionBox
            }
        };

        var scrollViewer = new ScrollViewer
        {
            Content = formPanel,
            MaxHeight = 600,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var dialog = new ContentDialog
        {
            Title = esEdicion ? "Editar Donación" : "Nueva Donación",
            Content = scrollViewer,
            PrimaryButtonText = esEdicion ? "Actualizar" : "Guardar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        System.Diagnostics.Debug.WriteLine($"[DIALOG] Mostrando diálogo...");
        var result = await dialog.ShowAsync();
        System.Diagnostics.Debug.WriteLine($"[DIALOG] Resultado del diálogo: {result}");
        
        if (result == ContentDialogResult.Primary)
        {
            if (pacienteCombo.SelectedValue == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DIALOG] ERROR: No se seleccionó un paciente");
                await ShowInfoDialog("Error", "Debe seleccionar un paciente");
                return null;
            }

            if (!fechaPicker.Date.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"[DIALOG] ERROR: No se seleccionó una fecha");
                await ShowInfoDialog("Error", "Debe seleccionar una fecha");
                return null;
            }

            if (string.IsNullOrWhiteSpace(procedimientoBox.Text))
            {
                System.Diagnostics.Debug.WriteLine($"[DIALOG] ERROR: Procedimiento vacío");
                await ShowInfoDialog("Error", "El procedimiento es obligatorio");
                return null;
            }

            if (double.IsNaN(montoSolicitadoBox.Value) || montoSolicitadoBox.Value <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[DIALOG] ERROR: Monto solicitado inválido");
                await ShowInfoDialog("Error", "Debe ingresar un monto solicitado válido");
                return null;
            }

            // Usar SelectedValue para mayor robustez
            var pacienteId = (int)pacienteCombo.SelectedValue;
            var pacienteSeleccionado = Pacientes.FirstOrDefault(p => p.idpaciente == pacienteId);
            if (pacienteSeleccionado == null)
            {
                await ShowInfoDialog("Error", "Paciente seleccionado no válido");
                return null;
            }
            System.Diagnostics.Debug.WriteLine($"[DIALOG] Paciente seleccionado ID: {pacienteSeleccionado.idpaciente}, Nombre: {pacienteSeleccionado.nombre}");

            var nuevaDonacion = new Donaciones
            {
                Fecha = fechaPicker.Date.Value.DateTime,
                idPaciente = pacienteSeleccionado.idpaciente,
                procedimiento = procedimientoBox.Text.Trim(),
                montoSolicitado = (decimal)(double.IsNaN(montoSolicitadoBox.Value) ? 0 : montoSolicitadoBox.Value),
                valor = (decimal)(double.IsNaN(valorDonacionBox.Value) ? 0 : valorDonacionBox.Value),
                total = (decimal)(double.IsNaN(totalBox.Value) ? 0 : totalBox.Value),
                observacion = observacionBox.Text.Trim()
            };

            System.Diagnostics.Debug.WriteLine($"[DIALOG] Donación creada exitosamente");
            return nuevaDonacion;
        }

        System.Diagnostics.Debug.WriteLine($"[DIALOG] Diálogo cancelado");
        return null;
    }

    private void DonacionesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsDonacionSelected = DonacionesListView?.SelectedItem != null;
    }

    // Missing pagination event handlers for the bottom pagination section
    private async void btnPrimeraPagina_Click(object sender, RoutedEventArgs e)
    {
        if (HasPreviousPage)
            await LoadPageAsync(1);
    }

    private async void btnPaginaAnterior_Click(object sender, RoutedEventArgs e)
    {
        if (HasPreviousPage)
            await LoadPageAsync(CurrentPage - 1);
    }

    private async void btnPaginaSiguiente_Click(object sender, RoutedEventArgs e)
    {
        if (HasNextPage)
            await LoadPageAsync(CurrentPage + 1);
    }

    private async void btnUltimaPagina_Click(object sender, RoutedEventArgs e)
    {
        if (HasNextPage)
            await LoadPageAsync(TotalPages);
    }

    private async void comboResultadosPorPagina_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item)
        {
            if (int.TryParse(item.Content?.ToString(), out int newPageSize))
            {
                _pageSize = newPageSize;
                
                // Invalidar cache y recargar primera página
                _cacheService.InvalidateCache("donaciones");
                await LoadPageAsync(1);
            }
        }
    }

    private async Task ShowInfoDialog(string title, string message)
    {
        // Asegurar que estamos en el UI thread
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
            // Verificar que XamlRoot esté disponible
            if (this.XamlRoot == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DIALOG] ERROR: XamlRoot es null");
                return;
            }

            // Crear contenido simple
            var contentStack = new StackPanel
            {
                Spacing = 12,
                MaxWidth = 450
            };

            // Icono según el tipo de mensaje
            string iconGlyph = "\uE946"; // Info por defecto
            Windows.UI.Color iconColor;

            if (title.Contains("Error"))
            {
                iconGlyph = "\uE783"; // Error
                iconColor = Windows.UI.Color.FromArgb(255, 196, 43, 28); // Rojo
            }
            else if (title.Contains("Éxito"))
            {
                iconGlyph = "\uE73E"; // Checkmark
                iconColor = Windows.UI.Color.FromArgb(255, 16, 124, 16); // Verde
            }
            else if (title.Contains("Información") || title.Contains("Advertencia"))
            {
                iconGlyph = "\uE946"; // Info
                iconColor = Windows.UI.Color.FromArgb(255, 255, 185, 0); // Amarillo
            }
            else
            {
                iconColor = Windows.UI.Color.FromArgb(255, 0, 120, 212); // Azul
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

    // Implementar IDisposable para limpiar recursos
    ~DonacionesPage()
    {
        Dispose();
    }

    private bool _disposed = false;

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _searchDelayTimer?.Dispose();
            }
            catch
            {
                // Ignorar errores durante la limpieza
            }
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
