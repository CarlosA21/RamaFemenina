using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace RamaFemenina;

public sealed partial class PacientesPage : Page
{
    private bool _isPatientSelected;
    
    public bool IsPatientSelected
    {
        get => _isPatientSelected;
        set
        {
            _isPatientSelected = value;
            // Notificar cambio para habilitar/deshabilitar botones
        }
    }

    public ObservableCollection<Paciente> Pacientes { get; set; }

    public PacientesPage()
    {
        InitializeComponent();
        Pacientes = new ObservableCollection<Paciente>();
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
            Id = 1,
            NombreCompleto = "María García López",
            Telefono = "555-0101",
            FechaRegistro = DateTime.Now.AddDays(-10).ToString("dd/MM/yyyy")
        });
        
        Pacientes.Add(new Paciente
        {
            Id = 2,
            NombreCompleto = "Ana Martínez Rodríguez",
            Telefono = "555-0102",
            FechaRegistro = DateTime.Now.AddDays(-5).ToString("dd/MM/yyyy")
        });

        PacientesListView.ItemsSource = Pacientes;
        
        // Mostrar estado vacío si no hay pacientes
        EmptyState.Visibility = Pacientes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnNuevoPaciente_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Abrir diálogo o navegar a página de nuevo paciente
        ShowInfoDialog("Nuevo Paciente", "Funcionalidad en desarrollo");
    }

    private void PacientesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsPatientSelected = PacientesListView.SelectedItem != null;
    }

    private async void ShowInfoDialog(string title, string message)
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
