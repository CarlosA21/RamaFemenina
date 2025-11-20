using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RamaFemenina.Models;
using RamaFemenina.Data;

namespace RamaFemenina;

public sealed partial class PacientesPage : Page, INotifyPropertyChanged
{
    private readonly RamaFemeninaContext _context;
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
        
        var app = Application.Current as App;
        _context = app!.Services.GetRequiredService<RamaFemeninaContext>();
        
        Pacientes = new ObservableCollection<Paciente>();
        PacientesFiltrados = new ObservableCollection<Paciente>();
        Donaciones = new ObservableCollection<Donaciones>();
        
        _ = CargarPacientesAsync();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = CargarPacientesAsync();
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
            EmptyState.Visibility = Pacientes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            await ShowInfoDialog("Error", $"Error al cargar pacientes: {ex.Message}");
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

        var cedulaBox = new TextBox
        {
            Header = "Cédula",
            PlaceholderText = "Ingrese el número de cédula",
            Text = pacienteExistente?.cedula ?? "",
            IsEnabled = !esEdicion
        };

        var nombreBox = new TextBox
        {
            Header = "Nombre Completo",
            PlaceholderText = "Ingrese el nombre completo del paciente",
            Text = pacienteExistente?.nombre ?? ""
        };

        var telefonoBox = new TextBox
        {
            Header = "Teléfono",
            PlaceholderText = "555-0000",
            Text = pacienteExistente?.telefono ?? ""
        };

        var celularBox = new TextBox
        {
            Header = "Celular",
            PlaceholderText = "555-0000",
            Text = pacienteExistente?.celular ?? ""
        };

        var nrecordBox = new TextBox
        {
            Header = "Número de Registro",
            PlaceholderText = "Número de registro",
            Text = pacienteExistente?.nrecord ?? ""
        };

        var sexoCombo = new ComboBox
        {
            Header = "Sexo",
            PlaceholderText = "Seleccione el sexo",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "M", "F" },
            SelectedItem = pacienteExistente?.sexo
        };

        var areaBox = new TextBox
        {
            Header = "Área",
            PlaceholderText = "Área médica",
            Text = pacienteExistente?.area ?? ""
        };

        var observacionesBox = new TextBox
        {
            Header = "Observaciones",
            PlaceholderText = "Observaciones adicionales",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            Text = pacienteExistente?.observaciones ?? ""
        };

        var formPanel = new StackPanel
        {
            Spacing = 16,
            Children = { cedulaBox, nombreBox, telefonoBox, celularBox, nrecordBox, sexoCombo, areaBox, observacionesBox }
        };

        var scrollViewer = new ScrollViewer
        {
            Content = formPanel,
            MaxHeight = 500,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var dialog = new ContentDialog
        {
            Title = esEdicion ? "Editar Paciente" : "Nuevo Paciente",
            Content = scrollViewer,
            PrimaryButtonText = "Guardar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(nombreBox.Text))
            {
                await ShowInfoDialog("Error", "El nombre es obligatorio");
                return null;
            }

            if (string.IsNullOrWhiteSpace(cedulaBox.Text))
            {
                await ShowInfoDialog("Error", "La cédula es obligatoria");
                return null;
            }

            if (!esEdicion && await _context.Pacientes.AnyAsync(p => p.cedula == cedulaBox.Text.Trim()))
            {
                await ShowInfoDialog("Error", "Ya existe un paciente con esta cédula");
                return null;
            }

            if (string.IsNullOrWhiteSpace(nrecordBox.Text))
            {
                await ShowInfoDialog("Error", "El número de registro es obligatorio");
                return null;
            }

            if (sexoCombo.SelectedItem == null)
            {
                await ShowInfoDialog("Error", "Debe seleccionar el sexo");
                return null;
            }

            return new Paciente
            {
                cedula = cedulaBox.Text.Trim(),
                nombre = nombreBox.Text.Trim(),
                telefono = telefonoBox.Text.Trim(),
                celular = celularBox.Text.Trim(),
                nrecord = nrecordBox.Text.Trim(),
                sexo = sexoCombo.SelectedItem.ToString(),
                area = areaBox.Text.Trim(),
                observaciones = observacionesBox.Text.Trim()
            };
        }

        return null;
    }

    private async Task<Donaciones> MostrarDialogoDonacion(Paciente paciente)
    {
        var fechaPicker = new CalendarDatePicker { Header = "Fecha de Donación", Date = DateTimeOffset.Now, MaxDate = DateTimeOffset.Now };
        var procedimientoBox = new TextBox { Header = "Procedimiento", PlaceholderText = "Descripción del procedimiento", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 80 };
        var montoSolicitadoBox = new NumberBox { Header = "Monto Solicitado", PlaceholderText = "0.00", SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden, Minimum = 0 };
        var valorDonacionBox = new NumberBox { Header = "Valor de la Donación", PlaceholderText = "0.00", SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden, Minimum = 0 };
        var totalBox = new NumberBox { Header = "Total", PlaceholderText = "0.00", SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden, Minimum = 0, IsEnabled = false };
        var observacionBox = new TextBox { Header = "Observaciones", PlaceholderText = "Observaciones adicionales", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 80 };

        valorDonacionBox.ValueChanged += (s, args) => { totalBox.Value = valorDonacionBox.Value; };

        var formPanel = new StackPanel
        {
            Spacing = 16,
            Children = {
                new TextBlock { Text = $"Paciente: {paciente.nombre} (Cédula: {paciente.cedula})", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                fechaPicker, procedimientoBox, montoSolicitadoBox, valorDonacionBox, totalBox, observacionBox
            }
        };

        var dialog = new ContentDialog
        {
            Title = "Registrar Donación",
            Content = new ScrollViewer { Content = formPanel, MaxHeight = 500, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            PrimaryButtonText = "Guardar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (!fechaPicker.Date.HasValue || string.IsNullOrWhiteSpace(procedimientoBox.Text) ||
                montoSolicitadoBox.Value <= 0 || valorDonacionBox.Value <= 0)
            {
                await ShowInfoDialog("Error", "Debe completar todos los campos obligatorios");
                return null;
            }

            return new Donaciones
            {
                Fecha = fechaPicker.Date.Value.DateTime,
                procedimiento = procedimientoBox.Text.Trim(),
                montoSolicitado = (decimal)montoSolicitadoBox.Value,
                valor = (decimal)valorDonacionBox.Value,
                total = (decimal)totalBox.Value,
                observacion = observacionBox.Text.Trim(),
                idPaciente = paciente.cedula
            };
        }

        return null;
    }

    private void PacientesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsPatientSelected = PacientesListView.SelectedItem != null;
    }

    private async Task ShowInfoDialog(string title, string message)
    {
        var dialog = new ContentDialog { Title = title, Content = message, CloseButtonText = "Ok", XamlRoot = this.XamlRoot };
        await dialog.ShowAsync();
    }
}
