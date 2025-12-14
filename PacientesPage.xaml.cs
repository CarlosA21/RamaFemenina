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
using Microsoft.UI.Xaml.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RamaFemenina.Models;
using RamaFemenina.Data;
using RamaFemenina.Services;
using RamaFemenina.Extensions;

namespace RamaFemenina;

public sealed partial class PacientesPage : Page, INotifyPropertyChanged
{
private readonly IServiceProvider _serviceProvider;
private readonly DataCacheService _cacheService;
private bool _isPatientSelected;
private Timer _searchDelayTimer;
private bool _isPageActive = true;
private bool _isLoading;
    
// Propiedades de paginación
private int _currentPage = 1;
private int _pageSize = 50;
private int _totalCount = 0;
private string _currentSearchTerm = "";

public bool IsPatientSelected
{
    get => _isPatientSelected;
    set
    {
        if (_isPatientSelected != value)
        {
            _isPatientSelected = value;
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

public ObservableCollection<Paciente> Pacientes { get; set; }

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

public PacientesPage()
{
    InitializeComponent();
        
    var app = Application.Current as App;
    _serviceProvider = app!.Services;
    _cacheService = app.Services.GetRequiredService<DataCacheService>();
        
    Pacientes = new ObservableCollection<Paciente>();
        
    // Inicialización de timer para búsqueda con delay
    _searchDelayTimer = new Timer(PerformSearch, null, Timeout.Infinite, Timeout.Infinite);
        
    // Habilitar caché de navegación para evitar reconstrucciones y mantener estado
    NavigationCacheMode = NavigationCacheMode.Enabled;

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

        // Garantizar primera carga de datos al mostrar la página
        if (Pacientes.Count == 0)
        {
            _ = LoadPageAsync(1);
        }
    };
}

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _isPageActive = true;
        
        // Cargar datos solo si es necesario
        if (e.Parameter?.ToString() == "Reload" || Pacientes.Count == 0)
        {
            await LoadPageAsync(1);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _isPageActive = false;
    }

    private async Task LoadPageAsync(int page, bool updateStats = true)
    {
        if (!_isPageActive) return;
        
        System.Diagnostics.Debug.WriteLine($"\n[PACIENTES-LOAD] ===== LoadPageAsync INICIO =====");
        System.Diagnostics.Debug.WriteLine($"[PACIENTES-LOAD] Página solicitada: {page}");
        System.Diagnostics.Debug.WriteLine($"[PACIENTES-LOAD] CurrentPage ANTES: {CurrentPage}");
        System.Diagnostics.Debug.WriteLine($"[PACIENTES-LOAD] TotalCount ANTES: {TotalCount}");
        System.Diagnostics.Debug.WriteLine($"[PACIENTES-LOAD] TotalPages ANTES: {TotalPages}");
        System.Diagnostics.Debug.WriteLine($"[PACIENTES-LOAD] HasNextPage ANTES: {HasNextPage}");
        System.Diagnostics.Debug.WriteLine($"[PACIENTES-LOAD] Término de búsqueda: '{_currentSearchTerm}'");
        
        try
        {
            IsLoading = true;
            var pacientes = await _cacheService.GetPacientesPaginatedAsync(page, _pageSize, _currentSearchTerm);
            var totalCount = await _cacheService.GetPacientesTotalCountAsync(_currentSearchTerm);

            System.Diagnostics.Debug.WriteLine($"[PACIENTES-LOAD] Registros obtenidos: {pacientes.Count()}");
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-LOAD] Total count de BD: {totalCount}");

            if (!_isPageActive) return;

            Pacientes.Clear();
            foreach (var paciente in pacientes)
            {
                // Limpiar datos NULL problemáticos
                if (paciente.nombre == null) paciente.nombre = "Sin especificar";
                if (paciente.cedula == null) paciente.cedula = "N/A";
                if (paciente.estado == null) paciente.estado = "Activo";

                Pacientes.Add(paciente);
            }

            CurrentPage = page;
            TotalCount = totalCount;
            
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-LOAD] CurrentPage DESPUÉS: {CurrentPage}");
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-LOAD] TotalCount DESPUÉS: {TotalCount}");
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-LOAD] TotalPages DESPUÉS: {TotalPages}");
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-LOAD] HasPreviousPage DESPUÉS: {HasPreviousPage}");
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-LOAD] HasNextPage DESPUÉS: {HasNextPage}");

            // Actualizar controles de UI
            if (PacientesListView != null)
                PacientesListView.ItemsSource = Pacientes;
            
            var hayPacientes = Pacientes.Count > 0;
            if (this.FindName("ListViewScroller") is UIElement listScroller)
                listScroller.Visibility = hayPacientes ? Visibility.Visible : Visibility.Collapsed;
            if (EmptyState != null)
                EmptyState.Visibility = hayPacientes ? Visibility.Collapsed : Visibility.Visible;
            
            if (updateStats && _isPageActive)
            {
                _ = ActualizarEstadisticasAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar pacientes: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            
            // CRÍTICO: Actualizar controles de paginación DESPUÉS de IsLoading = false
            // para que los botones se habiliten correctamente
            UpdatePaginationControls();
        }
    }

    private void UpdatePaginationControls()
    {
        System.Diagnostics.Debug.WriteLine($"[PACIENTES-UI] UpdatePaginationControls - IsLoading: {IsLoading}");
        
        if (this.FindName("btnPreviousPage") is Button prevBtn)
        {
            prevBtn.IsEnabled = HasPreviousPage && !IsLoading;
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-UI] btnPreviousPage.IsEnabled = {prevBtn.IsEnabled} (HasPreviousPage: {HasPreviousPage})");
        }
            
        if (this.FindName("btnNextPage") is Button nextBtn)
        {
            nextBtn.IsEnabled = HasNextPage && !IsLoading;
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-UI] btnNextPage.IsEnabled = {nextBtn.IsEnabled} (HasNextPage: {HasNextPage})");
        }
            
        if (this.FindName("btnFirstPage") is Button firstBtn)
        {
            firstBtn.IsEnabled = HasPreviousPage && !IsLoading;
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-UI] btnFirstPage.IsEnabled = {firstBtn.IsEnabled}");
        }
            
        if (this.FindName("btnLastPage") is Button lastBtn)
        {
            lastBtn.IsEnabled = HasNextPage && !IsLoading;
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-UI] btnLastPage.IsEnabled = {lastBtn.IsEnabled}");
        }

        if (this.FindName("txtPageInfo") is TextBlock pageInfoText)
        {
            pageInfoText.Text = PageInfo;
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-UI] PageInfo: {PageInfo}");
        }
        
        System.Diagnostics.Debug.WriteLine($"[PACIENTES-UI] ===== UpdatePaginationControls FIN =====\n");
    }

    private void PerformSearch(object state)
    {
        if (!_isPageActive) return;
        
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, async () =>
        {
            // Resetear a la primera página al realizar una búsqueda
            CurrentPage = 1;
            _cacheService.InvalidateCache("pacientes");
            await LoadPageAsync(CurrentPage);
        });
    }

    // Método duplicado eliminado

    private async Task ActualizarEstadisticasAsync()
    {
        try
        {
            if (!_isPageActive) return;
            
            // Usar una instancia separada del contexto para estadísticas
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
            
            var totalPacientes = await context.Pacientes.CountAsync().ConfigureAwait(false);

            if (!_isPageActive) return;

            var pacientesActivos = await context.Pacientes
                .CountAsync(p => p.estado == "Activo").ConfigureAwait(false);

            var pacientesMujeres = await context.Pacientes
                .CountAsync(p => p.sexo == "Femenino").ConfigureAwait(false);

            // Pacientes con donaciones
            var pacientesConDonaciones = await context.Pacientes
                .Where(p => context.Donaciones.Any(d => d.idPaciente == p.idpaciente))
                .CountAsync().ConfigureAwait(false);

            // Actualizar en UI Thread
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                try
                {
                    if (!_isPageActive) return;

                    if (this.FindName("txtTotalPacientes") is TextBlock totalText)
                        totalText.Text = totalPacientes.ToString();
                        
                    if (this.FindName("txtContador") is Microsoft.UI.Xaml.Documents.Run contadorRun)
                        contadorRun.Text = TotalCount.ToString();
                    
                    if (this.FindName("txtPacientesActivos") is TextBlock activosText)
                        activosText.Text = pacientesActivos.ToString();
                    
                    if (this.FindName("txtPacientesMujeres") is TextBlock mujeresText)
                        mujeresText.Text = pacientesMujeres.ToString();
                    
                    if (this.FindName("txtConDonaciones") is TextBlock donacionesText)
                        donacionesText.Text = pacientesConDonaciones.ToString();
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
            _searchDelayTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _currentSearchTerm = sender.Text?.Trim() ?? "";
            _searchDelayTimer?.Change(500, Timeout.Infinite);
        }
    }

    // Eventos de paginación
    private async void BtnFirstPage_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"\n[PACIENTES-BTN] BtnFirstPage_Click - HasPreviousPage: {HasPreviousPage}, IsLoading: {IsLoading}");
        if (HasPreviousPage && !IsLoading)
        {
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-BTN] Navegando a página 1 desde página {CurrentPage}");
            CurrentPage = 1;
            await LoadPageAsync(CurrentPage);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-BTN] Navegación bloqueada");
        }
    }

    private async void BtnPreviousPage_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"\n[PACIENTES-BTN] BtnPreviousPage_Click - HasPreviousPage: {HasPreviousPage}, IsLoading: {IsLoading}");
        if (HasPreviousPage && !IsLoading)
        {
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-BTN] Navegando a página {CurrentPage - 1} desde página {CurrentPage}");
            CurrentPage--;
            await LoadPageAsync(CurrentPage);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-BTN] Navegación bloqueada");
        }
    }

    private async void BtnNextPage_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"\n[PACIENTES-BTN] BtnNextPage_Click - CurrentPage: {CurrentPage}, TotalPages: {TotalPages}, HasNextPage: {HasNextPage}, IsLoading: {IsLoading}");
        if (HasNextPage && !IsLoading)
        {
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-BTN] Navegando a página {CurrentPage + 1} desde página {CurrentPage}");
            CurrentPage++;
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-BTN] CurrentPage actualizado a: {CurrentPage}");
            await LoadPageAsync(CurrentPage);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-BTN] Navegación bloqueada - HasNextPage: {HasNextPage}, IsLoading: {IsLoading}");
        }
    }

    private async void BtnLastPage_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"\n[PACIENTES-BTN] BtnLastPage_Click - HasNextPage: {HasNextPage}, IsLoading: {IsLoading}");
        if (HasNextPage && !IsLoading)
        {
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-BTN] Navegando a página {TotalPages} desde página {CurrentPage}");
            CurrentPage = TotalPages;
            await LoadPageAsync(CurrentPage);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[PACIENTES-BTN] Navegación bloqueada");
        }
    }

    private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
    {
        // Limpiar cache y recargar
        _cacheService.InvalidateCache("pacientes");
        await LoadPageAsync(CurrentPage);
    }

    private async void BtnNuevoPaciente_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Crear y mostrar el diálogo
            var nuevoPaciente = await MostrarDialogoPaciente(null);
            
            if (nuevoPaciente != null)
            {
                // Guardar en la base de datos
                await GuardarPacienteEnBaseDatos(nuevoPaciente);
                
                // Actualizar la interfaz
                await ActualizarInterfazDespuesDeGuardar(nuevoPaciente);
            }
        }
        catch (Exception ex)
        {
            await ShowInfoDialog("Error", $"Error al crear paciente: {ex.Message}");
        }
    }

    private async Task GuardarPacienteEnBaseDatos(Paciente paciente)
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
        
        context.Pacientes.Add(paciente);
        await context.SaveChangesAsync();
    }

    private async Task ActualizarInterfazDespuesDeGuardar(Paciente paciente)
    {
        // Invalidar caché y recargar datos
        _cacheService.InvalidateCache("pacientes");
        
        // Usar DispatcherQueue para asegurar que estamos en UI thread
        await DispatcherQueue.EnqueueAsync(async () =>
        {
            await LoadPageAsync(CurrentPage);
            
            // Pequeño delay para asegurar que el diálogo anterior se cerró completamente
            await Task.Delay(100);
            
            // Mostrar mensaje de éxito
            await ShowInfoDialog("Éxito", 
                $"Paciente registrado correctamente.\n\n" +
                $"Nombre: {paciente.nombre}\n" +
                $"Cédula: {paciente.cedula}");
        });
    }

    private async void BtnEditarPaciente_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Prefer the DataContext from the clicked button (inline actions don't always change ListView selection)
            var pacienteSeleccionado = (sender as Button)?.DataContext as Paciente ?? PacientesListView.SelectedItem as Paciente;
            if (pacienteSeleccionado == null)
            {
                await ShowInfoDialog("Error", "Debe seleccionar un paciente");
                return;
            }

            // Mostrar formulario con datos existentes
            var pacienteEditado = await MostrarDialogoPaciente(pacienteSeleccionado);
            
            if (pacienteEditado != null)
            {
                // Actualizar en la base de datos
                await ActualizarPacienteEnBaseDatos(pacienteSeleccionado.idpaciente, pacienteEditado);
                
                // Actualizar la interfaz
                await ActualizarInterfazDespuesDeActualizar(pacienteEditado);
            }
        }
        catch (Exception ex)
        {
            await ShowInfoDialog("Error", $"Error al editar paciente: {ex.Message}");
        }
    }

    private async Task ActualizarPacienteEnBaseDatos(int idPaciente, Paciente datosNuevos)
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();

        // Cargar entidad existente (trackeada)
        var pacienteExistente = await context.Pacientes.FirstOrDefaultAsync(p => p.idpaciente == idPaciente);
        if (pacienteExistente == null)
        {
            throw new InvalidOperationException("El paciente ya no existe en la base de datos");
        }

        // Aplicar nuevos valores a la entidad trackeada
        pacienteExistente.nombre = datosNuevos.nombre.Trim();
        pacienteExistente.cedula = datosNuevos.cedula.Trim();
        pacienteExistente.telefono = datosNuevos.telefono?.Trim();
        pacienteExistente.celular = datosNuevos.celular?.Trim();
        pacienteExistente.estado = datosNuevos.estado.Trim();
        pacienteExistente.nrecord = datosNuevos.nrecord?.Trim();
        pacienteExistente.observaciones = datosNuevos.observaciones?.Trim();
        pacienteExistente.sexo = datosNuevos.sexo.Trim();
        pacienteExistente.area = datosNuevos.area?.Trim();

        // Marcar propiedades como modificadas explícitamente
        var entry = context.Entry(pacienteExistente);
        entry.Property(e => e.nombre).IsModified = true;
        entry.Property(e => e.cedula).IsModified = true;
        entry.Property(e => e.telefono).IsModified = true;
        entry.Property(e => e.celular).IsModified = true;
        entry.Property(e => e.estado).IsModified = true;
        entry.Property(e => e.nrecord).IsModified = true;
        entry.Property(e => e.observaciones).IsModified = true;
        entry.Property(e => e.sexo).IsModified = true;
        entry.Property(e => e.area).IsModified = true;

        // Guardar cambios y validar resultado
        var affected = await context.SaveChangesAsync().ConfigureAwait(false);
        if (affected == 0)
        {
            // Forzar actualización si por alguna razón no se detectaron cambios
            context.Update(pacienteExistente);
            affected = await context.SaveChangesAsync().ConfigureAwait(false);
        }

        // Invalidar caché para que la UI no muestre datos viejos
        _cacheService.InvalidateCache("pacientes");
    }

    private async Task ActualizarInterfazDespuesDeActualizar(Paciente paciente)
    {
        // Invalidar caché y recargar datos
        _cacheService.InvalidateCache("pacientes");
        
        // Usar DispatcherQueue para asegurar que estamos en UI thread
        await DispatcherQueue.EnqueueAsync(async () =>
        {
            await LoadPageAsync(CurrentPage);
            
            // Pequeño delay para asegurar que el diálogo anterior se cerró completamente
            await Task.Delay(100);
            
            // Mostrar mensaje de éxito
            await ShowInfoDialog("Éxito", 
                $"El paciente ha sido actualizado exitosamente.\n\n" +
                $"Nombre: {paciente.nombre}\n" +
                $"Cédula: {paciente.cedula}");
        });
    }

    private async void BtnEliminarPaciente_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validar XamlRoot
            if (this.XamlRoot == null)
            {
                System.Diagnostics.Debug.WriteLine("[BTN-ELIMINAR] ERROR: XamlRoot es null");
                return;
            }

            // Prefer the DataContext from the clicked button (inline actions don't always change ListView selection)
            var pacienteSeleccionado = (sender as Button)?.DataContext as Paciente ?? PacientesListView?.SelectedItem as Paciente;
            if (pacienteSeleccionado == null)
            {
                await ShowInfoDialog("Error", "Debe seleccionar un paciente");
                return;
            }

            var confirmDialog = new ContentDialog
            {
                Title = "Confirmar Eliminación",
                Content = $"¿Está seguro que desea eliminar al paciente?\n\n" +
                          $"ID: {pacienteSeleccionado.idpaciente}\n" +
                          $"Nombre: {pacienteSeleccionado.nombre}\n" +
                          $"Cédula: {pacienteSeleccionado.cedula}\n\n" +
                          $"Esta acción no se puede deshacer.",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            ContentDialogResult result;
            try
            {
                result = await confirmDialog.ShowAsync();
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                System.Diagnostics.Debug.WriteLine($"[BTN-ELIMINAR] COM ERROR al mostrar diálogo: {comEx.Message}");
                await ShowInfoDialog("Error", "Error al mostrar el diálogo de confirmación.");
                return;
            }

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();

                    var paciente = await context.Pacientes.FindAsync(pacienteSeleccionado.idpaciente).ConfigureAwait(false);
                    if (paciente != null)
                    {
                        context.Pacientes.Remove(paciente);
                        await context.SaveChangesAsync().ConfigureAwait(false);

                        _cacheService.InvalidateCache("pacientes");
                        
                        await DispatcherQueue.EnqueueAsync(async () =>
                        {
                            await LoadPageAsync(CurrentPage);
                            
                            // Pequeño delay para evitar COMException
                            await Task.Delay(100);
                            
                            await ShowInfoDialog("Éxito", "Paciente eliminado correctamente");
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BTN-ELIMINAR] Error al eliminar: {ex.Message}");
                    await DispatcherQueue.EnqueueAsync(async () =>
                    {
                        await ShowInfoDialog("Error", $"Error al eliminar paciente: {ex.Message}");
                    });
                }
            }
        }
        catch (System.Runtime.InteropServices.COMException comEx)
        {
            System.Diagnostics.Debug.WriteLine($"[BTN-ELIMINAR] COM ERROR: {comEx.Message}");
            System.Diagnostics.Debug.WriteLine($"[BTN-ELIMINAR] HResult: {comEx.HResult:X}");
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                await ShowInfoDialog("Error", "Error al procesar la eliminación.");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BTN-ELIMINAR] ERROR: {ex.GetType().Name} - {ex.Message}");
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                await ShowInfoDialog("Error", $"Error inesperado: {ex.Message}");
            });
        }
    }

    private async void BtnRegistrarDonacion_Click(object sender, RoutedEventArgs e)
    {
        var pacienteSeleccionado = PacientesListView.SelectedItem as Paciente;
        if (pacienteSeleccionado == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar un paciente para registrar la donación");
            return;
        }

        var resultado = await MostrarDialogoDonacion(pacienteSeleccionado);
        if (resultado != null)
        {
            try
            {
                // Usar una instancia separada del contexto
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                
                context.Donaciones.Add(resultado);
                await context.SaveChangesAsync().ConfigureAwait(false);
                
                // Invalidar cache y recargar
                _cacheService.InvalidateCache("donaciones");
                await ShowInfoDialog("Éxito", $"Donación registrada correctamente.\nPaciente: {pacienteSeleccionado.nombre}\nMonto: RD$ {resultado.valor:N2}");
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al registrar donación: {ex.Message}");
            }
        }
    }

    private async void BtnVerDonaciones_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Obtener el paciente del DataContext del botón
            var pacienteSeleccionado = (sender as Button)?.DataContext as Paciente;
            if (pacienteSeleccionado == null)
            {
                await ShowInfoDialog("Error", "No se pudo obtener la información del paciente");
                return;
            }

            // Mostrar diálogo con las donaciones del paciente
            await MostrarDialogoDonacionesPaciente(pacienteSeleccionado);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BTN-VER-DONACIONES] ERROR: {ex.Message}");
            await ShowInfoDialog("Error", $"Error al cargar donaciones: {ex.Message}");
        }
    }

    private async Task MostrarDialogoDonacionesPaciente(Paciente paciente)
    {
        if (this.XamlRoot == null)
        {
            System.Diagnostics.Debug.WriteLine("[VER-DONACIONES] XamlRoot es null");
            return;
        }

        try
        {
            // Obtener las donaciones del paciente
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();

            var donaciones = await context.Donaciones
                .Where(d => d.idPaciente == paciente.idpaciente)
                .OrderByDescending(d => d.Fecha)
                .ToListAsync();

            // Crear el contenido del diálogo
            var contentPanel = new StackPanel { Spacing = 16 };

            // Información del paciente
            var infoPanel = new StackPanel
            {
                Spacing = 8,
                Padding = new Thickness(0, 0, 0, 16),
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            infoPanel.Children.Add(new TextBlock
            {
                Text = $"Paciente: {paciente.nombre}",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            });

            infoPanel.Children.Add(new TextBlock
            {
                Text = $"Cédula: {paciente.cedula}",
                FontSize = 12,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
            });

            contentPanel.Children.Add(infoPanel);

            // Resumen de donaciones
            if (donaciones.Any())
            {
                var totalDonado = donaciones.Sum(d => d.valor);
                var totalSolicitado = donaciones.Sum(d => d.montoSolicitado);
                var cantidadDonaciones = donaciones.Count;

                var resumenPanel = new StackPanel
                {
                    Spacing = 12,
                    Padding = new Thickness(16),
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Windows.UI.Color.FromArgb(20, 102, 126, 234)),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 16)
                };

                var gridResumen = new Grid
                {
                    ColumnSpacing = 16,
                    RowSpacing = 8
                };
                gridResumen.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                gridResumen.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                gridResumen.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                gridResumen.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var cantidadStack = new StackPanel();
                cantidadStack.Children.Add(new TextBlock
                {
                    Text = "Cantidad de Donaciones",
                    FontSize = 11,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
                });
                cantidadStack.Children.Add(new TextBlock
                {
                    Text = cantidadDonaciones.ToString(),
                    FontSize = 20,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                });
                Grid.SetColumn(cantidadStack, 0);
                Grid.SetRow(cantidadStack, 0);

                var totalStack = new StackPanel();
                totalStack.Children.Add(new TextBlock
                {
                    Text = "Total Donado",
                    FontSize = 11,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
                });
                totalStack.Children.Add(new TextBlock
                {
                    Text = $"RD$ {totalDonado:N2}",
                    FontSize = 20,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Windows.UI.Color.FromArgb(255, 16, 124, 16))
                });
                Grid.SetColumn(totalStack, 1);
                Grid.SetRow(totalStack, 0);

                var solicitadoStack = new StackPanel();
                solicitadoStack.Children.Add(new TextBlock
                {
                    Text = "Total Solicitado",
                    FontSize = 11,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
                });
                solicitadoStack.Children.Add(new TextBlock
                {
                    Text = $"RD$ {totalSolicitado:N2}",
                    FontSize = 20,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                });
                Grid.SetColumn(solicitadoStack, 0);
                Grid.SetRow(solicitadoStack, 1);

                var diferenciaStack = new StackPanel();
                var diferencia = totalDonado - totalSolicitado;
                diferenciaStack.Children.Add(new TextBlock
                {
                    Text = "Diferencia",
                    FontSize = 11,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
                });
                diferenciaStack.Children.Add(new TextBlock
                {
                    Text = $"RD$ {diferencia:N2}",
                    FontSize = 20,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        diferencia >= 0
                            ? Windows.UI.Color.FromArgb(255, 16, 124, 16)
                            : Windows.UI.Color.FromArgb(255, 196, 43, 28))
                });
                Grid.SetColumn(diferenciaStack, 1);
                Grid.SetRow(diferenciaStack, 1);

                gridResumen.Children.Add(cantidadStack);
                gridResumen.Children.Add(totalStack);
                gridResumen.Children.Add(solicitadoStack);
                gridResumen.Children.Add(diferenciaStack);

                resumenPanel.Children.Add(gridResumen);
                contentPanel.Children.Add(resumenPanel);

                // Lista de donaciones
                var listaDonaciones = new StackPanel { Spacing = 8 };

                foreach (var donacion in donaciones.Take(10)) // Limitar a 10 más recientes
                {
                    var donacionBorder = new Border
                    {
                        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Windows.UI.Color.FromArgb(10, 0, 0, 0)),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 0, 0, 4)
                    };

                    var donacionGrid = new Grid { ColumnSpacing = 12 };
                    donacionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    donacionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    donacionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var fechaBadge = new Border
                    {
                        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Windows.UI.Color.FromArgb(255, 102, 126, 234)),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(8, 4, 8, 4)
                    };
                    fechaBadge.Child = new TextBlock
                    {
                        Text = donacion.Fecha.ToString("dd/MM/yyyy"),
                        FontSize = 11,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
                    };
                    Grid.SetColumn(fechaBadge, 0);

                    var infoStack = new StackPanel { Spacing = 4 };
                    if (!string.IsNullOrWhiteSpace(donacion.procedimiento))
                    {
                        infoStack.Children.Add(new TextBlock
                        {
                            Text = donacion.procedimiento,
                            FontSize = 13,
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                        });
                    }
                    if (!string.IsNullOrWhiteSpace(donacion.observacion))
                    {
                        infoStack.Children.Add(new TextBlock
                        {
                            Text = donacion.observacion,
                            FontSize = 11,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                            TextTrimming = TextTrimming.CharacterEllipsis
                        });
                    }
                    Grid.SetColumn(infoStack, 1);

                    var montoText = new TextBlock
                    {
                        Text = $"RD$ {donacion.valor:N2}",
                        FontSize = 14,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Windows.UI.Color.FromArgb(255, 16, 124, 16)),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(montoText, 2);

                    donacionGrid.Children.Add(fechaBadge);
                    donacionGrid.Children.Add(infoStack);
                    donacionGrid.Children.Add(montoText);

                    donacionBorder.Child = donacionGrid;
                    listaDonaciones.Children.Add(donacionBorder);
                }

                if (donaciones.Count > 10)
                {
                    listaDonaciones.Children.Add(new TextBlock
                    {
                        Text = $"... y {donaciones.Count - 10} donaciones más",
                        FontSize = 11,
                        FontStyle = Windows.UI.Text.FontStyle.Italic,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                        Margin = new Thickness(0, 8, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }

                contentPanel.Children.Add(listaDonaciones);
            }
            else
            {
                // Sin donaciones
                var sinDonacionesPanel = new StackPanel
                {
                    Spacing = 12,
                    Padding = new Thickness(40),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                sinDonacionesPanel.Children.Add(new FontIcon
                {
                    Glyph = "\uE8EC",
                    FontSize = 48,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                sinDonacionesPanel.Children.Add(new TextBlock
                {
                    Text = "Sin donaciones registradas",
                    FontSize = 16,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                sinDonacionesPanel.Children.Add(new TextBlock
                {
                    Text = "Este paciente aún no tiene donaciones asociadas",
                    FontSize = 12,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                contentPanel.Children.Add(sinDonacionesPanel);
            }

            var scrollViewer = new ScrollViewer
            {
                Content = contentPanel,
                MaxHeight = 600,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var dialog = new ContentDialog
            {
                Title = "?? Historial de Donaciones",
                Content = scrollViewer,
                CloseButtonText = "Cerrar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VER-DONACIONES] ERROR: {ex.Message}");
            await ShowInfoDialog("Error", $"Error al cargar las donaciones: {ex.Message}");
        }
    }

    private async Task<Paciente> MostrarDialogoPaciente(Paciente pacienteExistente)
    {
        // Dialogo con validación utilizando PrimaryButtonClick y deferral (patrón similar a ChequesPage)
        if (this.XamlRoot == null)
        {
            System.Diagnostics.Debug.WriteLine("[PACIENTES] XamlRoot es null, no se puede mostrar diálogo");
            return null;
        }

        bool esEdicion = pacienteExistente != null;

        var cedulaBox = new TextBox
        {
            Header = "Cédula *",
            PlaceholderText = "000-0000000-0",
            Text = pacienteExistente?.cedula ?? "",
            MaxLength = 50
        };

        var nombreBox = new TextBox
        {
            Header = "Nombre Completo *",
            PlaceholderText = "Nombre y apellidos del paciente",
            Text = pacienteExistente?.nombre ?? "",
            MaxLength = 50
        };

        var telefonoBox = new TextBox
        {
            Header = "Teléfono",
            PlaceholderText = "809-000-0000",
            Text = pacienteExistente?.telefono ?? "",
            MaxLength = 50
        };

        var celularBox = new TextBox
        {
            Header = "Celular",
            PlaceholderText = "809-000-0000",
            Text = pacienteExistente?.celular ?? "",
            MaxLength = 50
        };

        var sexoCombo = new ComboBox
        {
            Header = "Sexo",
            MinWidth = 150
        };
        sexoCombo.Items.Add(new ComboBoxItem { Content = "Femenino", Tag = "Femenino" });
        sexoCombo.Items.Add(new ComboBoxItem { Content = "Masculino", Tag = "Masculino" });
        if (pacienteExistente?.sexo != null)
        {
            for (int i = 0; i < sexoCombo.Items.Count; i++)
            {
                if ((sexoCombo.Items[i] as ComboBoxItem)?.Tag?.ToString() == pacienteExistente.sexo)
                {
                    sexoCombo.SelectedIndex = i;
                    break;
                }
            }
        }

        var estadoCombo = new ComboBox
        {
            Header = "Estado",
            MinWidth = 150
        };
        estadoCombo.Items.Add(new ComboBoxItem { Content = "Activo", Tag = "Activo" });
        estadoCombo.Items.Add(new ComboBoxItem { Content = "Inactivo", Tag = "Inactivo" });
        estadoCombo.SelectedIndex = pacienteExistente?.estado == "Inactivo" ? 1 : 0;

        var nrecordBox = new TextBox
        {
            Header = "No. Record",
            PlaceholderText = "Número de record médico",
            Text = pacienteExistente?.nrecord ?? "",
            MaxLength = 50
        };

        var areaBox = new TextBox
        {
            Header = "Área *",
            PlaceholderText = "Área o departamento",
            Text = pacienteExistente?.area ?? "General",
            MaxLength = 50
        };

        var observacionesBox = new TextBox
        {
            Header = "Observaciones",
            PlaceholderText = "Observaciones médicas o comentarios",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            Text = pacienteExistente?.observaciones ?? "",
            MaxLength = 300
        };

        var formPanel = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                cedulaBox,
                nombreBox,
                telefonoBox,
                celularBox,
                sexoCombo,
                estadoCombo,
                areaBox,
                nrecordBox,
                observacionesBox
            }
        };

        var scrollViewer = new ScrollViewer
        {
            Content = formPanel,
            MaxHeight = 600,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(4)
        };

        var dialog = new ContentDialog
        {
            Title = esEdicion ? "Editar Paciente" : "Nuevo Paciente",
            Content = scrollViewer,
            PrimaryButtonText = esEdicion ? "Actualizar" : "Guardar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        Paciente pacienteResultado = null;
        string mensajeError = null;

        dialog.PrimaryButtonClick += (s, args) =>
        {
            var deferral = args.GetDeferral();
            try
            {
                // Validación
                if (string.IsNullOrWhiteSpace(cedulaBox.Text))
                {
                    mensajeError = "La cédula es obligatoria";
                    args.Cancel = true;
                    return;
                }
                if (string.IsNullOrWhiteSpace(nombreBox.Text))
                {
                    mensajeError = "El nombre es obligatorio";
                    args.Cancel = true;
                    return;
                }

                pacienteResultado = new Paciente
                {
                    cedula = cedulaBox.Text.Trim(),
                    nombre = nombreBox.Text.Trim(),
                    telefono = telefonoBox.Text.Trim(),
                    celular = celularBox.Text.Trim(),
                    sexo = (sexoCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Femenino",
                    estado = (estadoCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Activo",
                    area = areaBox.Text.Trim(),
                    nrecord = nrecordBox.Text.Trim(),
                    observaciones = observacionesBox.Text.Trim()
                };
            }
            catch (Exception ex)
            {
                mensajeError = ex.Message;
                args.Cancel = true;
            }
            finally
            {
                deferral.Complete();
            }
        };

        ContentDialogResult result;
        try
        {
            result = await dialog.ShowAsync();
        }
        catch (System.Runtime.InteropServices.COMException comEx)
        {
            System.Diagnostics.Debug.WriteLine($"[PACIENTES] COM ERROR al mostrar diálogo: {comEx.Message}");
            return null;
        }

        if (!string.IsNullOrEmpty(mensajeError))
        {
            // Mostrar error después de cerrar el diálogo para evitar COMException por diálogos anidados
            await ShowInfoDialog("Error", mensajeError);
        }

        return result == ContentDialogResult.Primary ? pacienteResultado : null;
    }

    private async Task<Donaciones> MostrarDialogoDonacion(Paciente paciente)
    {
        // Implementación básica del diálogo de donación
        var valorBox = new NumberBox
        {
            Header = "Monto Donado (RD$) *",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0
        };

        var montoSolicitadoBox = new NumberBox
        {
            Header = "Monto Solicitado (RD$)",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0
        };

        var procedimientoBox = new TextBox
        {
            Header = "Procedimiento",
            PlaceholderText = "Descripción del procedimiento médico",
            MaxLength = 50
        };

        var observacionBox = new TextBox
        {
            Header = "Observaciones",
            PlaceholderText = "Observaciones sobre la donación",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            MaxLength = 300
        };

        var infoPanel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 16)
        };
        infoPanel.Children.Add(new TextBlock { Text = $"Paciente: {paciente.nombre}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        infoPanel.Children.Add(new TextBlock { Text = $"Cédula: {paciente.cedula}", FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });

        var formPanel = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                infoPanel,
                valorBox,
                montoSolicitadoBox,
                procedimientoBox,
                observacionBox
            }
        };

        var dialog = new ContentDialog
        {
            Title = "Registrar Donación",
            Content = formPanel,
            PrimaryButtonText = "Registrar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (valorBox.Value <= 0 || double.IsNaN(valorBox.Value))
            {
                await ShowInfoDialog("Error", "Debe ingresar un monto válido");
                return null;
            }

            return new Donaciones
            {
                idPaciente = paciente.idpaciente,
                Fecha = DateTime.Now,
                valor = (decimal)valorBox.Value,
                total = (decimal)valorBox.Value, // Por simplicidad, igual al valor
                montoSolicitado = (decimal)(montoSolicitadoBox.Value > 0 ? montoSolicitadoBox.Value : valorBox.Value),
                procedimiento = procedimientoBox.Text.Trim(),
                observacion = observacionBox.Text.Trim()
            };
        }

        return null;
    }

    private void PacientesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsPatientSelected = PacientesListView?.SelectedItem != null;

        // Actualizar estado de botones directamente
        var haySeleccion = IsPatientSelected;

        if (this.FindName("btnEditar") is Button editBtn)
            editBtn.IsEnabled = haySeleccion;

        if (this.FindName("btnEliminar") is Button delBtn)
            delBtn.IsEnabled = haySeleccion;

        if (this.FindName("btnDonacion") is Button donacionBtn)
            donacionBtn.IsEnabled = haySeleccion;
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
