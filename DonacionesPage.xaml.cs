using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RamaFemenina.Models;
using RamaFemenina.Data;

namespace RamaFemenina;

public sealed partial class DonacionesPage : Page, INotifyPropertyChanged
{
    private readonly RamaFemeninaContext _context;
    private bool _isDonacionSelected;
    private bool _datosYaCargados = false;
    
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

    public ObservableCollection<Donaciones> DonacionesCollection { get; set; }
    public ObservableCollection<Donaciones> DonacionesFiltradas { get; set; }
    public ObservableCollection<Paciente> Pacientes { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public DonacionesPage()
    {
        InitializeComponent();
        
        // Habilitar caché de navegación
        NavigationCacheMode = NavigationCacheMode.Enabled;
        
        var app = Application.Current as App;
        _context = app!.Services.GetRequiredService<RamaFemeninaContext>();
        
        DonacionesCollection = new ObservableCollection<Donaciones>();
        DonacionesFiltradas = new ObservableCollection<Donaciones>();
        Pacientes = new ObservableCollection<Paciente>();
        
        // Cargar datos solo si no se han cargado antes
        if (!_datosYaCargados)
        {
            _ = CargarDatosAsync();
        }
        
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

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        // Solo recargar si es la primera vez o si se pasa un parámetro para forzar recarga
        if (!_datosYaCargados || e.Parameter?.ToString() == "Reload")
        {
            _ = CargarDatosAsync();
        }
    }

    private async Task CargarDatosAsync()
    {
        try
        {
            // Cargar pacientes
            Pacientes.Clear();
            var pacientes = await _context.Pacientes.ToListAsync();
            foreach (var paciente in pacientes)
            {
                Pacientes.Add(paciente);
            }

            // Cargar donaciones
            DonacionesCollection.Clear();
            var donaciones = await _context.Donaciones.OrderByDescending(d => d.Fecha).ToListAsync();
            foreach (var donacion in donaciones)
            {
                DonacionesCollection.Add(donacion);
            }

            ActualizarListaFiltrada();
            ActualizarEstadisticas();
            
            // Controlar visibilidad
            var hayDonaciones = DonacionesCollection.Count > 0;
            if (this.FindName("ListViewScroller") is UIElement listScroller)
                listScroller.Visibility = hayDonaciones ? Visibility.Visible : Visibility.Collapsed;
            EmptyState.Visibility = hayDonaciones ? Visibility.Collapsed : Visibility.Visible;
            
            // Marcar que los datos ya fueron cargados
            _datosYaCargados = true;
        }
        catch (Exception ex)
        {
            await ShowInfoDialog("Error", $"Error al cargar datos: {ex.Message}");
        }
    }

    private void ActualizarEstadisticas()
    {
        try
        {
            // Total de donaciones
            if (this.FindName("txtTotalDonaciones") is TextBlock totalText)
                totalText.Text = DonacionesCollection.Count.ToString();
                
            if (this.FindName("txtContador") is Microsoft.UI.Xaml.Documents.Run contadorRun)
                contadorRun.Text = DonacionesCollection.Count.ToString();
            
            // Calcular totales
            var totalSolicitado = DonacionesCollection.Sum(d => d.montoSolicitado);
            var totalDonado = DonacionesCollection.Sum(d => d.total);
            var diferencia = totalSolicitado - totalDonado;
            
            if (this.FindName("txtTotalSolicitado") is TextBlock solicitadoText)
                solicitadoText.Text = $"RD$ {totalSolicitado:N2}";
            
            if (this.FindName("txtTotalDonado") is TextBlock donadoText)
                donadoText.Text = $"RD$ {totalDonado:N2}";
            
            if (this.FindName("txtDiferencia") is TextBlock diferenciaText)
                diferenciaText.Text = $"RD$ {diferencia:N2}";
            
            // Porcentaje completado
            var porcentaje = totalSolicitado > 0 ? ((double)totalDonado / (double)totalSolicitado) * 100 : 0;
            if (this.FindName("txtPorcentaje") is TextBlock porcentajeText)
                porcentajeText.Text = $"{porcentaje:F1}% completado";
            
            if (this.FindName("progressSolicitado") is ProgressBar progress)
                progress.Value = Math.Min(porcentaje, 100);
        }
        catch
        {
            // Ignorar errores de estadísticas
        }
    }

    private void ActualizarListaFiltrada(string searchText = "")
    {
        if (DonacionesFiltradas == null || DonacionesCollection == null) return;
        
        DonacionesFiltradas.Clear();
        
        var donacionesFiltradas = string.IsNullOrWhiteSpace(searchText)
            ? DonacionesCollection
            : DonacionesCollection.Where(d =>
                (d.procedimiento != null && d.procedimiento.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (d.observacion != null && d.observacion.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (d.idPaciente != null && d.idPaciente.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                d.valor.ToString().Contains(searchText) ||
                d.montoSolicitado.ToString().Contains(searchText) ||
                d.idDonacion.ToString().Contains(searchText));

        foreach (var donacion in donacionesFiltradas)
        {
            DonacionesFiltradas.Add(donacion);
        }

        DonacionesListView.ItemsSource = DonacionesFiltradas;
        
        // Actualizar contador con resultados filtrados
        if (this.FindName("txtContador") is Microsoft.UI.Xaml.Documents.Run contadorRun)
            contadorRun.Text = DonacionesFiltradas.Count.ToString();
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ActualizarListaFiltrada(sender.Text);
        }
    }

    private async void BtnNuevaDonacion_Click(object sender, RoutedEventArgs e)
    {
        if (Pacientes.Count == 0)
        {
            await ShowInfoDialog("Advertencia", "No hay pacientes registrados. Por favor, registre un paciente primero.");
            return;
        }

        var resultado = await MostrarDialogoDonacion(null);
        if (resultado != null)
        {
            try
            {
                _context.Donaciones.Add(resultado);
                await _context.SaveChangesAsync();

                await CargarDatosAsync();
                await ShowInfoDialog("Éxito", $"Donación registrada correctamente.\nID: {resultado.idDonacion}\nTotal: ${resultado.total:N2}");
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al guardar donación: {ex.Message}");
            }
        }
    }

    private async void BtnEditarDonacion_Click(object sender, RoutedEventArgs e)
    {
        var donacionSeleccionada = DonacionesListView.SelectedItem as Donaciones;
        if (donacionSeleccionada == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar una donación");
            return;
        }

        var resultado = await MostrarDialogoDonacion(donacionSeleccionada);
        if (resultado != null)
        {
            try
            {
                var donacion = await _context.Donaciones.FindAsync(donacionSeleccionada.idDonacion);
                if (donacion != null)
                {
                    donacion.Fecha = resultado.Fecha;
                    donacion.idPaciente = resultado.idPaciente;
                    donacion.procedimiento = resultado.procedimiento;
                    donacion.observacion = resultado.observacion;
                    donacion.montoSolicitado = resultado.montoSolicitado;
                    donacion.valor = resultado.valor;
                    donacion.total = resultado.total;

                    await _context.SaveChangesAsync();
                    await CargarDatosAsync();
                    await ShowInfoDialog("Éxito", "Donación actualizada correctamente");
                }
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al actualizar donación: {ex.Message}");
            }
        }
    }

    private async void BtnEliminarDonacion_Click(object sender, RoutedEventArgs e)
    {
        var donacionSeleccionada = DonacionesListView.SelectedItem as Donaciones;
        if (donacionSeleccionada == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar una donación");
            return;
        }

        var confirmDialog = new ContentDialog
        {
            Title = "Confirmar Eliminación",
            Content = $"¿Está seguro que desea eliminar esta donación?\n\n" +
                      $"ID Donación: {donacionSeleccionada.idDonacion}\n" +
                      $"Paciente: {donacionSeleccionada.idPaciente}\n" +
                      $"Procedimiento: {donacionSeleccionada.procedimiento}\n" +
                      $"Monto: ${donacionSeleccionada.total:N2}\n\n" +
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
                var donacion = await _context.Donaciones.FindAsync(donacionSeleccionada.idDonacion);
                if (donacion != null)
                {
                    _context.Donaciones.Remove(donacion);
                    await _context.SaveChangesAsync();
                    await CargarDatosAsync();
                    await ShowInfoDialog("Éxito", "Donación eliminada correctamente");
                }
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al eliminar donación: {ex.Message}");
            }
        }
    }

    private async void BtnVerPaciente_Click(object sender, RoutedEventArgs e)
    {
        var donacionSeleccionada = DonacionesListView.SelectedItem as Donaciones;
        if (donacionSeleccionada == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar una donación");
            return;
        }

        try
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.cedula == donacionSeleccionada.idPaciente);

            if (paciente != null)
            {
                var donacionesPaciente = await _context.Donaciones
                    .Where(d => d.idPaciente == paciente.cedula)
                    .ToListAsync();

                var totalDonado = donacionesPaciente.Sum(d => d.total);
                var totalSolicitado = donacionesPaciente.Sum(d => d.montoSolicitado);

                await ShowInfoDialog("Información del Paciente",
                    $"Cédula: {paciente.cedula}\n" +
                    $"Nombre: {paciente.nombre}\n" +
                    $"Teléfono: {paciente.telefono ?? "N/A"}\n" +
                    $"Celular: {paciente.celular ?? "N/A"}\n" +
                    $"Área: {paciente.area ?? "N/A"}\n\n" +
                    $"DONACIONES:\n" +
                    $"Total de donaciones: {donacionesPaciente.Count}\n" +
                    $"Monto solicitado: ${totalSolicitado:N2}\n" +
                    $"Monto donado: ${totalDonado:N2}");
            }
            else
            {
                await ShowInfoDialog("Error", "No se encontró el paciente asociado");
            }
        }
        catch (Exception ex)
        {
            await ShowInfoDialog("Error", $"Error al cargar información del paciente: {ex.Message}");
        }
    }

