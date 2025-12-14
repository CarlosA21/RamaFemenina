using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
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

public sealed partial class ChequesPage : Page, INotifyPropertyChanged
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DataCacheService _cacheService;
    private bool _isChequeSelected;
    private bool _isLoading;
    private CancellationTokenSource _searchCancellationTokenSource;
    private Timer _searchDelayTimer;
    private bool _isPageActive = true;
    private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);

    // Propiedades de paginación
    private int _currentPage = 1;
    private int _pageSize = 50;
    private int _totalCount = 0;
    private string _currentSearchTerm = "";

    // Configuración de posiciones de impresión (en milímetros)
    // Valores por defecto originales
    private float nombreX = 47;
    private float nombreY = 36; //valor correcto
    private float fechaX = 165; // valor correcto
    private float fechaY = 21; // valor correcto
    // Espaciados entre dígitos de la fecha (modo separado)
    private float fechaStepX = 4f;
    private float fechaStepY = 0f;
    private float letraX = 16;
    private float letraY = 44; // valor correcto
    private float montoX = 160;
    private float montoY = 36; // valor correcto 
    private float conceptoX = 32;
    private float conceptoY = 120;
    private float fechaCirculoX = 6;
    private float fechaCirculoY = 120;
    private float montoCirculoX = 196;
    private float montoCirculoY = 120;
    // Tamaño de fuente independiente para la fecha en el círculo (más pequeña)
    private float fechaCirculoFontSize = 9f;

    public bool IsChequeSelected
    {
        get => _isChequeSelected;
        set
        {
            if (_isChequeSelected != value)
            {
                _isChequeSelected = value;
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
                
                // Asegurar que la actualización se ejecute en el UI thread
                if (DispatcherQueue.HasThreadAccess)
                {
                    OnPropertyChanged();
                    if (this.FindName("LoadingIndicator") is ProgressRing loadingIndicator)
                        loadingIndicator.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        OnPropertyChanged();
                        if (this.FindName("LoadingIndicator") is ProgressRing loadingIndicator)
                            loadingIndicator.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                    });
                }
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

    public ObservableCollection<Cheques> ChequesCollection { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ChequesPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Enabled;

        var app = Application.Current as App;
        _serviceProvider = app!.Services;
        _cacheService = app.Services.GetRequiredService<DataCacheService>();

        ChequesCollection = new ObservableCollection<Cheques>();
        _searchDelayTimer = new Timer(PerformSearch, null, Timeout.Infinite, Timeout.Infinite);

        // Evitar carga inicial duplicada desde Loaded; la carga se maneja en OnNavigatedTo

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

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _isPageActive = true;

        System.Diagnostics.Debug.WriteLine($"[CHEQUES-PAGE] OnNavigatedTo - Iniciando carga de datos");

        if (_searchCancellationTokenSource == null || _searchCancellationTokenSource.IsCancellationRequested)
        {
            _searchCancellationTokenSource?.Dispose();
            _searchCancellationTokenSource = new CancellationTokenSource();
        }

        // Recargar solo si la colección está vacía o se solicitó explicitamente
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, async () =>
        {
            try
            {
                if (ChequesCollection.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[CHEQUES-PAGE] Cargando cheques desde caché...");
                    await LoadPageAsync(CurrentPage > 0 ? CurrentPage : 1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CHEQUES-PAGE] Error in OnNavigatedTo: {ex.Message}");
            }
        });
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _isPageActive = false;
    }

    private async Task LoadPageAsync(int page, bool updateStats = true)
    {
        System.Diagnostics.Debug.WriteLine($"[ChequesPage] LoadPageAsync iniciado - Page: {page}");
        
        if (!await _loadingSemaphore.WaitAsync(5000))
        {
            System.Diagnostics.Debug.WriteLine("[ChequesPage] No se pudo obtener el semáforo después de 5 segundos - cancelando");
            return;
        }

        try
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                _searchCancellationTokenSource?.Cancel();
                _searchCancellationTokenSource?.Dispose();
            }
            catch { }

            _searchCancellationTokenSource = new CancellationTokenSource();

            var cheques = await _cacheService.GetChequesPaginatedAsync(
                page, _pageSize, _currentSearchTerm,
                cancellationToken: _searchCancellationTokenSource.Token);

            var totalCount = await _cacheService.GetChequesTotalCountAsync(_currentSearchTerm, cancellationToken: _searchCancellationTokenSource.Token);

            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                try
                {
                    if (!_isPageActive) return;

                    ChequesCollection.Clear();
                    foreach (var cheque in cheques)
                    {
                        if (cheque.concepto == null)
                            cheque.concepto = string.Empty;
                        if (cheque.nombre == null)
                            cheque.nombre = "Sin especificar";
                        if (cheque.numero == null)
                            cheque.numero = "SIN-NUM";

                        ChequesCollection.Add(cheque);
                    }

                    CurrentPage = page;
                    TotalCount = totalCount;

                    if (ChequesListView != null)
                        ChequesListView.ItemsSource = ChequesCollection;

                    var hayCheques = ChequesCollection.Count > 0;
                    if (this.FindName("ListViewScroller") is UIElement listScroller)
                        listScroller.Visibility = hayCheques ? Visibility.Visible : Visibility.Collapsed;
                    if (EmptyState != null)
                        EmptyState.Visibility = hayCheques ? Visibility.Collapsed : Visibility.Visible;

                    UpdatePaginationControls();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating UI: {ex.Message}");
                }
            });

            if (updateStats && _isPageActive)
            {
                _ = Task.Run(async () => await ActualizarEstadisticasAsync().ConfigureAwait(false));
            }
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("Operación de carga cancelada");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar cheques: {ex.Message}");
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                if (_isPageActive)
                    await ShowInfoDialog("Error", $"Error al cargar cheques: {ex.Message}");
            });
        }
        finally
        {
            IsLoading = false;
            _loadingSemaphore.Release();
        }
    }

    private void UpdatePaginationControls()
    {
        if (this.FindName("btnPreviousPage") is Button prevBtn)
            prevBtn.IsEnabled = HasPreviousPage && !IsLoading;

        if (this.FindName("btnNextPage") is Button nextBtn)
            nextBtn.IsEnabled = HasNextPage && !IsLoading;

        if (this.FindName("btnFirstPage") is Button firstBtn)
            firstBtn.IsEnabled = HasPreviousPage && !IsLoading;

        if (this.FindName("btnLastPage") is Button lastBtn)
            lastBtn.IsEnabled = HasNextPage && !IsLoading;

        if (this.FindName("txtPageInfo") is TextBlock pageInfoText)
            pageInfoText.Text = PageInfo;
    }

    private async Task ActualizarEstadisticasAsync()
    {
        try
        {
            if (!_isPageActive) return;

            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();

            var totalCheques = await context.Cheques.CountAsync();

            if (!_isPageActive) return;

            var montoTotal = await context.Cheques.SumAsync(c => c.monto);
            var promedio = totalCheques > 0 ? montoTotal / totalCheques : 0;

            var primerDiaMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var chequesEsteMes = await context.Cheques
                .Where(c => c.Fecha >= primerDiaMes)
                .SumAsync(c => c.monto);

            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                try
                {
                    if (!_isPageActive) return;

                    if (this.FindName("txtTotalCheques") is TextBlock totalText)
                        totalText.Text = totalCheques.ToString();

                    if (this.FindName("txtContador") is Microsoft.UI.Xaml.Documents.Run contadorRun)
                        contadorRun.Text = TotalCount.ToString();

                    if (this.FindName("txtMontoTotal") is TextBlock totalMontoText)
                        totalMontoText.Text = $"RD$ {montoTotal:N2}";

                    if (this.FindName("txtPromedio") is TextBlock promedioText)
                        promedioText.Text = $"RD$ {promedio:N2}";

                    if (this.FindName("txtEsteMes") is TextBlock mesText)
                        mesText.Text = $"RD$ {chequesEsteMes:N2}";
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

    private void PerformSearch(object state)
    {
        if (!_isPageActive) return;

        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, async () =>
        {
            _cacheService.InvalidateCache("cheques");
            await LoadPageAsync(1);
        });
    }

    private async void BtnFirstPage_Click(object sender, RoutedEventArgs e)
    {
        if (HasPreviousPage && !IsLoading)
            await LoadPageAsync(1);
    }

    private async void BtnPreviousPage_Click(object sender, RoutedEventArgs e)
    {
        if (HasPreviousPage && !IsLoading)
            await LoadPageAsync(CurrentPage - 1);
    }

    private async void BtnNextPage_Click(object sender, RoutedEventArgs e)
    {
        if (HasNextPage && !IsLoading)
            await LoadPageAsync(CurrentPage + 1);
    }

    private async void BtnLastPage_Click(object sender, RoutedEventArgs e)
    {
        if (HasNextPage && !IsLoading)
            await LoadPageAsync(TotalPages);
    }

    private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
    {
        _cacheService.InvalidateCache("cheques");
        await LoadPageAsync(CurrentPage);
    }

    private async void BtnNuevoCheque_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Crear y mostrar el diálogo
            var nuevoCheque = await MostrarFormularioCheque(null);
            
            if (nuevoCheque != null)
            {
                // Guardar en la base de datos
                await GuardarChequeEnBaseDatos(nuevoCheque);
                
                // Actualizar la interfaz
                await ActualizarInterfazDespuesDeGuardar(nuevoCheque);
            }
        }
        catch (Exception ex)
        {
            await MostrarError("Error al crear cheque", ex.Message);
        }
    }

    private async Task<Cheques> MostrarFormularioCheque(Cheques chequeParaEditar)
    {
        // Validar que podemos mostrar diálogos
        if (this.XamlRoot == null)
        {
            await MostrarError("Error", "No se puede mostrar el formulario en este momento.");
            return null;
        }

        bool esEdicion = chequeParaEditar != null;

        // Sugerir automáticamente el próximo número de cheque cuando no es edición
        string numeroSugerido = null;
        if (!esEdicion)
        {
            try
            {
                numeroSugerido = await GetNextChequeNumberAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CHEQUES] Error obteniendo número sugerido: {ex.Message}");
            }
        }
        
        // Crear controles del formulario
        var (numeroBox, nombreBox, montoBox, fechaPicker, conceptoBox, montoLetrasBox) = CrearControlesFormulario(chequeParaEditar, numeroSugerido);

        // Configurar conversión automática de monto a letras
        ConfigurarConversionMontoALetras(montoBox, montoLetrasBox, chequeParaEditar);

        // Crear el diálogo
        var dialog = CrearDialogoCheque(esEdicion, numeroBox, nombreBox, montoBox, fechaPicker, conceptoBox, montoLetrasBox);

        // Variables para capturar el resultado
        Cheques chequeResultado = null;
        string mensajeError = null;

        // Manejar validación y creación antes de cerrar el diálogo
        dialog.PrimaryButtonClick += async (s, args) =>
        {
            var deferral = args.GetDeferral();
            
            try
            {
                // Capturar valores del formulario
                var datosFormulario = CapturarDatosFormulario(numeroBox, nombreBox, montoBox, fechaPicker, conceptoBox);

                // Validar datos
                mensajeError = ValidarDatosCheque(datosFormulario);

                if (string.IsNullOrEmpty(mensajeError))
                {
                    // Verificar número duplicado
                    mensajeError = await VerificarNumeroDuplicado(datosFormulario.Numero, chequeParaEditar);
                }

                if (string.IsNullOrEmpty(mensajeError))
                {
                    // Crear el objeto cheque
                    chequeResultado = CrearObjetoCheque(datosFormulario);
                }
                else
                {
                    args.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                mensajeError = $"Error al procesar el formulario: {ex.Message}";
                args.Cancel = true;
            }
            finally
            {
                deferral.Complete();
            }
        };

        // Mostrar el diálogo
        var resultado = await dialog.ShowAsync();

        // Mostrar error después de cerrar el diálogo si hay alguno
        if (!string.IsNullOrEmpty(mensajeError))
        {
            await Task.Delay(100); // Pequeño delay para evitar conflictos
            await MostrarError("Error", mensajeError);
            return null;
        }

        // Retornar el cheque si todo salió bien
        return resultado == ContentDialogResult.Primary ? chequeResultado : null;
    }

    private async Task<string> GetNextChequeNumberAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();

        var lastCheque = await context.Cheques
            .OrderByDescending(c => c.idCheque)
            .FirstOrDefaultAsync();

        var lastNumber = lastCheque?.numero;
        if (string.IsNullOrWhiteSpace(lastNumber))
        {
            return "000001";
        }

        var digits = new string(lastNumber.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits))
        {
            return "000001";
        }

        int len = digits.Length;
        if (!int.TryParse(digits, out int numeric))
        {
            return "000001";
        }
        numeric++;
        var nextDigits = numeric.ToString().PadLeft(len, '0');

        int firstDigitIndex = lastNumber.IndexOfAny("0123456789".ToCharArray());
        if (firstDigitIndex >= 0)
        {
            int endDigitIndex = firstDigitIndex;
            while (endDigitIndex < lastNumber.Length && char.IsDigit(lastNumber[endDigitIndex]))
                endDigitIndex++;

            var prefix = lastNumber.Substring(0, firstDigitIndex);
            var suffix = lastNumber.Substring(endDigitIndex);
            return prefix + nextDigits + suffix;
        }

        return nextDigits;
    }

    private (TextBox numeroBox, TextBox nombreBox, NumberBox montoBox, CalendarDatePicker fechaPicker, TextBox conceptoBox, TextBox montoLetrasBox) 
        CrearControlesFormulario(Cheques chequeExistente, string numeroSugerido)
    {
        var numeroBox = new TextBox
        {
            Header = "Número de Cheque *",
            PlaceholderText = "Ej: 001234",
            Text = chequeExistente?.numero ?? (numeroSugerido ?? ""),
            MaxLength = 20
        };

        var nombreBox = new TextBox
        {
            Header = "Páguese a la Orden de *",
            PlaceholderText = "Nombre completo o razón social",
            Text = chequeExistente?.nombre ?? "",
            MaxLength = 200
        };

        var montoBox = new NumberBox
        {
            Header = "Monto (RD$) *",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0.01,
            SmallChange = 0.01,
            LargeChange = 100.0,
            Value = chequeExistente?.monto != null ? (double)chequeExistente.monto : 0
        };

        var montoLetrasBox = new TextBox
        {
            Header = "Monto en Letras",
            PlaceholderText = "Se generará automáticamente",
            IsReadOnly = true,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 60
        };

        var fechaPicker = new CalendarDatePicker
        {
            Header = "Fecha del Cheque *",
            Date = chequeExistente?.Fecha != null ? new DateTimeOffset(chequeExistente.Fecha) : DateTimeOffset.Now,
            MaxDate = DateTimeOffset.Now.AddYears(1)
        };

        var conceptoBox = new TextBox
        {
            Header = "Concepto de Pago *",
            PlaceholderText = "Descripción del motivo del pago",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            Text = chequeExistente?.concepto ?? ""
        };

        return (numeroBox, nombreBox, montoBox, fechaPicker, conceptoBox, montoLetrasBox);
    }

    private void ConfigurarConversionMontoALetras(NumberBox montoBox, TextBox montoLetrasBox, Cheques chequeExistente)
    {
        montoBox.ValueChanged += (s, args) =>
        {
            try
            {
                if (montoBox.Value > 0)
                {
                    montoLetrasBox.Text = ConvertirNumeroALetras((decimal)montoBox.Value);
                }
                else
                {
                    montoLetrasBox.Text = "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al convertir monto a letras: {ex.Message}");
                montoLetrasBox.Text = "Error en conversión";
            }
        };

        // Mostrar monto en letras inicial si es edición
        if (chequeExistente?.monto > 0)
        {
            montoLetrasBox.Text = ConvertirNumeroALetras(chequeExistente.monto);
        }
    }

    private ContentDialog CrearDialogoCheque(bool esEdicion, TextBox numeroBox, TextBox nombreBox, 
        NumberBox montoBox, CalendarDatePicker fechaPicker, TextBox conceptoBox, TextBox montoLetrasBox)
    {
        var formPanel = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                numeroBox,
                nombreBox,
                montoBox,
                montoLetrasBox,
                fechaPicker,
                conceptoBox
            }
        };

        var scrollViewer = new ScrollViewer
        {
            Content = formPanel,
            MaxHeight = 600,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(4)
        };

        return new ContentDialog
        {
            Title = esEdicion ? "?? Editar Cheque" : "? Nuevo Cheque",
            Content = scrollViewer,
            PrimaryButtonText = esEdicion ? "Actualizar" : "Guardar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };
    }

    private (string Numero, string Nombre, double Monto, DateTime Fecha, string Concepto) CapturarDatosFormulario(
        TextBox numeroBox, TextBox nombreBox, NumberBox montoBox, CalendarDatePicker fechaPicker, TextBox conceptoBox)
    {
        return (
            Numero: numeroBox.Text?.Trim() ?? "",
            Nombre: nombreBox.Text?.Trim() ?? "",
            Monto: montoBox.Value,
            Fecha: fechaPicker.Date?.DateTime ?? DateTime.Now,
            Concepto: conceptoBox.Text?.Trim() ?? ""
        );
    }

    private string ValidarDatosCheque((string Numero, string Nombre, double Monto, DateTime Fecha, string Concepto) datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Numero))
            return "El número de cheque es obligatorio";

        if (string.IsNullOrWhiteSpace(datos.Nombre))
            return "El nombre del beneficiario es obligatorio";

        if (datos.Monto <= 0 || double.IsNaN(datos.Monto))
            return "El monto debe ser mayor a cero";

        if (string.IsNullOrWhiteSpace(datos.Concepto))
            return "El concepto de pago es obligatorio";

        if (datos.Fecha > DateTime.Now.AddYears(1))
            return "La fecha no puede ser superior a un año";

        return null; // Sin errores
    }

    private async Task<string> VerificarNumeroDuplicado(string numero, Cheques chequeExistente)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();

            bool existeNumero;

            if (chequeExistente == null) // Nuevo cheque
            {
                existeNumero = await context.Cheques.AnyAsync(c => c.numero == numero);
            }
            else // Edición
            {
                existeNumero = await context.Cheques.AnyAsync(c => c.numero == numero && c.idCheque != chequeExistente.idCheque);
            }

            return existeNumero ? "Ya existe un cheque con este número" : null;
        }
        catch (Exception ex)
        {
            return $"Error al verificar número duplicado: {ex.Message}";
        }
    }

    private Cheques CrearObjetoCheque((string Numero, string Nombre, double Monto, DateTime Fecha, string Concepto) datos)
    {
        return new Cheques
        {
            numero = datos.Numero,
            nombre = datos.Nombre,
            monto = (decimal)datos.Monto,
            Fecha = datos.Fecha,
            concepto = datos.Concepto
        };
    }

    private async Task GuardarChequeEnBaseDatos(Cheques cheque)
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();

        context.Cheques.Add(cheque);
        await context.SaveChangesAsync();
    }

    private async Task ActualizarInterfazDespuesDeGuardar(Cheques cheque)
    {
        // Invalidar caché y recargar datos
        _cacheService.InvalidateCache("cheques");
        await LoadPageAsync(CurrentPage);

        // Mostrar mensaje de éxito
        await MostrarExito("Cheque Creado", 
            $"El cheque ha sido registrado exitosamente.\n\n" +
            $"?? Número: {cheque.numero}\n" +
            $"?? Beneficiario: {cheque.nombre}\n" +
            $"?? Monto: RD$ {cheque.monto:N2}");
    }

    private async Task MostrarError(string titulo, string mensaje)
    {
        await ShowInfoDialog($"? {titulo}", mensaje);
    }

    private async Task MostrarExito(string titulo, string mensaje)
    {
        await ShowInfoDialog($"? {titulo}", mensaje);
    }

    private async void BtnEditarCheque_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validar que hay un cheque seleccionado
            var chequeSeleccionado = ChequesListView?.SelectedItem as Cheques;
            if (chequeSeleccionado == null)
            {
                await MostrarError("Sin Selección", "Debe seleccionar un cheque para editar");
                return;
            }

            // Mostrar formulario con datos existentes
            var chequeEditado = await MostrarFormularioCheque(chequeSeleccionado);
            
            if (chequeEditado != null)
            {
                // Actualizar en la base de datos
                await ActualizarChequeEnBaseDatos(chequeSeleccionado.idCheque, chequeEditado);
                
                // Actualizar la interfaz
                await ActualizarInterfazDespuesDeActualizar(chequeEditado);
            }
        }
        catch (Exception ex)
        {
            await MostrarError("Error al editar cheque", ex.Message);
        }
    }

    private async Task ActualizarChequeEnBaseDatos(int idCheque, Cheques datosNuevos)
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();

        // Cargar entidad existente (trackeada)
        var chequeExistente = await context.Cheques.FirstOrDefaultAsync(c => c.idCheque == idCheque);
        if (chequeExistente == null)
        {
            throw new InvalidOperationException("El cheque ya no existe en la base de datos");
        }

        // Aplicar nuevos valores a la entidad trackeada
        chequeExistente.numero = datosNuevos.numero.Trim();
        chequeExistente.nombre = datosNuevos.nombre.Trim();
        chequeExistente.monto = datosNuevos.monto;
        chequeExistente.Fecha = datosNuevos.Fecha;
        chequeExistente.concepto = datosNuevos.concepto?.Trim();

        // Marcar propiedades como modificadas explícitamente (por si el ChangeTracker no detecta cambios)
        var entry = context.Entry(chequeExistente);
        entry.Property(e => e.numero).IsModified = true;
        entry.Property(e => e.nombre).IsModified = true;
        entry.Property(e => e.monto).IsModified = true;
        entry.Property(e => e.Fecha).IsModified = true;
        entry.Property(e => e.concepto).IsModified = true;

        // Guardar cambios y validar resultado
        var affected = await context.SaveChangesAsync().ConfigureAwait(false);
        if (affected == 0)
        {
            // Forzar actualización si por alguna razón no se detectaron cambios
            context.Update(chequeExistente);
            affected = await context.SaveChangesAsync().ConfigureAwait(false);
        }

        // Invalidar caché para que la UI no muestre datos viejos
        _cacheService.InvalidateCache("cheques");
    }

    private async Task ActualizarInterfazDespuesDeActualizar(Cheques cheque)
    {
        // Invalidar caché y recargar datos
        _cacheService.InvalidateCache("cheques");
        await LoadPageAsync(CurrentPage);

        // Mostrar mensaje de éxito
        await MostrarExito("Cheque Actualizado", 
            $"El cheque ha sido actualizado exitosamente.\n\n" +
            $"?? Número: {cheque.numero}\n" +
            $"?? Beneficiario: {cheque.nombre}\n" +
            $"?? Monto: RD$ {cheque.monto:N2}");
    }

    private async void BtnEliminarCheque_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validar XamlRoot
            if (this.XamlRoot == null)
            {
                System.Diagnostics.Debug.WriteLine("[BTN-ELIMINAR] ERROR: XamlRoot es null");
                return;
            }

            var chequeSeleccionado = ChequesListView?.SelectedItem as Cheques;
            if (chequeSeleccionado == null)
            {
                await ShowInfoDialog("Error", "Debe seleccionar un cheque");
                return;
            }

            var confirmDialog = new ContentDialog
            {
                Title = "Confirmar Eliminación",
                Content = $"¿Está seguro que desea eliminar este cheque?\n\n" +
                          $"Nº Cheque: {chequeSeleccionado.numero}\n" +
                          $"Páguese a: {chequeSeleccionado.nombre}\n" +
                          $"Monto: ${chequeSeleccionado.monto:N2}\n\n" +
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

                    var cheque = await context.Cheques.FindAsync(chequeSeleccionado.idCheque).ConfigureAwait(false);
                    if (cheque != null)
                    {
                        context.Cheques.Remove(cheque);
                        await context.SaveChangesAsync().ConfigureAwait(false);

                        _cacheService.InvalidateCache("cheques");
                        await LoadPageAsync(CurrentPage);
                        
                        await DispatcherQueue.EnqueueAsync(async () =>
                        {
                            await ShowInfoDialog("Éxito", "Cheque eliminado correctamente");
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BTN-ELIMINAR] Error al eliminar: {ex.Message}");
                    await DispatcherQueue.EnqueueAsync(async () =>
                    {
                        await ShowInfoDialog("Error", $"Error al eliminar cheque: {ex.Message}");
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

    private async void BtnImprimirCheque_Click(object sender, RoutedEventArgs e)
    {
        var chequeSeleccionado = ChequesListView.SelectedItem as Cheques;
        if (chequeSeleccionado == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar un cheque");
            return;
        }

        var configuring = await MostrarDialogoConfiguracionImpresion();
        if (!configuring)
        {
            return;
        }

        var impresoraSeleccionada = await MostrarDialogoSeleccionImpresora();
        if (string.IsNullOrEmpty(impresoraSeleccionada))
        {
            return;
        }

        try
        {
            PrintDocument printDoc = new PrintDocument();
            printDoc.PrinterSettings.PrinterName = impresoraSeleccionada;

            // Imprimir en retrato (portrait)
            printDoc.DefaultPageSettings.Landscape = false;
            printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            printDoc.OriginAtMargins = false;

            // Usar tamaño de página Media Carta en retrato: 8.5 x 5.5 pulgadas
            int InchesToHundredths(float inches) => (int)Math.Round(inches * 100f);
            var halfLetterSize = new PaperSize("MediaCarta", InchesToHundredths(8.5f), InchesToHundredths(5.5f))
            {
                RawKind = (int)PaperKind.Custom
            };
            printDoc.DefaultPageSettings.PaperSize = halfLetterSize;

            printDoc.PrintPage += (s, ev) => PrintCheque(s, ev, chequeSeleccionado);

            if (!printDoc.PrinterSettings.IsValid)
            {
                await ShowInfoDialog("Error", $"La impresora '{impresoraSeleccionada}' no está disponible.");
                return;
            }

            printDoc.Print();
            await ShowInfoDialog("Éxito", $"Cheque enviado a la impresora '{impresoraSeleccionada}' correctamente");
        }
        catch (Exception ex)
        {
            await ShowInfoDialog("Error", $"Error al imprimir: {ex.Message}");
        }
    }

    private async Task<string> MostrarDialogoSeleccionImpresora()
    {
        var impresoras = PrinterSettings.InstalledPrinters;

        if (impresoras.Count == 0)
        {
            await ShowInfoDialog("Error", "No hay impresoras instaladas en el sistema.");
            return null;
        }

        var listaImpresoras = new System.Collections.Generic.List<string>();
        foreach (string impresora in impresoras)
        {
            listaImpresoras.Add(impresora);
        }

        var impresoraPredeterminada = new PrinterSettings().PrinterName;

        var comboBox = new ComboBox
        {
            Header = "Seleccione la impresora:",
            ItemsSource = listaImpresoras,
            SelectedItem = impresoraPredeterminada,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var infoText = new TextBlock
        {
            Text = $"Impresora predeterminada: {impresoraPredeterminada}",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
            FontSize = 12
        };

        var panel = new StackPanel
        {
            Spacing = 8,
            Children = { infoText, comboBox }
        };

        var dialog = new ContentDialog
        {
            Title = "Seleccionar Impresora",
            Content = panel,
            PrimaryButtonText = "Imprimir",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && comboBox.SelectedItem != null)
        {
            return comboBox.SelectedItem.ToString();
        }

        return null;
    }

    private void PrintCheque(object sender, PrintPageEventArgs e, Cheques cheque)
    {
        try
        {
            e.PageSettings.Margins = new Margins(0, 0, 0, 0);

            float HundredthsInchToMm(float hundredths) => hundredths / 100f * 25.4f;
            // Compensar márgenes duros de la Epson LX-350 para que el lado izquierdo se imprima
            float hardXmm = HundredthsInchToMm(e.PageSettings.HardMarginX);
            float hardYmm = HundredthsInchToMm(e.PageSettings.HardMarginY);
            e.Graphics.PageUnit = GraphicsUnit.Millimeter;
            e.Graphics.TranslateTransform(-hardXmm, -hardYmm);

            // 1) Dibujar imagen de fondo del cheque (JPG) antes de los textos
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var imagePath = System.IO.Path.Combine(baseDir, "Assets", "cheque_fondo.jpg");
                if (System.IO.File.Exists(imagePath))
                {
                    using var fondo = System.Drawing.Image.FromFile(imagePath);
                    // Dibujar a tamaño página en retrato Media Carta: 215.9mm x 139.7mm
                    const float pageWidthMm = 215.9f;  // 8.5"
                    const float pageHeightMm = 139.7f; // 5.5"
                    e.Graphics.DrawImage(fondo, new System.Drawing.RectangleF(0, 0, pageWidthMm, pageHeightMm));
                }
            }
            catch (Exception imgEx)
            {
                System.Diagnostics.Debug.WriteLine($"[PrintCheque] Error dibujando imagen de fondo: {imgEx.Message}");
            }

            // 2) Dibujar los textos encima
            using Font font = new Font("Courier New", 11, System.Drawing.FontStyle.Regular);
            Brush brush = Brushes.Black;

            e.Graphics.DrawString(cheque.nombre, font, brush, nombreX, nombreY);
            DrawDateDigitsSeparated(e.Graphics, cheque.Fecha, fechaX, fechaY, font, brush);
            string montoEnLetras = ConvertirNumeroALetras(cheque.monto);
            e.Graphics.DrawString(montoEnLetras, font, brush, letraX, letraY);
            e.Graphics.DrawString(cheque.monto.ToString("N2"), font, brush, montoX, montoY);
            e.Graphics.DrawString(cheque.concepto, font, brush, conceptoX, conceptoY);
            PrintCircles(e.Graphics, cheque.Fecha, cheque.monto, font, brush);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en PrintCheque: {ex.Message}");
        }
    }

    private void PrintCircles(Graphics graphics, DateTime fecha, decimal monto, Font font, Brush brush)
    {
        try
        {
            float fechaXLeft = fechaCirculoX;
            float fechaYLine = fechaCirculoY;
            // Usar una fuente más pequeña para la fecha del círculo
            using var fechaFont = new Font(font.FontFamily, fechaCirculoFontSize, font.Style);
            graphics.DrawString(fecha.ToString("dd/MM/yyyy"), fechaFont, brush, fechaXLeft, fechaYLine);

            var fmt = StringFormat.GenericTypographic;
            var montoText = monto.ToString("N2");
            var size = graphics.MeasureString(montoText, font, int.MaxValue, fmt);
            float rightBoundaryX = montoCirculoX;
            float montoXRightAligned = rightBoundaryX - size.Width;
            float montoYLine = montoCirculoY;
            graphics.DrawString(montoText, font, brush, montoXRightAligned, montoYLine, fmt);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al imprimir fecha y monto inferiores: {ex.Message}");
        }
    }

    private void DrawDateDigitsSeparated(Graphics graphics, DateTime fecha, float startX, float startY, Font font, Brush brush)
    {
        int dia = fecha.Day;
        int mes = fecha.Month;
        int year = fecha.Year;

        // Espaciado configurable entre dígitos
        float stepX = fechaStepX;
        float stepY = fechaStepY;

        graphics.DrawString((dia / 10).ToString(), font, brush, startX, startY);
        graphics.DrawString((dia % 10).ToString(), font, brush, startX + stepX, startY + stepY);

        graphics.DrawString((mes / 10).ToString(), font, brush, startX + stepX * 2, startY + stepY * 2);
        graphics.DrawString((mes % 10).ToString(), font, brush, startX + stepX * 3, startY + stepY * 3);

        graphics.DrawString((year / 1000).ToString(), font, brush, startX + stepX * 4, startY + stepY * 4);
        graphics.DrawString(((year % 1000) / 100).ToString(), font, brush, startX + stepX * 5, startY + stepY * 5);
        graphics.DrawString((((year % 1000) % 100) / 10).ToString(), font, brush, startX + stepX * 6, startY + stepY * 6);
        graphics.DrawString((((year % 1000) % 100) % 10).ToString(), font, brush, startX + stepX * 7, startY + stepY * 7);
    }

    private async Task<bool> MostrarDialogoConfiguracionImpresion()
    {
        var infoText = new TextBlock
        {
            Text = "Ajuste las posiciones de los campos en el cheque (en milímetros).",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var nombreXBox = new NumberBox { Header = "Nombre - Posición X (mm)", Value = nombreX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var nombreYBox = new NumberBox { Header = "Nombre - Posición Y (mm)", Value = nombreY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var fechaXBox = new NumberBox { Header = "Fecha - Posición X (mm)", Value = fechaX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var fechaYBox = new NumberBox { Header = "Fecha - Posición Y (mm)", Value = fechaY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var fechaStepXBox = new NumberBox { Header = "Fecha - Espaciado X (mm)", Value = fechaStepX, Minimum = 0, Maximum = 20, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var fechaStepYBox = new NumberBox { Header = "Fecha - Espaciado Y (mm)", Value = fechaStepY, Minimum = -5, Maximum = 5, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var letraXBox = new NumberBox { Header = "Monto en Letras - Posición X (mm)", Value = letraX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var letraYBox = new NumberBox { Header = "Monto en Letras - Posición Y (mm)", Value = letraY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var montoXBox = new NumberBox { Header = "Monto Numérico - Posición X (mm)", Value = montoX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var montoYBox = new NumberBox { Header = "Monto Numérico - Posición Y (mm)", Value = montoY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var conceptoXBox = new NumberBox { Header = "Concepto - Posición X (mm)", Value = conceptoX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var conceptoYBox = new NumberBox { Header = "Concepto - Posición Y (mm)", Value = conceptoY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };

        // Campos adicionales para impresión en círculo (inferior)
        var fechaCirculoXBox = new NumberBox { Header = "FechaCírculo - Posición X (mm)", Value = fechaCirculoX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var fechaCirculoYBox = new NumberBox { Header = "FechaCírculo - Posición Y (mm)", Value = fechaCirculoY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var fechaCirculoFontBox = new NumberBox { Header = "FechaCírculo - Tamaño de Fuente (pt)", Value = fechaCirculoFontSize, Minimum = 6, Maximum = 20, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var montoCirculoXBox = new NumberBox { Header = "MontoCírculo - Posición X (mm)", Value = montoCirculoX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var montoCirculoYBox = new NumberBox { Header = "MontoCírculo - Posición Y (mm)", Value = montoCirculoY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };

        var formPanel = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                infoText,
                nombreXBox, nombreYBox,
                fechaXBox, fechaYBox, fechaStepXBox, fechaStepYBox,
                letraXBox, letraYBox,
                montoXBox, montoYBox,
                conceptoXBox, conceptoYBox,
                fechaCirculoXBox, fechaCirculoYBox, fechaCirculoFontBox,
                montoCirculoXBox, montoCirculoYBox
            }
        };

        var scrollViewer = new ScrollViewer
        {
            Content = formPanel,
            MaxHeight = 500,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var dialog = new ContentDialog
        {
            Title = "Configurar Impresión de Cheque",
            Content = scrollViewer,
            PrimaryButtonText = "Imprimir",
            SecondaryButtonText = "Usar Valores Predeterminados",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            nombreX = (float)nombreXBox.Value;
            nombreY = (float)nombreYBox.Value;
            fechaX = (float)fechaXBox.Value;
            fechaY = (float)fechaYBox.Value;
            fechaStepX = (float)fechaStepXBox.Value;
            fechaStepY = (float)fechaStepYBox.Value;
            letraX = (float)letraXBox.Value;
            letraY = (float)letraYBox.Value;
            montoX = (float)montoXBox.Value;
            montoY = (float)montoYBox.Value;
            conceptoX = (float)conceptoXBox.Value;
            conceptoY = (float)conceptoYBox.Value;
            fechaCirculoX = (float)fechaCirculoXBox.Value;
            fechaCirculoY = (float)fechaCirculoYBox.Value;
            fechaCirculoFontSize = (float)fechaCirculoFontBox.Value;
            montoCirculoX = (float)montoCirculoXBox.Value;
            montoCirculoY = (float)montoCirculoYBox.Value;
            return true;
        }
        else if (result == ContentDialogResult.Secondary)
        {
            return true;
        }

        return false;
    }

    private string ConvertirNumeroALetras(decimal numero)
    {
        if (numero == 0) return "CERO PESOS 00/100";
        if (numero < 0) return "Número inválido";

        int parteEntera = (int)numero;
        int centavos = (int)Math.Round((numero - parteEntera) * 100);

        string resultado = ConvertirEnteroALetras(parteEntera);
        return $"{resultado} PESOS {centavos:00}/100";
    }

    private string ConvertirEnteroALetras(int numero)
    {
        if (numero == 0) return "CERO";

        if (numero < 0) return "Número inválido";

        if (numero >= 1000000)
        {
            int millones = numero / 1000000;
            int resto = numero % 1000000;

            string textoMillones = millones == 1
                ? "UN MILLON"
                : ConvertirEnteroALetras(millones) + " MILLONES";

            if (resto > 0)
            {
                return textoMillones + " " + ConvertirEnteroALetras(resto);
            }
            return textoMillones;
        }

        if (numero >= 1000)
        {
            int miles = numero / 1000;
            int resto = numero % 1000;

            string textoMiles = miles == 1
                ? "MIL"
                : ConvertirEnteroALetras(miles) + " MIL";

            if (resto > 0)
            {
                return textoMiles + " " + ConvertirEnteroALetras(resto);
            }
            return textoMiles;
        }

        if (numero >= 100)
        {
            return ConvertirCentenas(numero);
        }

        return ConvertirDecenas(numero);
    }

    private string ConvertirCentenas(int numero)
    {
        string[] famosas = {
            "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS",
            "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"
        };

        int c = numero / 100;
        int resto = numero % 100;

        if (numero == 100)
        {
            return "Cien";
        }

        string resultado = famosas[c];

        if (resto > 0)
        {
            resultado += " " + ConvertirDecenas(resto);
        }

        return resultado;
    }

    private string ConvertirDecenas(int numero)
    {
        string[] unidades = {
            "", "UN", "DOS", "TRES", "CUATRO",
            "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE"
        };

        string[] decenas = {
            "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA",
            "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"
        };

        string[] especiales = {
            "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE",
            "DIECISEIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE"
        };

        if (numero < 10)
        {
            return unidades[numero];
        }

        if (numero < 20)
        {
            return especiales[numero - 10];
        }

        if (numero < 30)
        {
            int u = numero % 10;
            if (u == 0)
                return "VEINTE";
            else
                return "VEINTI" + unidades[u].ToLower();
        }

        if (numero < 100)
        {
            int d = numero / 10;
            int u = numero % 10;

            if (u == 0)
                return decenas[d];
            else
                return decenas[d] + " Y " + unidades[u];
        }

        return "";
    }

    private void ChequesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsChequeSelected = ChequesListView?.SelectedItem != null;

        var haySeleccion = IsChequeSelected;

        if (this.FindName("btnEditar") is Button editBtn)
            editBtn.IsEnabled = haySeleccion;

        if (this.FindName("btnEliminar") is Button delBtn)
            delBtn.IsEnabled = haySeleccion;

        if (this.FindName("btnImprimir") is Button printBtn)
            printBtn.IsEnabled = haySeleccion;
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

    public void Dispose()
    {
        try
        {
            _isPageActive = false;
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource?.Dispose();
            _searchDelayTimer?.Dispose();
            _loadingSemaphore?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in Dispose: {ex.Message}");
        }
    }
}
