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
using RamaFemenina.Models;

namespace RamaFemenina;

public sealed partial class DonacionesPage : Page, INotifyPropertyChanged
{
    private bool _isDonacionSelected;
    
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
        // Initialize collections BEFORE InitializeComponent to prevent NullReferenceException
        // when event handlers fire during XAML initialization
        DonacionesCollection = new ObservableCollection<Donaciones>();
        DonacionesFiltradas = new ObservableCollection<Donaciones>();
        Pacientes = new ObservableCollection<Paciente>();
        
        InitializeComponent();
        CargarDatos();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
    }

    private void CargarDatos()
    {
        // TODO: Cargar desde base de datos
        // Cargar pacientes de ejemplo
        Pacientes.Add(new Paciente { cedula = 1, nombre = "María García López" });
        Pacientes.Add(new Paciente { cedula = 2, nombre = "Ana Martínez Rodríguez" });
        Pacientes.Add(new Paciente { cedula = 3, nombre = "Carmen Pérez Santos" });

        // Datos de ejemplo de donaciones
        DonacionesCollection.Add(new Donaciones
        {
            idDonacion = 1,
            Fecha = new DateTime(2024, 1, 15),
            idPaciente = 1,
            procedimiento = "Cirugía de corazón",
            observacion = "Paciente de escasos recursos",
            montoSolicitado = 50000.00m,
            valor = 50000.00m,
            total = 50000.00m
        });

        DonacionesCollection.Add(new Donaciones
        {
            idDonacion = 2,
            Fecha = new DateTime(2024, 1, 20),
            idPaciente = 2,
            procedimiento = "Tratamiento de cáncer",
            observacion = "Requiere quimioterapia urgente",
            montoSolicitado = 75000.00m,
            valor = 45000.00m,
            total = 45000.00m
        });

        DonacionesCollection.Add(new Donaciones
        {
            idDonacion = 3,
            Fecha = new DateTime(2024, 1, 25),
            idPaciente = 3,
            procedimiento = "Diálisis",
            observacion = "Tratamiento mensual",
            montoSolicitado = 25000.00m,
            valor = 0.00m,
            total = 0.00m
        });

        ActualizarListaFiltrada();
        ActualizarResumen();
        EmptyState.Visibility = DonacionesCollection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ActualizarListaFiltrada(string searchText = "")
    {
        // Safety check: prevent execution if collections aren't initialized yet
        if (DonacionesFiltradas == null || DonacionesCollection == null) return;
        
        DonacionesFiltradas.Clear();
        
        var donacionesFiltradas = string.IsNullOrWhiteSpace(searchText)
            ? DonacionesCollection
            : DonacionesCollection.Where(d =>
                (d.procedimiento != null && d.procedimiento.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (d.observacion != null && d.observacion.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                d.idPaciente.ToString().Contains(searchText) ||
                d.valor.ToString().Contains(searchText) ||
                d.montoSolicitado.ToString().Contains(searchText));

        // Aplicar ordenamiento
        var ordenadas = AplicarOrdenamiento(donacionesFiltradas);

        foreach (var donacion in ordenadas)
        {
            DonacionesFiltradas.Add(donacion);
        }

        ActualizarResumen();
    }

    private IEnumerable<Donaciones> AplicarOrdenamiento(IEnumerable<Donaciones> donaciones)
    {
        var selectedIndex = OrdenarCombo?.SelectedIndex ?? 0;
        
        return selectedIndex switch
        {
            0 => donaciones.OrderByDescending(d => d.Fecha), // Fecha (Reciente)
            1 => donaciones.OrderBy(d => d.Fecha), // Fecha (Antigua)
            2 => donaciones.OrderByDescending(d => d.total), // Monto (Mayor)
            3 => donaciones.OrderBy(d => d.total), // Monto (Menor)
            _ => donaciones.OrderByDescending(d => d.Fecha)
        };
    }

    private void ActualizarResumen()
    {
        // Safety check: prevent execution if collections or UI elements aren't initialized yet
        if (DonacionesFiltradas == null || TotalDonacionesText == null) return;
        
        var totalDonaciones = DonacionesFiltradas.Count;
        var totalSolicitado = DonacionesFiltradas.Sum(d => d.montoSolicitado);
        var totalDonado = DonacionesFiltradas.Sum(d => d.total);
        var diferencia = totalSolicitado - totalDonado;

        TotalDonacionesText.Text = $"Total de donaciones: {totalDonaciones}";
        TotalSolicitadoText.Text = $"Total solicitado: ${totalSolicitado:N2}";
        TotalDonadoText.Text = $"Total donado: ${totalDonado:N2}";
        DiferenciaText.Text = $"Diferencia: ${diferencia:N2}";
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ActualizarListaFiltrada(sender.Text);
        }
    }

    private void FiltroFecha_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // TODO: Implementar filtrado por fecha
        ActualizarListaFiltrada(SearchBox?.Text ?? "");
    }

    private void Ordenar_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ActualizarListaFiltrada(SearchBox?.Text ?? "");
    }

    private async void BtnNuevaDonacion_Click(object sender, RoutedEventArgs e)
    {
        var resultado = await MostrarDialogoDonacion(null);
        if (resultado != null)
        {
            int nuevoId = DonacionesCollection.Count > 0 ? DonacionesCollection.Max(d => d.idDonacion) + 1 : 1;
            resultado.idDonacion = nuevoId;

            DonacionesCollection.Add(resultado);
            ActualizarListaFiltrada(SearchBox?.Text ?? "");
            EmptyState.Visibility = Visibility.Collapsed;

            await ShowInfoDialog("Éxito", $"Donación registrada correctamente.\nID: {nuevoId}\nTotal: ${resultado.total:N2}");
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
            donacionSeleccionada.Fecha = resultado.Fecha;
            donacionSeleccionada.idPaciente = resultado.idPaciente;
            donacionSeleccionada.procedimiento = resultado.procedimiento;
            donacionSeleccionada.observacion = resultado.observacion;
            donacionSeleccionada.montoSolicitado = resultado.montoSolicitado;
            donacionSeleccionada.valor = resultado.valor;
            donacionSeleccionada.total = resultado.total;

            ActualizarListaFiltrada(SearchBox?.Text ?? "");
            await ShowInfoDialog("Éxito", "Donación actualizada correctamente");
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

        var messagePanel = new StackPanel { Spacing = 12 };
        
        messagePanel.Children.Add(new TextBlock
        {
            Text = "¿Está seguro que desea eliminar esta donación?",
            TextWrapping = TextWrapping.Wrap
        });

        messagePanel.Children.Add(new TextBlock
        {
            Text = $"ID Donación: {donacionSeleccionada.idDonacion}",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        messagePanel.Children.Add(new TextBlock
        {
            Text = $"Paciente ID: {donacionSeleccionada.idPaciente}",
            TextWrapping = TextWrapping.Wrap
        });

        messagePanel.Children.Add(new TextBlock
        {
            Text = $"Procedimiento: {donacionSeleccionada.procedimiento}",
            TextWrapping = TextWrapping.Wrap
        });

        messagePanel.Children.Add(new TextBlock
        {
            Text = $"Monto: ${donacionSeleccionada.total:N2}",
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
            DonacionesCollection.Remove(donacionSeleccionada);
            ActualizarListaFiltrada(SearchBox?.Text ?? "");
            EmptyState.Visibility = DonacionesCollection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            DonacionesListView.SelectedItem = null;

            await ShowInfoDialog("Éxito", "Donación eliminada correctamente");
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

        var paciente = Pacientes.FirstOrDefault(p => p.cedula == donacionSeleccionada.idPaciente);
        if (paciente != null)
        {
            await ShowInfoDialog("Información del Paciente", 
                $"ID: {paciente.cedula}\n" +
                $"Nombre: {paciente.nombre}\n\n" +
                $"Esta funcionalidad puede navegar a la página de pacientes.");
        }
        else
        {
            await ShowInfoDialog("Error", "No se encontró el paciente asociado");
        }
    }

    private async void BtnGenerarReporte_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implementar generación de reportes
        await ShowInfoDialog("Generar Reporte", 
            "Funcionalidad de generación de reportes en desarrollo.\n\n" +
            $"Total de donaciones: {DonacionesFiltradas.Count}\n" +
            $"Monto total: ${DonacionesFiltradas.Sum(d => d.total):N2}");
    }

    private async Task<Donaciones> MostrarDialogoDonacion(Donaciones donacionExistente)
    {
        bool esEdicion = donacionExistente != null;

        // Selector de paciente
        var pacienteCombo = new ComboBox
        {
            Header = "Paciente",
            PlaceholderText = "Seleccione un paciente",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = Pacientes,
            DisplayMemberPath = "nombre"
        };

        if (esEdicion)
        {
            pacienteCombo.SelectedItem = Pacientes.FirstOrDefault(p => p.cedula == donacionExistente.idPaciente);
        }

        var fechaPicker = new CalendarDatePicker
        {
            Header = "Fecha de Donación",
            Date = donacionExistente?.Fecha != null ? new DateTimeOffset(donacionExistente.Fecha) : DateTimeOffset.Now,
            MaxDate = DateTimeOffset.Now.AddYears(1)
        };

        var procedimientoBox = new TextBox
        {
            Header = "Procedimiento Médico",
            PlaceholderText = "Descripción del procedimiento",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            Text = donacionExistente?.procedimiento ?? ""
        };

        var montoSolicitadoBox = new NumberBox
        {
            Header = "Monto Solicitado (RD$)",
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

        // Calcular total y porcentaje automáticamente
        void ActualizarTotal()
        {
            totalBox.Value = valorDonacionBox.Value;
            
            if (montoSolicitadoBox.Value > 0)
            {
                var porcentaje = (valorDonacionBox.Value / montoSolicitadoBox.Value) * 100;
                porcentajeText.Text = $"Porcentaje completado: {porcentaje:F1}%";
                progressBar.Value = porcentaje;
                
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

        // Inicializar valores
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
            // Validaciones
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
