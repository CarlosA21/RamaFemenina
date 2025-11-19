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

public sealed partial class ReciboPage : Page, INotifyPropertyChanged
{
    private bool _isReciboSelected;
    
    public bool IsReciboSelected
    {
        get => _isReciboSelected;
        set
        {
            if (_isReciboSelected != value)
            {
                _isReciboSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<Recibo> RecibosCollection { get; set; }
    public ObservableCollection<Recibo> RecibosFiltrados { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ReciboPage()
    {
        // Initialize collections BEFORE InitializeComponent to prevent NullReferenceException
        RecibosCollection = new ObservableCollection<Recibo>();
        RecibosFiltrados = new ObservableCollection<Recibo>();
        
        InitializeComponent();
        CargarDatos();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
    }

    private void CargarDatos()
    {
        // Datos de ejemplo
        RecibosCollection.Add(new Recibo
        {
            NumeroRecibo = 1001,
            Fecha = new DateTime(2024, 1, 15),
            RecibimosDe = "María García López",
            Monto = 50000.00m,
            MontoEnLetras = "Cincuenta mil pesos",
            Concepto = "Pago de donación para cirugía de corazón",
            EsEfectivo = true
        });

        RecibosCollection.Add(new Recibo
        {
            NumeroRecibo = 1002,
            Fecha = new DateTime(2024, 1, 18),
            RecibimosDe = "Corporación XYZ",
            Monto = 75000.00m,
            MontoEnLetras = "Setenta y cinco mil pesos",
            Concepto = "Donación para tratamiento oncológico",
            EsTransferencia = true,
            NumeroFacturaNCF = "B0100000123"
        });

        RecibosCollection.Add(new Recibo
        {
            NumeroRecibo = 1003,
            Fecha = new DateTime(2024, 1, 20),
            RecibimosDe = "Ana Martínez Rodríguez",
            Monto = 25000.00m,
            MontoEnLetras = "Veinticinco mil pesos",
            Concepto = "Contribución para programa de diálisis",
            EsCheque = true,
            NumeroCheque = "456789",
            Banco = "Banco Popular"
        });

        RecibosCollection.Add(new Recibo
        {
            NumeroRecibo = 1004,
            Fecha = new DateTime(2024, 1, 22),
            RecibimosDe = "Fundación Esperanza",
            Monto = 100000.00m,
            MontoEnLetras = "Cien mil pesos",
            Concepto = "Donación mensual para medicamentos",
            EsTransferencia = true,
            NumeroFacturaNCF = "B0100000124"
        });

        ActualizarListaFiltrada();
        ActualizarResumen();
        EmptyState.Visibility = RecibosCollection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ActualizarListaFiltrada(string searchText = "", string filtroPago = "")
    {
        if (RecibosFiltrados == null || RecibosCollection == null) return;
        
        RecibosFiltrados.Clear();
        
        var recibosFiltrados = RecibosCollection.AsEnumerable();

        // Filtro de búsqueda
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            recibosFiltrados = recibosFiltrados.Where(r =>
                r.NumeroRecibo.ToString().Contains(searchText) ||
                (r.RecibimosDe != null && r.RecibimosDe.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (r.Concepto != null && r.Concepto.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (r.NumeroCheque != null && r.NumeroCheque.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                r.Monto.ToString().Contains(searchText));
        }

        // Filtro por tipo de pago
        if (!string.IsNullOrWhiteSpace(filtroPago) && filtroPago != "Todos")
        {
            recibosFiltrados = filtroPago switch
            {
                "Efectivo" => recibosFiltrados.Where(r => r.EsEfectivo),
                "Transferencia" => recibosFiltrados.Where(r => r.EsTransferencia),
                "Cheque" => recibosFiltrados.Where(r => r.EsCheque),
                _ => recibosFiltrados
            };
        }

        // Aplicar ordenamiento
        var ordenados = AplicarOrdenamiento(recibosFiltrados);

        foreach (var recibo in ordenados)
        {
            RecibosFiltrados.Add(recibo);
        }

        ActualizarResumen();
    }

    private IEnumerable<Recibo> AplicarOrdenamiento(IEnumerable<Recibo> recibos)
    {
        var selectedIndex = OrdenarCombo?.SelectedIndex ?? 0;
        
        return selectedIndex switch
        {
            0 => recibos.OrderByDescending(r => r.Fecha), // Fecha (Reciente)
            1 => recibos.OrderBy(r => r.Fecha), // Fecha (Antigua)
            2 => recibos.OrderByDescending(r => r.Monto), // Monto (Mayor)
            3 => recibos.OrderBy(r => r.Monto), // Monto (Menor)
            4 => recibos.OrderBy(r => r.NumeroRecibo), // Número de Recibo
            _ => recibos.OrderByDescending(r => r.Fecha)
        };
    }

    private void ActualizarResumen()
    {
        if (RecibosFiltrados == null || TotalRecibosText == null) return;
        
        var totalRecibos = RecibosFiltrados.Count;
        var totalEfectivo = RecibosFiltrados.Where(r => r.EsEfectivo).Sum(r => r.Monto);
        var totalTransferencia = RecibosFiltrados.Where(r => r.EsTransferencia).Sum(r => r.Monto);
        var totalCheque = RecibosFiltrados.Where(r => r.EsCheque).Sum(r => r.Monto);

        TotalRecibosText.Text = $"Total de recibos: {totalRecibos}";
        TotalEfectivoText.Text = $"Efectivo: ${totalEfectivo:N2}";
        TotalTransferenciaText.Text = $"Transferencia: ${totalTransferencia:N2}";
        TotalChequeText.Text = $"Cheque: ${totalCheque:N2}";
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var filtroPago = FiltroPagoCombo?.SelectedItem is ComboBoxItem item ? item.Content?.ToString() : "";
            ActualizarListaFiltrada(sender.Text, filtroPago);
        }
    }

    private void FiltroFecha_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // TODO: Implementar filtrado por fecha
        var filtroPago = FiltroPagoCombo?.SelectedItem is ComboBoxItem item ? item.Content?.ToString() : "";
        ActualizarListaFiltrada(SearchBox?.Text ?? "", filtroPago);
    }

    private void FiltroPago_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FiltroPagoCombo?.SelectedItem is ComboBoxItem item)
        {
            ActualizarListaFiltrada(SearchBox?.Text ?? "", item.Content?.ToString());
        }
    }

    private void Ordenar_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var filtroPago = FiltroPagoCombo?.SelectedItem is ComboBoxItem item ? item.Content?.ToString() : "";
        ActualizarListaFiltrada(SearchBox?.Text ?? "", filtroPago);
    }

    private async void BtnNuevoRecibo_Click(object sender, RoutedEventArgs e)
    {
        var resultado = await MostrarDialogoRecibo(null);
        if (resultado != null)
        {
            int nuevoNumero = RecibosCollection.Count > 0 ? RecibosCollection.Max(r => r.NumeroRecibo) + 1 : 1001;
            resultado.NumeroRecibo = nuevoNumero;

            RecibosCollection.Add(resultado);
            var filtroPago = FiltroPagoCombo?.SelectedItem is ComboBoxItem item ? item.Content?.ToString() : "";
            ActualizarListaFiltrada(SearchBox?.Text ?? "", filtroPago);
            EmptyState.Visibility = Visibility.Collapsed;

            await ShowInfoDialog("Éxito", $"Recibo creado correctamente.\nNo. Recibo: {nuevoNumero}\nMonto: ${resultado.Monto:N2}");
        }
    }

    private async void BtnEditarRecibo_Click(object sender, RoutedEventArgs e)
    {
        var reciboSeleccionado = RecibosListView.SelectedItem as Recibo;
        if (reciboSeleccionado == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar un recibo");
            return;
        }

        var resultado = await MostrarDialogoRecibo(reciboSeleccionado);
        if (resultado != null)
        {
            reciboSeleccionado.Fecha = resultado.Fecha;
            reciboSeleccionado.RecibimosDe = resultado.RecibimosDe;
            reciboSeleccionado.Monto = resultado.Monto;
            reciboSeleccionado.MontoEnLetras = resultado.MontoEnLetras;
            reciboSeleccionado.Concepto = resultado.Concepto;
            reciboSeleccionado.EsEfectivo = resultado.EsEfectivo;
            reciboSeleccionado.EsTransferencia = resultado.EsTransferencia;
            reciboSeleccionado.EsCheque = resultado.EsCheque;
            reciboSeleccionado.NumeroFacturaNCF = resultado.NumeroFacturaNCF;
            reciboSeleccionado.NumeroCheque = resultado.NumeroCheque;
            reciboSeleccionado.Banco = resultado.Banco;

            var filtroPago = FiltroPagoCombo?.SelectedItem is ComboBoxItem item ? item.Content?.ToString() : "";
            ActualizarListaFiltrada(SearchBox?.Text ?? "", filtroPago);
            await ShowInfoDialog("Éxito", "Recibo actualizado correctamente");
        }
    }

    private async void BtnEliminarRecibo_Click(object sender, RoutedEventArgs e)
    {
        var reciboSeleccionado = RecibosListView.SelectedItem as Recibo;
        if (reciboSeleccionado == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar un recibo");
            return;
        }

        var messagePanel = new StackPanel { Spacing = 12 };
        
        messagePanel.Children.Add(new TextBlock
        {
            Text = "¿Está seguro que desea eliminar este recibo?",
            TextWrapping = TextWrapping.Wrap
        });

        messagePanel.Children.Add(new TextBlock
        {
            Text = $"No. Recibo: {reciboSeleccionado.NumeroRecibo}",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        messagePanel.Children.Add(new TextBlock
        {
            Text = $"De: {reciboSeleccionado.RecibimosDe}",
            TextWrapping = TextWrapping.Wrap
        });

        messagePanel.Children.Add(new TextBlock
        {
            Text = $"Monto: ${reciboSeleccionado.Monto:N2}",
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
            RecibosCollection.Remove(reciboSeleccionado);
            var filtroPago = FiltroPagoCombo?.SelectedItem is ComboBoxItem item ? item.Content?.ToString() : "";
            ActualizarListaFiltrada(SearchBox?.Text ?? "", filtroPago);
            EmptyState.Visibility = RecibosCollection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            RecibosListView.SelectedItem = null;

            await ShowInfoDialog("Éxito", "Recibo eliminado correctamente");
        }
    }

    private async void BtnImprimirRecibo_Click(object sender, RoutedEventArgs e)
    {
        var reciboSeleccionado = RecibosListView.SelectedItem as Recibo;
        if (reciboSeleccionado == null)
        {
            await ShowInfoDialog("Error", "Debe seleccionar un recibo");
            return;
        }

        // TODO: Implementar impresión
        await ShowInfoDialog("Imprimir Recibo", 
            $"Funcionalidad de impresión en desarrollo.\n\n" +
            $"Recibo No.: {reciboSeleccionado.NumeroRecibo}\n" +
            $"Fecha: {reciboSeleccionado.FechaFormateada}\n" +
            $"Monto: ${reciboSeleccionado.Monto:N2}");
    }

    private async void BtnGenerarReporte_Click(object sender, RoutedEventArgs e)
    {
        var totalRecibos = RecibosFiltrados.Count;
        var montoTotal = RecibosFiltrados.Sum(r => r.Monto);
        
        await ShowInfoDialog("Generar Reporte", 
            "Funcionalidad de generación de reportes en desarrollo.\n\n" +
            $"Total de recibos: {totalRecibos}\n" +
            $"Monto total: ${montoTotal:N2}");
    }

    private async Task<Recibo> MostrarDialogoRecibo(Recibo reciboExistente)
    {
        bool esEdicion = reciboExistente != null;

        var fechaPicker = new CalendarDatePicker
        {
            Header = "Fecha del Recibo",
            Date = reciboExistente?.Fecha != null ? new DateTimeOffset(reciboExistente.Fecha) : DateTimeOffset.Now,
            MaxDate = DateTimeOffset.Now.AddYears(1)
        };

        var recibimosDe = new TextBox
        {
            Header = "Recibimos de",
            PlaceholderText = "Nombre completo o razón social",
            Text = reciboExistente?.RecibimosDe ?? ""
        };

        var montoBox = new NumberBox
        {
            Header = "Monto (RD$)",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0,
            SmallChange = 100.0,
            LargeChange = 1000.0,
            Value = reciboExistente?.Monto != null ? (double)reciboExistente.Monto : 0
        };

        var montoLetras = new TextBox
        {
            Header = "Monto en Letras",
            PlaceholderText = "Ej: Cincuenta mil pesos",
            Text = reciboExistente?.MontoEnLetras ?? ""
        };

        var conceptoBox = new TextBox
        {
            Header = "Por Concepto de",
            PlaceholderText = "Descripción del pago",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            Text = reciboExistente?.Concepto ?? ""
        };

        // Tipo de pago
        var efectivoCheck = new CheckBox
        {
            Content = "Efectivo",
            IsChecked = reciboExistente?.EsEfectivo ?? false
        };

        var transferenciaCheck = new CheckBox
        {
            Content = "Transferencia",
            IsChecked = reciboExistente?.EsTransferencia ?? false
        };

        var chequeCheck = new CheckBox
        {
            Content = "Cheque",
            IsChecked = reciboExistente?.EsCheque ?? false
        };

        var ncfBox = new TextBox
        {
            Header = "No. Factura / NCF",
            PlaceholderText = "Número de comprobante fiscal",
            Text = reciboExistente?.NumeroFacturaNCF ?? "",
            IsEnabled = reciboExistente?.EsTransferencia ?? false
        };

        var numeroChequeBox = new TextBox
        {
            Header = "Número de Cheque",
            PlaceholderText = "000000",
            Text = reciboExistente?.NumeroCheque ?? "",
            IsEnabled = reciboExistente?.EsCheque ?? false
        };

        var bancoBox = new TextBox
        {
            Header = "Banco",
            PlaceholderText = "Nombre del banco",
            Text = reciboExistente?.Banco ?? "",
            IsEnabled = reciboExistente?.EsCheque ?? false
        };

        // Eventos para habilitar/deshabilitar campos según el tipo de pago
        efectivoCheck.Checked += (s, e) => 
        {
            transferenciaCheck.IsChecked = false;
            chequeCheck.IsChecked = false;
            ncfBox.IsEnabled = false;
            numeroChequeBox.IsEnabled = false;
            bancoBox.IsEnabled = false;
        };

        transferenciaCheck.Checked += (s, e) => 
        {
            efectivoCheck.IsChecked = false;
            chequeCheck.IsChecked = false;
            ncfBox.IsEnabled = true;
            numeroChequeBox.IsEnabled = false;
            bancoBox.IsEnabled = false;
        };

        chequeCheck.Checked += (s, e) => 
        {
            efectivoCheck.IsChecked = false;
            transferenciaCheck.IsChecked = false;
            ncfBox.IsEnabled = false;
            numeroChequeBox.IsEnabled = true;
            bancoBox.IsEnabled = true;
        };

        var tiposPagoPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Children = { efectivoCheck, transferenciaCheck, chequeCheck }
        };

        var formPanel = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                fechaPicker,
                recibimosDe,
                montoBox,
                montoLetras,
                conceptoBox,
                new TextBlock { Text = "Tipo de Pago", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                tiposPagoPanel,
                ncfBox,
                numeroChequeBox,
                bancoBox
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
            Title = esEdicion ? "Editar Recibo" : "Nuevo Recibo",
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
            if (string.IsNullOrWhiteSpace(recibimosDe.Text))
            {
                await ShowInfoDialog("Error", "Debe ingresar el nombre de quien recibe");
                return null;
            }

            if (!fechaPicker.Date.HasValue)
            {
                await ShowInfoDialog("Error", "Debe seleccionar una fecha");
                return null;
            }

            if (montoBox.Value <= 0 || double.IsNaN(montoBox.Value))
            {
                await ShowInfoDialog("Error", "Debe ingresar un monto válido");
                return null;
            }

            if (string.IsNullOrWhiteSpace(conceptoBox.Text))
            {
                await ShowInfoDialog("Error", "El concepto es obligatorio");
                return null;
            }

            if (!efectivoCheck.IsChecked.GetValueOrDefault() && 
                !transferenciaCheck.IsChecked.GetValueOrDefault() && 
                !chequeCheck.IsChecked.GetValueOrDefault())
            {
                await ShowInfoDialog("Error", "Debe seleccionar un tipo de pago");
                return null;
            }

            return new Recibo
            {
                Fecha = fechaPicker.Date.Value.DateTime,
                RecibimosDe = recibimosDe.Text.Trim(),
                Monto = (decimal)montoBox.Value,
                MontoEnLetras = montoLetras.Text.Trim(),
                Concepto = conceptoBox.Text.Trim(),
                EsEfectivo = efectivoCheck.IsChecked.GetValueOrDefault(),
                EsTransferencia = transferenciaCheck.IsChecked.GetValueOrDefault(),
                EsCheque = chequeCheck.IsChecked.GetValueOrDefault(),
                NumeroFacturaNCF = ncfBox.Text.Trim(),
                NumeroCheque = numeroChequeBox.Text.Trim(),
                Banco = bancoBox.Text.Trim()
            };
        }

        return null;
    }

    private void RecibosListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsReciboSelected = RecibosListView.SelectedItem != null;
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
