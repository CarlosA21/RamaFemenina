using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace RamaFemenina;

public sealed partial class PacientesPage : Page, INotifyPropertyChanged
{
    private readonly RamaFemeninaContext _context;
    private bool _isPatientSelected;
    private bool _datosYaCargados = false;
    
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

    public ObservableCollection<Paciente> Pacientes { get; set; }
    public ObservableCollection<Paciente> PacientesFiltrados { get; set; }
    public ObservableCollection<Donaciones> Donaciones { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public PacientesPage()
    {
        InitializeComponent();
        
        // Habilitar caché de navegación
        NavigationCacheMode = NavigationCacheMode.Enabled;

        var app = Application.Current as App;
        _context = app!.Services.GetRequiredService<RamaFemeninaContext>();
        
        Pacientes = new ObservableCollection<Paciente>();
        PacientesFiltrados = new ObservableCollection<Paciente>();
        Donaciones = new ObservableCollection<Donaciones>();
        
        // Cargar datos solo si no se han cargado antes
        if (!_datosYaCargados)
        {
            _ = CargarPacientesAsync();
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
            _ = CargarPacientesAsync();
        }
    }

    private async Task CargarPacientesAsync()
    {
        try
        {
            Pacientes.Clear();
            
            var pacientes = await _context.Pacientes.ToListAsync();
            
            foreach (var paciente in pacientes)
            {
                Pacientes.Add(paciente);
            }

            ActualizarListaFiltrada();
            ActualizarEstadisticas();
            EmptyState.Visibility = Pacientes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            
            // Marcar que los datos ya fueron cargados
            _datosYaCargados = true;
        }
        catch (Exception ex)
        {
            await ShowInfoDialog("Error", $"Error al cargar pacientes: {ex.Message}");
        }
    }

    private void ActualizarEstadisticas()
    {
        try
        {
            // Total de pacientes
            if (this.FindName("txtTotalPacientes") is TextBlock totalText)
                totalText.Text = Pacientes.Count.ToString();
                
            if (this.FindName("txtContador") is Run contadorRun)
                contadorRun.Text = Pacientes.Count.ToString();
            
            // Pacientes activos (aquellos sin "fallecido" en observaciones)
            var activos = Pacientes.Count(p => string.IsNullOrEmpty(p.observaciones) || 
                                               !p.observaciones.Contains("fallecid", StringComparison.OrdinalIgnoreCase));
            if (this.FindName("txtPacientesActivos") is TextBlock activosText)
                activosText.Text = activos.ToString();
            
            // Contar áreas únicas
            var areasUnicas = Pacientes.Where(p => !string.IsNullOrEmpty(p.area))
                                       .Select(p => p.area)
                                       .Distinct()
                                       .Count();
            if (this.FindName("txtPorArea") is TextBlock areasText)
                areasText.Text = areasUnicas.ToString();
            
            // Pacientes registrados este mes
            if (this.FindName("txtUltimoMes") is TextBlock mesText)
                mesText.Text = "N/A";
        }
        catch
        {
            // Ignorar errores de estadísticas
        }
    }

    private void ActualizarListaFiltrada(string searchText = "")
    {
        PacientesFiltrados.Clear();
        
        var pacientesFiltrados = string.IsNullOrWhiteSpace(searchText)
            ? Pacientes
            : Pacientes.Where(p =>
                p.nombre.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                p.cedula.ToString().Contains(searchText) ||
                (p.telefono != null && p.telefono.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (p.celular != null && p.celular.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                p.nrecord.ToString().Contains(searchText) ||
                (!string.IsNullOrEmpty(p.observaciones) && p.observaciones.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.sexo) && p.sexo.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.area) && p.area.Contains(searchText, StringComparison.OrdinalIgnoreCase)));

        foreach (var paciente in pacientesFiltrados)
        {
            PacientesFiltrados.Add(paciente);
        }

        PacientesListView.ItemsSource = PacientesFiltrados;
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ActualizarListaFiltrada(sender.Text);
        }
    }

    private async void BtnNuevoPaciente_Click(object sender, RoutedEventArgs e)
    {
        var resultado = await MostrarDialogoPaciente(null);
        if (resultado != null)
        {
            try
            {
                _context.Pacientes.Add(resultado);
                await _context.SaveChangesAsync();

                await CargarPacientesAsync();
                await ShowInfoDialog("Éxito", "Paciente agregado correctamente");
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al guardar: {ex.Message}");
            }
        }
    }