    private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
    {
        _datosYaCargados = false;
        await CargarDatosAsync();
    }

    private async Task<Donaciones> MostrarDialogoDonacion(Donaciones donacionExistente)
    {
        bool esEdicion = donacionExistente != null;

        var pacienteCombo = new ComboBox
        {
            Header = "Paciente *",
            PlaceholderText = "Seleccione un paciente",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = Pacientes
        };

        // Crear el DisplayMemberPath manualmente porque necesitamos mostrar cédula y nombre
        pacienteCombo.ItemTemplate = new DataTemplate();
        var factory = Microsoft.UI.Xaml.Markup.XamlReader.Load(
            @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                <StackPanel Orientation='Horizontal' Spacing='8'>
                    <TextBlock Text='{Binding cedula}' FontWeight='SemiBold'/>
                    <TextBlock Text='-'/>
                    <TextBlock Text='{Binding nombre}'/>
                </StackPanel>
            </DataTemplate>") as DataTemplate;
        pacienteCombo.ItemTemplate = factory;

        if (esEdicion)
        {
            pacienteCombo.SelectedItem = Pacientes.FirstOrDefault(p => p.cedula == donacionExistente.idPaciente);
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
            totalBox.Value = valorDonacionBox.Value;
            
            if (montoSolicitadoBox.Value > 0)
            {
                var porcentaje = (valorDonacionBox.Value / montoSolicitadoBox.Value) * 100;
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

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (pacienteCombo.SelectedItem == null)
            {
                await ShowInfoDialog("Error", "Debe seleccionar un paciente");
                return null;
            }

            if (!fechaPicker.Date.HasValue)
            {
                await ShowInfoDialog("Error", "Debe seleccionar una fecha");
                return null;
            }

            if (string.IsNullOrWhiteSpace(procedimientoBox.Text))
            {
                await ShowInfoDialog("Error", "El procedimiento es obligatorio");
                return null;
            }

            if (montoSolicitadoBox.Value <= 0 || double.IsNaN(montoSolicitadoBox.Value))
            {
                await ShowInfoDialog("Error", "Debe ingresar un monto solicitado válido");
                return null;
            }

            var pacienteSeleccionado = pacienteCombo.SelectedItem as Paciente;

            return new Donaciones
            {
                Fecha = fechaPicker.Date.Value.DateTime,
                idPaciente = pacienteSeleccionado.cedula,
                procedimiento = procedimientoBox.Text.Trim(),
                montoSolicitado = (decimal)montoSolicitadoBox.Value,
                valor = (decimal)valorDonacionBox.Value,
                total = (decimal)totalBox.Value,
                observacion = observacionBox.Text.Trim()
            };
        }

        return null;
    }

    private void DonacionesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsDonacionSelected = DonacionesListView.SelectedItem != null;
        
        // Actualizar estado de botones directamente
        var haySeleccion = IsDonacionSelected;
        
        if (this.FindName("btnEditar") is Button editBtn)
            editBtn.IsEnabled = haySeleccion;
            
        if (this.FindName("btnEliminar") is Button delBtn)
            delBtn.IsEnabled = haySeleccion;
            
        if (this.FindName("btnVerPaciente") is Button verBtn)
            verBtn.IsEnabled = haySeleccion;
    }

    private async Task ShowInfoDialog(string title, string message)
    {
        // Crear contenido mejorado
        var contentStack = new StackPanel
        {
            Spacing = 12,
            MaxWidth = 450
        };

        // Icono según el tipo de mensaje
        string iconGlyph = "\uE946"; // Info por defecto
        Microsoft.UI.Xaml.Media.Brush iconColor = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorAttentionBrush"];

        if (title.Contains("Error") || title.Contains("?"))
        {
            iconGlyph = "\uE783"; // Error
            iconColor = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        }
        else if (title.Contains("Éxito") || title.Contains("?") || title.Contains("??"))
        {
            iconGlyph = "\uE73E"; // Checkmark
            iconColor = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
        }
        else if (title.Contains("Información") || title.Contains("??"))
        {
            iconGlyph = "\uE946"; // Info
            iconColor = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
        }

        var iconBorder = new Border
        {
            Width = 56,
            Height = 56,
            CornerRadius = new CornerRadius(28),
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var icon = new FontIcon
        {
            Glyph = iconGlyph,
            FontSize = 28,
            Foreground = iconColor,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        iconBorder.Child = icon;

        var messageText = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            FontSize = 14,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
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
}
