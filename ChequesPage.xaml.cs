using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RamaFemenina.Models;

namespace RamaFemenina;

public sealed partial class ChequesPage : Page, INotifyPropertyChanged
{
    private bool _isChequeSelected;
    
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

    public ObservableCollection<Cheques> Cheques { get; set; }
    public ObservableCollection<Cheques> ChequesFiltrados { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ChequesPage()
    {
        InitializeComponent();
        Cheques = new ObservableCollection<Cheques>();
        ChequesFiltrados = new ObservableCollection<Cheques>();
        CargarCheques();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
    }

    private void CargarCheques()
    {
        // TODO: Cargar desde base de datos
        // Por ahora, datos de ejemplo
        Cheques.Add(new Cheques
        {
            idCheque = 1,
            numero = "001234",
            nombre = "Juan Pérez González",
            monto = 5000.00m,
            concepto = "Pago de servicios médicos",
            Fecha = new DateTime(2024, 1, 15)
        });
        
        Cheques.Add(new Cheques
        {
            idCheque = 2,
            numero = "001235",
            nombre = "María García López",
            monto = 3500.50m,
            concepto = "Donación para tratamiento",
            Fecha = new DateTime(2024, 1, 20)
        });

        Cheques.Add(new Cheques
        {
            idCheque = 3,
            numero = "001236",
            nombre = "Hospital Central",
            monto = 12000.00m,
            concepto = "Pago de factura médica",
            Fecha = new DateTime(2024, 1, 25)
        });

        // Inicializar la lista filtrada con todos los cheques
        ActualizarListaFiltrada();
        ActualizarResumen();
        
        // Mostrar estado vacío si no hay cheques
        EmptyState.Visibility = Cheques.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ActualizarListaFiltrada(string searchText = "")
    {
        ChequesFiltrados.Clear();
        
        var chequesFiltrados = string.IsNullOrWhiteSpace(searchText)
            ? Cheques
            : Cheques.Where(c =>
                c.numero.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                c.nombre.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                c.concepto.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                c.monto.ToString().Contains(searchText));

        foreach (var cheque in chequesFiltrados.OrderByDescending(c => c.Fecha))
        {
            ChequesFiltrados.Add(cheque);
        }

        ChequesListView.ItemsSource = ChequesFiltrados;
        ActualizarResumen();
    }

    private void ActualizarResumen()
    {
        var totalCheques = ChequesFiltrados.Count;
        var montoTotal = ChequesFiltrados.Sum(c => c.monto);

        TotalChequesText.Text = $"Total de cheques: {totalCheques}";
        TotalMontoText.Text = $"Monto total: ${montoTotal:N2}";
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ActualizarListaFiltrada(sender.Text);
        }
    }

    private async void BtnNuevoCheque_Click(object sender, RoutedEventArgs e)
    {
        var resultado = await MostrarDialogoCheque(null);
        if (resultado != null)
        {
            // Generar nuevo ID
            int nuevoId = Cheques.Count > 0 ? Cheques.Max(c => c.idCheque) + 1 : 1;
            resultado.idCheque = nuevoId;

            Cheques.Add(resultado);
            ActualizarListaFiltrada(SearchBox?.Text ?? "");
            EmptyState.Visibility = Visibility.Collapsed;

            await ShowInfoDialog("Éxito", "Cheque registrado correctamente");
        }
    }

    private async void BtnEditarCheque_Click(object sender, RoutedEventArgs e)
    {
        var chequeSeleccionado = ChequesListView.SelectedItem as Cheques;
        if (chequeSeleccionado == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar un cheque");
            return;
        }

        var resultado = await MostrarDialogoCheque(chequeSeleccionado);
        if (resultado != null)
        {
            // Actualizar el cheque existente
            chequeSeleccionado.numero = resultado.numero;
            chequeSeleccionado.nombre = resultado.nombre;
            chequeSeleccionado.monto = resultado.monto;
            chequeSeleccionado.concepto = resultado.concepto;
            chequeSeleccionado.Fecha = resultado.Fecha;

            ActualizarListaFiltrada(SearchBox?.Text ?? "");
            await ShowInfoDialog("Éxito", "Cheque actualizado correctamente");
        }
    }

    private async void BtnEliminarCheque_Click(object sender, RoutedEventArgs e)
    {
        var chequeSeleccionado = ChequesListView.SelectedItem as Cheques;
        if (chequeSeleccionado == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar un cheque");
            return;
        }

        var messagePanel = new StackPanel { Spacing = 12 };
        
        messagePanel.Children.Add(new TextBlock
        {
            Text = "¿Está seguro que desea eliminar este cheque?",
            TextWrapping = TextWrapping.Wrap
        });

        messagePanel.Children.Add(new TextBlock
        {
            Text = $"N° Cheque: {chequeSeleccionado.numero}",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        messagePanel.Children.Add(new TextBlock
        {
            Text = $"Páguese a: {chequeSeleccionado.nombre}",
            TextWrapping = TextWrapping.Wrap
        });

        messagePanel.Children.Add(new TextBlock
        {
            Text = $"Monto: ${chequeSeleccionado.monto:N2}",
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
            Cheques.Remove(chequeSeleccionado);
            ActualizarListaFiltrada(SearchBox?.Text ?? "");
            EmptyState.Visibility = Cheques.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ChequesListView.SelectedItem = null;

            await ShowInfoDialog("Éxito", "Cheque eliminado correctamente");
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

        // TODO: Implementar funcionalidad de impresión
        await ShowInfoDialog("Imprimir Cheque", 
            $"Funcionalidad de impresión en desarrollo.\n\n" +
            $"Cheque N°: {chequeSeleccionado.numero}\n" +
            $"Monto: ${chequeSeleccionado.monto:N2}");
    }

    private async Task<Cheques> MostrarDialogoCheque(Cheques chequeExistente)
    {
        bool esEdicion = chequeExistente != null;

        // Crear campos del formulario
        var numeroBox = new TextBox
        {
            Header = "Número de Cheque",
            PlaceholderText = "000000",
            Text = chequeExistente?.numero ?? ""
        };

        var nombreBox = new TextBox
        {
            Header = "Páguese Contra Este Cheque a la Orden de",
            PlaceholderText = "Nombre completo o razón social",
            Text = chequeExistente?.nombre ?? ""
        };

        var montoBox = new NumberBox
        {
            Header = "Monto (RD$)",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0,
            SmallChange = 0.01,
            LargeChange = 100.0,
            Value = chequeExistente?.monto != null ? (double)chequeExistente.monto : 0
        };

        var montoLetrasBox = new TextBox
        {
            Header = "Pesos (Moneda de curso legal)",
            PlaceholderText = "Monto en letras se generará automáticamente",
            IsReadOnly = true,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray)
        };

        // Evento para convertir monto a letras
        montoBox.ValueChanged += (s, args) =>
        {
            if (montoBox.Value > 0)
            {
                montoLetrasBox.Text = ConvertirNumeroALetras((decimal)montoBox.Value);
            }
            else
            {
                montoLetrasBox.Text = "";
            }
        };

        // Inicializar monto en letras si es edición
        if (chequeExistente?.monto > 0)
        {
            montoLetrasBox.Text = ConvertirNumeroALetras(chequeExistente.monto);
        }

        var fechaPicker = new CalendarDatePicker
        {
            Header = "Fecha",
            Date = chequeExistente?.Fecha != null ? new DateTimeOffset(chequeExistente.Fecha) : DateTimeOffset.Now,
            MaxDate = DateTimeOffset.Now.AddYears(1)
        };

        var conceptoBox = new TextBox
        {
            Header = "Concepto de Pago",
            PlaceholderText = "Descripción del concepto de pago",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            Text = chequeExistente?.concepto ?? ""
        };

        // Crear panel del formulario
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
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        // Crear diálogo
        var dialog = new ContentDialog
        {
            Title = esEdicion ? "Editar Cheque" : "Nuevo Cheque",
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
            if (string.IsNullOrWhiteSpace(numeroBox.Text))
            {
                await ShowInfoDialog("Error", "El número de cheque es obligatorio");
                return null;
            }

            if (string.IsNullOrWhiteSpace(nombreBox.Text))
            {
                await ShowInfoDialog("Error", "El nombre del beneficiario es obligatorio");
                return null;
            }

            if (montoBox.Value <= 0 || double.IsNaN(montoBox.Value))
            {
                await ShowInfoDialog("Error", "Debe ingresar un monto válido");
                return null;
            }

            if (!fechaPicker.Date.HasValue)
            {
                await ShowInfoDialog("Error", "Debe seleccionar una fecha");
                return null;
            }

            if (string.IsNullOrWhiteSpace(conceptoBox.Text))
            {
                await ShowInfoDialog("Error", "El concepto de pago es obligatorio");
                return null;
            }

            // Verificar que el número de cheque no exista (solo en creación)
            if (!esEdicion && Cheques.Any(c => c.numero == numeroBox.Text))
            {
                await ShowInfoDialog("Error", "Ya existe un cheque con este número");
                return null;
            }

            return new Cheques
            {
                numero = numeroBox.Text.Trim(),
                nombre = nombreBox.Text.Trim(),
                monto = (decimal)montoBox.Value,
                Fecha = fechaPicker.Date.Value.DateTime,
                concepto = conceptoBox.Text.Trim()
            };
        }

        return null;
    }

    private string ConvertirNumeroALetras(decimal numero)
    {
        // Implementación simplificada de conversión de número a letras
        if (numero == 0) return "Cero pesos";

        string[] unidades = { "", "Uno", "Dos", "Tres", "Cuatro", "Cinco", "Seis", "Siete", "Ocho", "Nueve" };
        string[] decenas = { "", "Diez", "Veinte", "Treinta", "Cuarenta", "Cincuenta", "Sesenta", "Setenta", "Ochenta", "Noventa" };
        string[] especiales = { "Diez", "Once", "Doce", "Trece", "Catorce", "Quince", "Dieciséis", "Diecisiete", "Dieciocho", "Diecinueve" };

        int parteEntera = (int)numero;
        int centavos = (int)((numero - parteEntera) * 100);

        if (parteEntera < 10)
        {
            return $"{unidades[parteEntera]} pesos con {centavos:00}/100";
        }
        else if (parteEntera < 100)
        {
            int d = parteEntera / 10;
            int u = parteEntera % 10;
            if (parteEntera >= 10 && parteEntera < 20)
            {
                return $"{especiales[parteEntera - 10]} pesos con {centavos:00}/100";
            }
            return $"{decenas[d]}{(u > 0 ? " y " + unidades[u] : "")} pesos con {centavos:00}/100";
        }

        return $"{numero:N2} pesos"; // Para números mayores, mostrar formato numérico
    }

    private void ChequesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsChequeSelected = ChequesListView.SelectedItem != null;
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