    private async void BtnEditarPaciente_Click(object sender, RoutedEventArgs e)
    {
        var pacienteSeleccionado = PacientesListView.SelectedItem as Paciente;
        if (pacienteSeleccionado == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar un paciente");
            return;
        }

        var resultado = await MostrarDialogoPaciente(pacienteSeleccionado);
        if (resultado != null)
        {
            try
            {
                var paciente = await _context.Pacientes.FindAsync(pacienteSeleccionado.cedula);
                if (paciente != null)
                {
                    paciente.nombre = resultado.nombre;
                    paciente.telefono = resultado.telefono;
                    paciente.celular = resultado.celular;
                    paciente.nrecord = resultado.nrecord;
                    paciente.sexo = resultado.sexo;
                    paciente.area = resultado.area;
                    paciente.observaciones = resultado.observaciones;

                    await _context.SaveChangesAsync();
                    await CargarPacientesAsync();
                    await ShowInfoDialog("Éxito", "Paciente actualizado correctamente");
                }
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al actualizar: {ex.Message}");
            }
        }
    }

    private async void BtnEliminarPaciente_Click(object sender, RoutedEventArgs e)
    {
        var pacienteSeleccionado = PacientesListView.SelectedItem as Paciente;
        if (pacienteSeleccionado == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar un paciente");
            return;
        }

        var donacionesAsociadas = await _context.Donaciones
            .Where(d => d.idPaciente == pacienteSeleccionado.cedula)
            .CountAsync();

        var mensaje = $"¿Está seguro que desea eliminar este paciente?\n\nPaciente: {pacienteSeleccionado.nombre}\nCédula: {pacienteSeleccionado.cedula}";
        
        if (donacionesAsociadas > 0)
        {
            mensaje += $"\n\n?? ADVERTENCIA: Este paciente tiene {donacionesAsociadas} donación(es) registrada(s).\nLas donaciones asociadas también serán eliminadas.";
        }
        
        mensaje += "\n\nEsta acción no se puede deshacer.";

        var confirmDialog = new ContentDialog
        {
            Title = "Confirmar Eliminación",
            Content = mensaje,
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
                var paciente = await _context.Pacientes.FindAsync(pacienteSeleccionado.cedula);
                if (paciente != null)
                {
                    _context.Pacientes.Remove(paciente);
                    await _context.SaveChangesAsync();
                    await CargarPacientesAsync();

                    var mensajeExito = donacionesAsociadas > 0
                        ? $"Paciente eliminado correctamente.\nSe eliminaron {donacionesAsociadas} donación(es) asociada(s)."
                        : "Paciente eliminado correctamente.";

                    await ShowInfoDialog("Éxito", mensajeExito);
                }
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al eliminar: {ex.Message}");
            }
        }
    }

    private async void BtnRegistrarDonacion_Click(object sender, RoutedEventArgs e)
    {
        var pacienteSeleccionado = PacientesListView.SelectedItem as Paciente;
        if (pacienteSeleccionado == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar un paciente");
            return;
        }

        var resultado = await MostrarDialogoDonacion(pacienteSeleccionado);
        if (resultado != null)
        {
            try
            {
                _context.Donaciones.Add(resultado);
                await _context.SaveChangesAsync();
                await ShowInfoDialog("Éxito", $"Donación registrada correctamente.\nID Donación: {resultado.idDonacion}\nTotal: ${resultado.total:N2}");
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al guardar donación: {ex.Message}");
            }
        }
    }

    private async Task<Paciente> MostrarDialogoPaciente(Paciente pacienteExistente)
    {
        bool esEdicion = pacienteExistente != null;

        // Crear grid principal
        var mainGrid = new Grid
        {
            RowSpacing = 20
        };
        
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Formulario
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Info

        // Header con icono
        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var iconBorder = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["PrimaryGradient"],
            Width = 48,
            Height = 48,
            CornerRadius = new CornerRadius(12)
        };

        var iconContent = new FontIcon
        {
            Glyph = "\uE77B",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            FontSize = 24
        };
        iconBorder.Child = iconContent;

        var headerTextPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        var titleText = new TextBlock
        {
            Text = esEdicion ? "Editar Paciente" : "Nuevo Paciente",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        };

        var subtitleText = new TextBlock
        {
            Text = "Complete la información del paciente",
            FontSize = 13,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        headerTextPanel.Children.Add(titleText);
        headerTextPanel.Children.Add(subtitleText);
        headerPanel.Children.Add(iconBorder);
        headerPanel.Children.Add(headerTextPanel);

        Grid.SetRow(headerPanel, 0);
        mainGrid.Children.Add(headerPanel);

        // Formulario
        var formPanel = new StackPanel { Spacing = 16 };

        var cedulaBox = new TextBox
        {
            Header = "Cédula *",
            PlaceholderText = "000-0000000-0",
            Text = pacienteExistente?.cedula ?? "",
            IsEnabled = !esEdicion,
            CornerRadius = new CornerRadius(8)
        };

        var nombreBox = new TextBox
        {
            Header = "Nombre Completo *",
            PlaceholderText = "Nombre y apellidos del paciente",
            Text = pacienteExistente?.nombre ?? "",
            CornerRadius = new CornerRadius(8)
        };

        // Grid para teléfonos
        var telefonosGrid = new Grid
        {
            ColumnSpacing = 16
        };
        telefonosGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        telefonosGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var telefonoBox = new TextBox
        {
            Header = "Teléfono Fijo",
            PlaceholderText = "809-555-0000",
            Text = pacienteExistente?.telefono ?? "",
            CornerRadius = new CornerRadius(8)
        };
        Grid.SetColumn(telefonoBox, 0);

        var celularBox = new TextBox
        {
            Header = "Celular",
            PlaceholderText = "809-555-0000",
            Text = pacienteExistente?.celular ?? "",
            CornerRadius = new CornerRadius(8)
        };
        Grid.SetColumn(celularBox, 1);

        telefonosGrid.Children.Add(telefonoBox);
        telefonosGrid.Children.Add(celularBox);

        // Grid para registro y sexo
        var registroSexoGrid = new Grid
        {
            ColumnSpacing = 16
        };
        registroSexoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        registroSexoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var nrecordBox = new TextBox
        {
            Header = "Número de Registro *",
            PlaceholderText = "REG-0000",
            Text = pacienteExistente?.nrecord ?? "",
            CornerRadius = new CornerRadius(8)
        };
        Grid.SetColumn(nrecordBox, 0);

        var sexoCombo = new ComboBox
        {
            Header = "Sexo *",
            PlaceholderText = "Seleccione",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "M", "F" },
            SelectedItem = pacienteExistente?.sexo,
            CornerRadius = new CornerRadius(8)
        };
        Grid.SetColumn(sexoCombo, 1);

        registroSexoGrid.Children.Add(nrecordBox);
        registroSexoGrid.Children.Add(sexoCombo);

        var areaBox = new TextBox
        {
            Header = "Área Médica",
            PlaceholderText = "Ej: Cardiología, Pediatría, etc.",
            Text = pacienteExistente?.area ?? "",
            CornerRadius = new CornerRadius(8)
        };

        var observacionesBox = new TextBox
        {
            Header = "Observaciones Médicas",
            PlaceholderText = "Notas adicionales, alergias, condiciones especiales...",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 90,
            Text = pacienteExistente?.observaciones ?? "",
            CornerRadius = new CornerRadius(8)
        };

        formPanel.Children.Add(cedulaBox);
        formPanel.Children.Add(nombreBox);
        formPanel.Children.Add(telefonosGrid);
        formPanel.Children.Add(registroSexoGrid);
        formPanel.Children.Add(areaBox);
        formPanel.Children.Add(observacionesBox);

        Grid.SetRow(formPanel, 1);
        mainGrid.Children.Add(formPanel);

        // Info box
        var infoBorder = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorAttentionBackgroundBrush"],
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 8, 0, 0)
        };

        var infoPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        var infoIcon = new FontIcon
        {
            Glyph = "\uE946",
            FontSize = 16,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorAttentionBrush"]
        };

        var infoText = new TextBlock
        {
            Text = "* Los campos marcados son obligatorios para el registro",
            FontSize = 12,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        infoPanel.Children.Add(infoIcon);
        infoPanel.Children.Add(infoText);
        infoBorder.Child = infoPanel;

        Grid.SetRow(infoBorder, 2);
        mainGrid.Children.Add(infoBorder);

        var scrollViewer = new ScrollViewer
        {
            Content = mainGrid,
            MaxHeight = 600,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(2)
        };

        var dialog = new ContentDialog
        {
            Content = scrollViewer,
            PrimaryButtonText = "?? Guardar",
            CloseButtonText = "? Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"]
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(nombreBox.Text))
            {
                await ShowInfoDialog("?? Validación", "El nombre del paciente es obligatorio.");
                return await MostrarDialogoPaciente(pacienteExistente);
            }

            if (string.IsNullOrWhiteSpace(cedulaBox.Text))
            {
                await ShowInfoDialog("?? Validación", "La cédula del paciente es obligatoria.");
                return await MostrarDialogoPaciente(pacienteExistente);
            }

            if (!esEdicion && await _context.Pacientes.AnyAsync(p => p.cedula == cedulaBox.Text.Trim()))
            {
                await ShowInfoDialog("?? Duplicado", "Ya existe un paciente registrado con esta cédula.\n\nPor favor, verifique el número ingresado.");
                return await MostrarDialogoPaciente(pacienteExistente);
            }

            if (string.IsNullOrWhiteSpace(nrecordBox.Text))
            {
                await ShowInfoDialog("?? Validación", "El número de registro es obligatorio.");
                return await MostrarDialogoPaciente(pacienteExistente);
            }

            if (sexoCombo.SelectedItem == null)
            {
                await ShowInfoDialog("?? Validación", "Debe seleccionar el sexo del paciente.");
                return await MostrarDialogoPaciente(pacienteExistente);
            }

            return new Paciente
            {
                cedula = cedulaBox.Text.Trim(),
                nombre = nombreBox.Text.Trim(),
                telefono = string.IsNullOrWhiteSpace(telefonoBox.Text) ? "" : telefonoBox.Text.Trim(),
                celular = string.IsNullOrWhiteSpace(celularBox.Text) ? "" : celularBox.Text.Trim(),
                nrecord = nrecordBox.Text.Trim(),
                sexo = sexoCombo.SelectedItem.ToString(),
                area = string.IsNullOrWhiteSpace(areaBox.Text) ? "" : areaBox.Text.Trim(),
                observaciones = string.IsNullOrWhiteSpace(observacionesBox.Text) ? "" : observacionesBox.Text.Trim()
            };
        }

        return null;
    }

    private async Task<Donaciones> MostrarDialogoDonacion(Paciente paciente)
    {
        // Crear grid principal
        var mainGrid = new Grid
        {
            RowSpacing = 20
        };
        
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Info paciente
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Formulario
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Alert

        // Header con icono
        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var iconBorder = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SuccessGradient"],
            Width = 48,
            Height = 48,
            CornerRadius = new CornerRadius(12)
        };

        var iconContent = new FontIcon
        {
            Glyph = "\uE8EC",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            FontSize = 24
        };
        iconBorder.Child = iconContent;

        var headerTextPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        var titleText = new TextBlock
        {
            Text = "Registrar Donación",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        };

        var subtitleText = new TextBlock
        {
            Text = "Registre los detalles de la donación médica",
            FontSize = 13,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        headerTextPanel.Children.Add(titleText);
        headerTextPanel.Children.Add(subtitleText);
        headerPanel.Children.Add(iconBorder);
        headerPanel.Children.Add(headerTextPanel);

        Grid.SetRow(headerPanel, 0);
        mainGrid.Children.Add(headerPanel);

        // Info del paciente
        var pacienteInfoBorder = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16)
        };

        var pacienteInfoStack = new StackPanel { Spacing = 4 };
        
        var pacienteLabel = new TextBlock
        {
            Text = "Paciente:",
            FontSize = 11,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };

        var pacienteNombre = new TextBlock
        {
            Text = paciente.nombre,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        };

        var pacienteCedula = new TextBlock
        {
            Text = $"Cédula: {paciente.cedula}",
            FontSize = 12,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        pacienteInfoStack.Children.Add(pacienteLabel);
        pacienteInfoStack.Children.Add(pacienteNombre);
        pacienteInfoStack.Children.Add(pacienteCedula);
        pacienteInfoBorder.Child = pacienteInfoStack;

        Grid.SetRow(pacienteInfoBorder, 1);
        mainGrid.Children.Add(pacienteInfoBorder);

        // Formulario
        var formPanel = new StackPanel { Spacing = 16 };

        var fechaPicker = new CalendarDatePicker
        {
            Header = "Fecha de la Donación *",
            Date = DateTimeOffset.Now,
            MaxDate = DateTimeOffset.Now,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            CornerRadius = new CornerRadius(8)
        };

        var procedimientoBox = new TextBox
        {
            Header = "Procedimiento Médico *",
            PlaceholderText = "Descripción detallada del procedimiento o tratamiento",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            CornerRadius = new CornerRadius(8)
        };

        // Grid para montos
        var montosGrid = new Grid
        {
            ColumnSpacing = 16
        };
        montosGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        montosGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var montoSolicitadoBox = new NumberBox
        {
            Header = "Monto Solicitado (RD$) *",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0,
            CornerRadius = new CornerRadius(8)
        };
        Grid.SetColumn(montoSolicitadoBox, 0);

        var valorDonacionBox = new NumberBox
        {
            Header = "Valor Donación (RD$) *",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0,
            CornerRadius = new CornerRadius(8)
        };
        Grid.SetColumn(valorDonacionBox, 1);

        montosGrid.Children.Add(montoSolicitadoBox);
        montosGrid.Children.Add(valorDonacionBox);

        var totalBox = new NumberBox
        {
            Header = "Total Acumulado (RD$)",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0,
            IsEnabled = false,
            CornerRadius = new CornerRadius(8)
        };

        // Actualizar total automáticamente
        valorDonacionBox.ValueChanged += (s, args) =>
        {
            totalBox.Value = valorDonacionBox.Value;
        };

        var observacionBox = new TextBox
        {
            Header = "Observaciones",
            PlaceholderText = "Notas adicionales sobre la donación",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            CornerRadius = new CornerRadius(8)
        };

        formPanel.Children.Add(fechaPicker);
        formPanel.Children.Add(procedimientoBox);
        formPanel.Children.Add(montosGrid);
        formPanel.Children.Add(totalBox);
        formPanel.Children.Add(observacionBox);

        Grid.SetRow(formPanel, 2);
        mainGrid.Children.Add(formPanel);

        // Alert box
        var alertBorder = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"],
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 8, 0, 0)
        };

        var alertPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        var alertIcon = new FontIcon
        {
            Glyph = "\uE946",
            FontSize = 16,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"]
        };

        var alertText = new TextBlock
        {
            Text = "* Complete todos los campos obligatorios para registrar la donación",
            FontSize = 12,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        alertPanel.Children.Add(alertIcon);
        alertPanel.Children.Add(alertText);
        alertBorder.Child = alertPanel;

        Grid.SetRow(alertBorder, 3);
        mainGrid.Children.Add(alertBorder);

        var scrollViewer = new ScrollViewer
        {
            Content = mainGrid,
            MaxHeight = 600,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(2)
        };

        var dialog = new ContentDialog
        {
            Content = scrollViewer,
            PrimaryButtonText = "?? Registrar Donación",
            CloseButtonText = "? Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // Validaciones
            if (!fechaPicker.Date.HasValue)
            {
                await ShowInfoDialog("?? Validación", "Debe seleccionar una fecha para la donación.");
                return await MostrarDialogoDonacion(paciente);
            }

            if (string.IsNullOrWhiteSpace(procedimientoBox.Text))
            {
                await ShowInfoDialog("?? Validación", "Debe especificar el procedimiento médico.");
                return await MostrarDialogoDonacion(paciente);
            }

            if (montoSolicitadoBox.Value <= 0)
            {
                await ShowInfoDialog("?? Validación", "El monto solicitado debe ser mayor a cero.");
                return await MostrarDialogoDonacion(paciente);
            }

            if (valorDonacionBox.Value <= 0)
            {
                await ShowInfoDialog("?? Validación", "El valor de la donación debe ser mayor a cero.");
                return await MostrarDialogoDonacion(paciente);
            }

            return new Donaciones
            {
                Fecha = fechaPicker.Date.Value.DateTime,
                procedimiento = procedimientoBox.Text.Trim(),
                montoSolicitado = (decimal)montoSolicitadoBox.Value,
                valor = (decimal)valorDonacionBox.Value,
                total = (decimal)totalBox.Value,
                observacion = string.IsNullOrWhiteSpace(observacionBox.Text) ? "" : observacionBox.Text.Trim(),
                idPaciente = paciente.cedula
            };
        }

        return null;
    }

    private void PacientesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsPatientSelected = PacientesListView.SelectedItem != null;
        
        // Actualizar estado de botones directamente
        var haySeleccion = IsPatientSelected;
        
        if (this.FindName("btnEditar") is Button editBtn)
            editBtn.IsEnabled = haySeleccion;
            
        if (this.FindName("btnEliminar") is Button delBtn)
            delBtn.IsEnabled = haySeleccion;
            
        if (this.FindName("btnDonacion") is Button donBtn)
            donBtn.IsEnabled = haySeleccion;
    }

    private async Task ShowInfoDialog(string title, string message)
    {
        // Crear contenido mejorado
        var contentStack = new StackPanel
        {
            Spacing = 12,
            MaxWidth = 400
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

    private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
    {
        _datosYaCargados = false;
        await CargarPacientesAsync();
    }
}
