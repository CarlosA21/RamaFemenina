using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RamaFemenina.Models;

namespace RamaFemenina;

public sealed partial class PacientesPage : Page, INotifyPropertyChanged
{
    private bool _isPatientSelected;
    
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
        Pacientes = new ObservableCollection<Paciente>();
        PacientesFiltrados = new ObservableCollection<Paciente>();
        Donaciones = new ObservableCollection<Donaciones>();
        CargarPacientes();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // Aquí podrías recibir parámetros si los necesitas
    }

    private void CargarPacientes()
    {
        // TODO: Cargar desde base de datos
        // Por ahora, datos de ejemplo
        Pacientes.Add(new Paciente
        {
            cedula = 1,
            nombre = "María García López",
            telefono = "555-0101",
            celular = "555-1234",
            nrecord = 1001,
            observaciones = "Paciente regular",
            sexo = "F",
            area = "Cardiología"
        });
        
        Pacientes.Add(new Paciente
        {
            cedula = 2,
            nombre = "Ana Martínez Rodríguez",
            telefono = "555-0102",
            celular = "555-5678",
            nrecord = 1002,
            observaciones = "Primera visita",
            sexo = "F",
            area = "Pediatría"
        });

        // Inicializar la lista filtrada con todos los pacientes
        ActualizarListaFiltrada();
        
        // Mostrar estado vacío si no hay pacientes
        EmptyState.Visibility = Pacientes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ActualizarListaFiltrada(string searchText = "")
    {
        PacientesFiltrados.Clear();
        
        var pacientesFiltrados = string.IsNullOrWhiteSpace(searchText)
            ? Pacientes
            : Pacientes.Where(p =>
                p.nombre.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                p.cedula.ToString().Contains(searchText) ||
                p.telefono.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                p.celular.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
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
        // Crear campos del formulario
        var cedulaBox = new NumberBox
        {
            Header = "Cédula",
            PlaceholderText = "Ingrese el número de cédula",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 1
        };

        var nombreBox = new TextBox
        {
            Header = "Nombre Completo",
            PlaceholderText = "Ingrese el nombre completo del paciente"
        };

        var telefonoBox = new TextBox
        {
            Header = "Teléfono",
            PlaceholderText = "555-0000"
        };

        var celularBox = new TextBox
        {
            Header = "Celular",
            PlaceholderText = "555-0000"
        };

        var nrecordBox = new NumberBox
        {
            Header = "Número de Registro",
            PlaceholderText = "Número de registro",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 1
        };

        var sexoCombo = new ComboBox
        {
            Header = "Sexo",
            PlaceholderText = "Seleccione el sexo",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "M", "F" }
        };

        var areaBox = new TextBox
        {
            Header = "Área",
            PlaceholderText = "Área médica (ej: Cardiología, Pediatría)"
        };

        var observacionesBox = new TextBox
        {
            Header = "Observaciones",
            PlaceholderText = "Observaciones adicionales (opcional)",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80
        };

        // Crear panel del formulario
        var formPanel = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                cedulaBox,
                nombreBox,
                telefonoBox,
                celularBox,
                nrecordBox,
                sexoCombo,
                areaBox,
                observacionesBox
            }
        };

        var scrollViewer = new ScrollViewer
        {
            Content = formPanel,
            MaxHeight = 500,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        // Crear diálogo
        var dialog = new ContentDialog
        {
            Title = "Nuevo Paciente",
            Content = scrollViewer,
            PrimaryButtonText = "Guardar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        // Si el usuario presionó Guardar
        if (result == ContentDialogResult.Primary)
        {
            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(nombreBox.Text))
            {
                await ShowInfoDialog("Error", "El nombre es obligatorio");
                return;
            }

            if (cedulaBox.Value <= 0 || double.IsNaN(cedulaBox.Value))
            {
                await ShowInfoDialog("Error", "Debe ingresar una cédula válida");
                return;
            }

            // Verificar si la cédula ya existe
            if (Pacientes.Any(p => p.cedula == (int)cedulaBox.Value))
            {
                await ShowInfoDialog("Error", "Ya existe un paciente con esta cédula");
                return;
            }

            if (nrecordBox.Value <= 0 || double.IsNaN(nrecordBox.Value))
            {
                await ShowInfoDialog("Error", "Debe ingresar un número de registro válido");
                return;
            }

            if (sexoCombo.SelectedItem == null)
            {
                await ShowInfoDialog("Error", "Debe seleccionar el sexo");
                return;
            }

            // Crear nuevo paciente
            var nuevoPaciente = new Paciente
            {
                cedula = (int)cedulaBox.Value,
                nombre = nombreBox.Text.Trim(),
                telefono = telefonoBox.Text.Trim(),
                celular = celularBox.Text.Trim(),
                nrecord = (long)nrecordBox.Value,
                sexo = sexoCombo.SelectedItem.ToString(),
                area = areaBox.Text.Trim(),
                observaciones = observacionesBox.Text.Trim()
            };

            // Agregar a la colección
            Pacientes.Add(nuevoPaciente);
            
            // Actualizar la lista filtrada
            ActualizarListaFiltrada(SearchBox?.Text ?? "");
            
            // Actualizar visibilidad del estado vacío
            EmptyState.Visibility = Visibility.Collapsed;

            // Mostrar mensaje de éxito
            await ShowInfoDialog("Éxito", "Paciente agregado correctamente");
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

        // Crear campos del formulario con los datos actuales
        var cedulaBox = new NumberBox
        {
            Header = "Cédula",
            PlaceholderText = "Ingrese el número de cédula",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 1,
            Value = pacienteSeleccionado.cedula,
            IsEnabled = false // No permitir editar la cédula
        };

        var nombreBox = new TextBox
        {
            Header = "Nombre Completo",
            PlaceholderText = "Ingrese el nombre completo del paciente",
            Text = pacienteSeleccionado.nombre
        };

        var telefonoBox = new TextBox
        {
            Header = "Teléfono",
            PlaceholderText = "555-0000",
            Text = pacienteSeleccionado.telefono
        };

        var celularBox = new TextBox
        {
            Header = "Celular",
            PlaceholderText = "555-0000",
            Text = pacienteSeleccionado.celular
        };

        var nrecordBox = new NumberBox
        {
            Header = "Número de Registro",
            PlaceholderText = "Número de registro",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 1,
            Value = pacienteSeleccionado.nrecord
        };

        var sexoCombo = new ComboBox
        {
            Header = "Sexo",
            PlaceholderText = "Seleccione el sexo",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "M", "F" },
            SelectedItem = pacienteSeleccionado.sexo
        };

        var areaBox = new TextBox
        {
            Header = "Área",
            PlaceholderText = "Área médica (ej: Cardiología, Pediatría)",
            Text = pacienteSeleccionado.area ?? ""
        };

        var observacionesBox = new TextBox
        {
            Header = "Observaciones",
            PlaceholderText = "Observaciones adicionales (opcional)",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            Text = pacienteSeleccionado.observaciones ?? ""
        };

        // Crear panel del formulario
        var formPanel = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                cedulaBox,
                nombreBox,
                telefonoBox,
                celularBox,
                nrecordBox,
                sexoCombo,
                areaBox,
                observacionesBox
            }
        };

        var scrollViewer = new ScrollViewer
        {
            Content = formPanel,
            MaxHeight = 500,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        // Crear diálogo
        var dialog = new ContentDialog
        {
            Title = "Editar Paciente",
            Content = scrollViewer,
            PrimaryButtonText = "Guardar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        // Si el usuario presionó Guardar
        if (result == ContentDialogResult.Primary)
        {
            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(nombreBox.Text))
            {
                await ShowInfoDialog("Error", "El nombre es obligatorio");
                return;
            }

            if (nrecordBox.Value <= 0 || double.IsNaN(nrecordBox.Value))
            {
                await ShowInfoDialog("Error", "Debe ingresar un número de registro válido");
                return;
            }

            if (sexoCombo.SelectedItem == null)
            {
                await ShowInfoDialog("Error", "Debe seleccionar el sexo");
                return;
            }

            // Actualizar los datos del paciente
            pacienteSeleccionado.nombre = nombreBox.Text.Trim();
            pacienteSeleccionado.telefono = telefonoBox.Text.Trim();
            pacienteSeleccionado.celular = celularBox.Text.Trim();
            pacienteSeleccionado.nrecord = (long)nrecordBox.Value;
            pacienteSeleccionado.sexo = sexoCombo.SelectedItem.ToString();
            pacienteSeleccionado.area = areaBox.Text.Trim();
            pacienteSeleccionado.observaciones = observacionesBox.Text.Trim();

            // Actualizar la lista filtrada
            ActualizarListaFiltrada(SearchBox?.Text ?? "");

            // Mostrar mensaje de éxito
            await ShowInfoDialog("Éxito", "Paciente actualizado correctamente");
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

        // Crear contenido del diálogo de confirmación
        var messagePanel = new StackPanel
        {
            Spacing = 12
        };

        messagePanel.Children.Add(new TextBlock
        {
            Text = "¿Está seguro que desea eliminar este paciente?",
            TextWrapping = TextWrapping.Wrap
        });

        messagePanel.Children.Add(new TextBlock
        {
            Text = $"Paciente: {pacienteSeleccionado.nombre}",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        messagePanel.Children.Add(new TextBlock
        {
            Text = $"Cédula: {pacienteSeleccionado.cedula}",
            TextWrapping = TextWrapping.Wrap
        });

        // Verificar si tiene donaciones asociadas
        var donacionesAsociadas = Donaciones.Where(d => d.idPaciente == pacienteSeleccionado.cedula).ToList();
        if (donacionesAsociadas.Count > 0)
        {
            messagePanel.Children.Add(new TextBlock
            {
                Text = $"\n?? ADVERTENCIA: Este paciente tiene {donacionesAsociadas.Count} donación(es) registrada(s).",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            messagePanel.Children.Add(new TextBlock
            {
                Text = "Las donaciones asociadas también serán eliminadas.",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange),
                TextWrapping = TextWrapping.Wrap
            });
        }

        messagePanel.Children.Add(new TextBlock
        {
            Text = "\nEsta acción no se puede deshacer.",
            FontStyle = Windows.UI.Text.FontStyle.Italic,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
            TextWrapping = TextWrapping.Wrap
        });

        // Crear diálogo de confirmación
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

        // Si el usuario confirmó la eliminación
        if (result == ContentDialogResult.Primary)
        {
            // Eliminar donaciones asociadas
            foreach (var donacion in donacionesAsociadas)
            {
                Donaciones.Remove(donacion);
            }

            // Eliminar el paciente
            Pacientes.Remove(pacienteSeleccionado);

            // Actualizar la lista filtrada
            ActualizarListaFiltrada(SearchBox?.Text ?? "");

            // Actualizar visibilidad del estado vacío
            EmptyState.Visibility = Pacientes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Limpiar selección
            PacientesListView.SelectedItem = null;

            // Mostrar mensaje de éxito
            var mensajeExito = donacionesAsociadas.Count > 0
                ? $"Paciente eliminado correctamente.\nSe eliminaron {donacionesAsociadas.Count} donación(es) asociada(s)."
                : "Paciente eliminado correctamente.";

            await ShowInfoDialog("Éxito", mensajeExito);
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

        // Crear campos del formulario
        var infoPaciente = new TextBlock
        {
            Text = $"Paciente: {pacienteSeleccionado.nombre} (Cédula: {pacienteSeleccionado.cedula})",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var fechaPicker = new CalendarDatePicker
        {
            Header = "Fecha de Donación",
            Date = DateTimeOffset.Now,
            MaxDate = DateTimeOffset.Now
        };

        var procedimientoBox = new TextBox
        {
            Header = "Procedimiento",
            PlaceholderText = "Descripción del procedimiento",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80
        };

        var montoSolicitadoBox = new NumberBox
        {
            Header = "Monto Solicitado",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0,
            SmallChange = 0.01,
            LargeChange = 1.0
        };

        var valorDonacionBox = new NumberBox
        {
            Header = "Valor de la Donación",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0,
            SmallChange = 0.01,
            LargeChange = 1.0
        };

        var totalBox = new NumberBox
        {
            Header = "Total",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0,
            SmallChange = 0.01,
            LargeChange = 1.0,
            IsEnabled = false
        };

        var observacionBox = new TextBox
        {
            Header = "Observaciones",
            PlaceholderText = "Observaciones adicionales (opcional)",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80
        };

        // Evento para calcular el total automáticamente
        valorDonacionBox.ValueChanged += (s, args) =>
        {
            totalBox.Value = valorDonacionBox.Value;
        };

        // Crear panel del formulario con scroll
        var formPanel = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                infoPaciente,
                fechaPicker,
                procedimientoBox,
                montoSolicitadoBox,
                valorDonacionBox,
                totalBox,
                observacionBox
            }
        };

        var scrollViewer = new ScrollViewer
        {
            Content = formPanel,
            MaxHeight = 500,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        // Crear diálogo
        var dialog = new ContentDialog
        {
            Title = "Registrar Donación",
            Content = scrollViewer,
            PrimaryButtonText = "Guardar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        // Si el usuario presionó Guardar
        if (result == ContentDialogResult.Primary)
        {
            // Validar campos obligatorios
            if (!fechaPicker.Date.HasValue)
            {
                await ShowInfoDialog("Error", "Debe seleccionar una fecha");
                return;
            }

            if (string.IsNullOrWhiteSpace(procedimientoBox.Text))
            {
                await ShowInfoDialog("Error", "Debe ingresar el procedimiento");
                return;
            }

            if (montoSolicitadoBox.Value <= 0 || double.IsNaN(montoSolicitadoBox.Value))
            {
                await ShowInfoDialog("Error", "Debe ingresar un monto solicitado válido");
                return;
            }

            if (valorDonacionBox.Value <= 0 || double.IsNaN(valorDonacionBox.Value))
            {
                await ShowInfoDialog("Error", "Debe ingresar un valor de donación válido");
                return;
            }

            // Generar ID de donación (en producción vendría de la base de datos)
            int nuevoIdDonacion = Donaciones.Count > 0 ? Donaciones.Max(d => d.idDonacion) + 1 : 1;

            // Crear nueva donación
            var nuevaDonacion = new Donaciones
            {
                idDonacion = nuevoIdDonacion,
                Fecha = fechaPicker.Date.Value.DateTime,
                procedimiento = procedimientoBox.Text.Trim(),
                montoSolicitado = (decimal)montoSolicitadoBox.Value,
                valor = (decimal)valorDonacionBox.Value,
                total = (decimal)totalBox.Value,
                observacion = observacionBox.Text.Trim(),
                idPaciente = pacienteSeleccionado.cedula
            };

            // Agregar a la colección
            Donaciones.Add(nuevaDonacion);

            // Mostrar mensaje de éxito
            await ShowInfoDialog("Éxito", $"Donación registrada correctamente.\nID Donación: {nuevoIdDonacion}\nTotal: ${nuevaDonacion.total:N2}");
        }
    }

    private void PacientesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsPatientSelected = PacientesListView.SelectedItem != null;
    }

    private async Task ShowInfoDialog(string title, string message)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "Ok",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
