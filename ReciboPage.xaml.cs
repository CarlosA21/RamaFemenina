using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
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

public sealed partial class ReciboPage : Page, INotifyPropertyChanged
{
    private readonly RamaFemeninaContext _context;
    private bool _isReciboSelected;
    
    // Configuración de posiciones de impresión (en milímetros)
    private float numeroReciboX = 150;
    private float numeroReciboY = 20;
    private float fechaX = 150;
    private float fechaY = 35;
    private float recibimosDex = 15;
    private float recibimosDeY = 50;
    private float montoX = 150;
    private float montoY = 65;
    private float montoLetrasX = 15;
    private float montoLetrasY = 80;
    private float conceptoX = 15;
    private float conceptoY = 95;
    private float tipoPagoX = 15;
    private float tipoPagoY = 110;
    
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

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ReciboPage()
    {
        // Inicializar la colección
        RecibosCollection = new ObservableCollection<Recibo>();
        
        InitializeComponent();
        
        var app = Application.Current as App;
        _context = app!.Services.GetRequiredService<RamaFemeninaContext>();
        
        _ = CargarRecibosAsync();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = CargarRecibosAsync();
    }

    private async Task CargarRecibosAsync()
    {
        try
        {
            RecibosCollection.Clear();
            var recibos = await _context.Recibos.OrderByDescending(r => r.Fecha).ToListAsync();
            
            foreach (var recibo in recibos)
            {
                RecibosCollection.Add(recibo);
            }

            ActualizarLista();
            EmptyState.Visibility = RecibosCollection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            await ShowInfoDialog("Error", $"Error al cargar recibos: {ex.Message}");
        }
    }

    private void ActualizarLista(string searchText = "")
    {
        if (RecibosCollection == null || RecibosListView == null) return;
        
        var recibos = RecibosCollection.AsEnumerable();

        // Filtro por tipo de recibo
        var tipoReciboSeleccionado = TipoReciboCombo?.SelectedIndex ?? 0;
        if (tipoReciboSeleccionado == 1) // Ingreso
        {
            recibos = recibos.Where(r => r.TipoRecibo == "Ingreso");
        }
        else if (tipoReciboSeleccionado == 2) // Egreso
        {
            recibos = recibos.Where(r => r.TipoRecibo == "Egreso");
        }

        // Filtro de búsqueda
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            recibos = recibos.Where(r =>
                r.NumeroRecibo.ToString().Contains(searchText) ||
                (r.RecibimosDe != null && r.RecibimosDe.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (r.Cedula != null && r.Cedula.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (r.Concepto != null && r.Concepto.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (r.NumeroCheque != null && r.NumeroCheque.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                r.Monto.ToString().Contains(searchText));
        }

        // Aplicar ordenamiento
        var ordenados = AplicarOrdenamiento(recibos);

        RecibosListView.ItemsSource = ordenados.ToList();
        ActualizarResumen(ordenados);
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

    private void ActualizarResumen(IEnumerable<Recibo> recibos = null)
    {
        if (TotalRecibosText == null) return;
        
        var recibosParaResumen = recibos ?? RecibosCollection;
        var listaRecibos = recibosParaResumen.ToList();
        
        var totalRecibos = listaRecibos.Count;
        var totalIngresos = listaRecibos.Where(r => r.TipoRecibo == "Ingreso").Sum(r => r.Monto);
        var totalEgresos = listaRecibos.Where(r => r.TipoRecibo == "Egreso").Sum(r => r.Monto);
        var totalEfectivo = listaRecibos.Where(r => r.EsEfectivo).Sum(r => r.Monto);
        var totalTransferencia = listaRecibos.Where(r => r.EsTransferencia).Sum(r => r.Monto);
        var totalCheque = listaRecibos.Where(r => r.EsCheque).Sum(r => r.Monto);

        TotalRecibosText.Text = $"Total de recibos: {totalRecibos}";
        TotalIngresosText.Text = $"Ingresos: ${totalIngresos:N2}";
        TotalEgresosText.Text = $"Egresos: ${totalEgresos:N2}";
        TotalEfectivoText.Text = $"Efectivo: ${totalEfectivo:N2}";
        TotalChequeText.Text = $"Cheque: ${totalCheque:N2}";
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ActualizarLista(sender.Text);
        }
    }

    private void Ordenar_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ActualizarLista(SearchBox?.Text ?? "");
    }
    
    private void TipoRecibo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ActualizarLista(SearchBox?.Text ?? "");
    }

    private async void BtnNuevoRecibo_Click(object sender, RoutedEventArgs e)
    {
        var resultado = await MostrarDialogoRecibo(null);
        if (resultado != null)
        {
            try
            {
                _context.Recibos.Add(resultado);
                await _context.SaveChangesAsync();

                await CargarRecibosAsync();
                await ShowInfoDialog("Éxito", $"Recibo creado correctamente.\nNo. Recibo: {resultado.NumeroRecibo}\nMonto: ${resultado.Monto:N2}");
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al guardar recibo: {ex.Message}");
            }
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
            try
            {
                var recibo = await _context.Recibos.FindAsync(reciboSeleccionado.NumeroRecibo);
                if (recibo != null)
                {
                    recibo.TipoRecibo = resultado.TipoRecibo;
                    recibo.Fecha = resultado.Fecha;
                    recibo.RecibimosDe = resultado.RecibimosDe;
                    recibo.Cedula = resultado.Cedula;
                    recibo.Monto = resultado.Monto;
                    recibo.MontoEnLetras = resultado.MontoEnLetras;
                    recibo.Concepto = resultado.Concepto;
                    recibo.EsEfectivo = resultado.EsEfectivo;
                    recibo.EsTransferencia = resultado.EsTransferencia;
                    recibo.EsCheque = resultado.EsCheque;
                    recibo.NumeroFacturaNCF = resultado.NumeroFacturaNCF;
                    recibo.NumeroCheque = resultado.NumeroCheque;
                    recibo.Banco = resultado.Banco;

                    await _context.SaveChangesAsync();
                    await CargarRecibosAsync();
                    await ShowInfoDialog("Éxito", "Recibo actualizado correctamente");
                }
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al actualizar recibo: {ex.Message}");
            }
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
            try
            {
                var recibo = await _context.Recibos.FindAsync(reciboSeleccionado.NumeroRecibo);
                if (recibo != null)
                {
                    _context.Recibos.Remove(recibo);
                    await _context.SaveChangesAsync();
                    await CargarRecibosAsync();
                    await ShowInfoDialog("Éxito", "Recibo eliminado correctamente");
                }
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al eliminar recibo: {ex.Message}");
            }
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

        // Mostrar diálogo de configuración de impresión
        var configurar = await MostrarDialogoConfiguracionImpresion();
        if (!configurar)
        {
            return;
        }

        // Mostrar diálogo de selección de impresora
        var impresoraSeleccionada = await MostrarDialogoSeleccionImpresora();
        if (string.IsNullOrEmpty(impresoraSeleccionada))
        {
            return;
        }

        try
        {
            PrintDocument printDoc = new PrintDocument();
            printDoc.PrinterSettings.PrinterName = impresoraSeleccionada;
            printDoc.PrintPage += (s, ev) => PrintRecibo(s, ev, reciboSeleccionado);

            // Verificar que la impresora es válida
            if (!printDoc.PrinterSettings.IsValid)
            {
                await ShowInfoDialog("Error", $"La impresora '{impresoraSeleccionada}' no está disponible.");
                return;
            }

            // Imprimir
            printDoc.Print();
            await ShowInfoDialog("Éxito", $"Recibo enviado a la impresora '{impresoraSeleccionada}' correctamente");
        }
        catch (Exception ex)
        {
            await ShowInfoDialog("Error", $"Error al imprimir: {ex.Message}");
        }
    }

    /// <summary>
    /// Muestra un diálogo para seleccionar la impresora a utilizar
    /// </summary>
    private async Task<string> MostrarDialogoSeleccionImpresora()
    {
        // Obtener todas las impresoras instaladas
        var impresoras = PrinterSettings.InstalledPrinters;
        
        if (impresoras.Count == 0)
        {
            await ShowInfoDialog("Error", "No hay impresoras instaladas en el sistema.");
            return null;
        }

        // Crear lista de impresoras
        var listaImpresoras = new System.Collections.Generic.List<string>();
        foreach (string impresora in impresoras)
        {
            listaImpresoras.Add(impresora);
        }

        // Obtener impresora predeterminada
        var impresoraPredeterminada = new PrinterSettings().PrinterName;

        // Crear ComboBox para selección
        var comboBox = new ComboBox
        {
            Header = "Seleccione la impresora:",
            ItemsSource = listaImpresoras,
            SelectedItem = impresoraPredeterminada,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 8, 0, 0)
        };

        // Texto informativo
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

    private void PrintRecibo(object sender, PrintPageEventArgs e, Recibo recibo)
    {
        try
        {
            // Configurar unidades en milímetros
            e.Graphics.PageUnit = GraphicsUnit.Millimeter;

            // Fuente para imprimir
            Font font = new Font("Courier New", 11, System.Drawing.FontStyle.Regular);
            Font boldFont = new Font("Courier New", 11, System.Drawing.FontStyle.Bold);
            Brush brush = Brushes.Black;

            // Imprimir número de recibo
            e.Graphics.DrawString($"No. {recibo.NumeroRecibo}", boldFont, brush, numeroReciboX, numeroReciboY);

            // Imprimir fecha
            e.Graphics.DrawString(recibo.FechaFormateada, font, brush, fechaX, fechaY);

            // Imprimir "Recibimos de"
            e.Graphics.DrawString(recibo.RecibimosDe ?? "", font, brush, recibimosDex, recibimosDeY);

            // Imprimir monto numérico
            e.Graphics.DrawString(recibo.Monto.ToString("N2"), boldFont, brush, montoX, montoY);

            // Imprimir monto en letras
            e.Graphics.DrawString(recibo.MontoEnLetras ?? "", font, brush, montoLetrasX, montoLetrasY);

            // Imprimir concepto
            e.Graphics.DrawString(recibo.Concepto ?? "", font, brush, conceptoX, conceptoY);

            // Imprimir tipo de pago y detalles
            string tipoPagoTexto = $"{recibo.TipoPago}";
            if (!string.IsNullOrEmpty(recibo.DetallesPago))
            {
                tipoPagoTexto += $" - {recibo.DetallesPago}";
            }
            e.Graphics.DrawString(tipoPagoTexto, font, brush, tipoPagoX, tipoPagoY);

            font.Dispose();
            boldFont.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en PrintRecibo: {ex.Message}");
        }
    }

    private async Task<bool> MostrarDialogoConfiguracionImpresion()
    {
        var infoText = new TextBlock
        {
            Text = "Ajuste las posiciones de los campos en el recibo (en milímetros).",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };

        // Crear controles para cada posición
        var numeroReciboXBox = new NumberBox { Header = "Número de Recibo - Posición X (mm)", Value = numeroReciboX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var numeroReciboYBox = new NumberBox { Header = "Número de Recibo - Posición Y (mm)", Value = numeroReciboY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        
        var fechaXBox = new NumberBox { Header = "Fecha - Posición X (mm)", Value = fechaX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var fechaYBox = new NumberBox { Header = "Fecha - Posición Y (mm)", Value = fechaY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        
        var recibimosDeXBox = new NumberBox { Header = "Recibimos De - Posición X (mm)", Value = recibimosDex, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var recibimosDeYBox = new NumberBox { Header = "Recibimos De - Posición Y (mm)", Value = recibimosDeY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        
        var montoXBox = new NumberBox { Header = "Monto Numérico - Posición X (mm)", Value = montoX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var montoYBox = new NumberBox { Header = "Monto Numérico - Posición Y (mm)", Value = montoY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        
        var montoLetrasXBox = new NumberBox { Header = "Monto en Letras - Posición X (mm)", Value = montoLetrasX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var montoLetrasYBox = new NumberBox { Header = "Monto en Letras - Posición Y (mm)", Value = montoLetrasY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        
        var conceptoXBox = new NumberBox { Header = "Concepto - Posición X (mm)", Value = conceptoX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var conceptoYBox = new NumberBox { Header = "Concepto - Posición Y (mm)", Value = conceptoY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };

        var formPanel = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                infoText,
                numeroReciboXBox, numeroReciboYBox,
                fechaXBox, fechaYBox,
                recibimosDeXBox, recibimosDeYBox,
                montoXBox, montoYBox,
                montoLetrasXBox, montoLetrasYBox,
                conceptoXBox, conceptoYBox
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
            Title = "Configurar Impresión de Recibo",
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
            // Guardar valores personalizados
            numeroReciboX = (float)numeroReciboXBox.Value;
            numeroReciboY = (float)numeroReciboYBox.Value;
            fechaX = (float)fechaXBox.Value;
            fechaY = (float)fechaYBox.Value;
            recibimosDex = (float)recibimosDeXBox.Value;
            recibimosDeY = (float)recibimosDeYBox.Value;
            montoX = (float)montoXBox.Value;
            montoY = (float)montoYBox.Value;
            montoLetrasX = (float)montoLetrasXBox.Value;
            montoLetrasY = (float)montoLetrasYBox.Value;
            conceptoX = (float)conceptoXBox.Value;
            conceptoY = (float)conceptoYBox.Value;
            return true;
        }
        else if (result == ContentDialogResult.Secondary)
        {
            // Usar valores predeterminados (ya están configurados)
            return true;
        }

        return false;
    }

    private async void BtnGenerarReporte_Click(object sender, RoutedEventArgs e)
    {
        var recibosActuales = (RecibosListView.ItemsSource as List<Recibo>) ?? RecibosCollection.ToList();
        
        var totalRecibos = recibosActuales.Count;
        var montoTotal = recibosActuales.Sum(r => r.Monto);
        var totalIngresos = recibosActuales.Where(r => r.TipoRecibo == "Ingreso").Sum(r => r.Monto);
        var totalEgresos = recibosActuales.Where(r => r.TipoRecibo == "Egreso").Sum(r => r.Monto);
        var totalEfectivo = recibosActuales.Where(r => r.EsEfectivo).Sum(r => r.Monto);
        var totalTransferencia = recibosActuales.Where(r => r.EsTransferencia).Sum(r => r.Monto);
        var totalCheque = recibosActuales.Where(r => r.EsCheque).Sum(r => r.Monto);
        
        var reportePanel = new StackPanel { Spacing = 12 };
        
        reportePanel.Children.Add(new TextBlock
        {
            Text = "Resumen de Recibos",
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontSize = 16,
            TextWrapping = TextWrapping.Wrap
        });

        reportePanel.Children.Add(new TextBlock
        {
            Text = $"Total de recibos: {totalRecibos}",
            TextWrapping = TextWrapping.Wrap
        });

        reportePanel.Children.Add(new TextBlock
        {
            Text = $"Monto total: ${montoTotal:N2}",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        reportePanel.Children.Add(new Border
        {
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Margin = new Thickness(0, 8, 0, 8)
        });

        reportePanel.Children.Add(new TextBlock
        {
            Text = "Desglose por tipo de recibo:",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        reportePanel.Children.Add(new TextBlock
        {
            Text = $"• Ingresos: ${totalIngresos:N2} ({recibosActuales.Count(r => r.TipoRecibo == "Ingreso")} recibos)",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green)
        });

        reportePanel.Children.Add(new TextBlock
        {
            Text = $"• Egresos: ${totalEgresos:N2} ({recibosActuales.Count(r => r.TipoRecibo == "Egreso")} recibos)",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red)
        });

        reportePanel.Children.Add(new Border
        {
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Margin = new Thickness(0, 8, 0, 8)
        });

        reportePanel.Children.Add(new TextBlock
        {
            Text = "Desglose por tipo de pago:",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        reportePanel.Children.Add(new TextBlock
        {
            Text = $"• Efectivo: ${totalEfectivo:N2} ({recibosActuales.Count(r => r.EsEfectivo)} recibos)",
            TextWrapping = TextWrapping.Wrap
        });

        reportePanel.Children.Add(new TextBlock
        {
            Text = $"• Transferencia: ${totalTransferencia:N2} ({recibosActuales.Count(r => r.EsTransferencia)} recibos)",
            TextWrapping = TextWrapping.Wrap
        });

        reportePanel.Children.Add(new TextBlock
        {
            Text = $"• Cheque: ${totalCheque:N2} ({recibosActuales.Count(r => r.EsCheque)} recibos)",
            TextWrapping = TextWrapping.Wrap
        });

        var dialog = new ContentDialog
        {
            Title = "Reporte de Recibos",
            Content = new ScrollViewer
            {
                Content = reportePanel,
                MaxHeight = 400
            },
            CloseButtonText = "Cerrar",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }

    private async Task<Recibo> MostrarDialogoRecibo(Recibo reciboExistente)
    {
        bool esEdicion = reciboExistente != null;

        // Selector de tipo de recibo
        var tipoReciboCombo = new ComboBox
        {
            Header = "Tipo de Recibo *",
            MinWidth = 200,
            SelectedIndex = 0
        };
        tipoReciboCombo.Items.Add(new ComboBoxItem { Content = "Ingreso", Tag = "Ingreso" });
        tipoReciboCombo.Items.Add(new ComboBoxItem { Content = "Egreso", Tag = "Egreso" });
        
        if (esEdicion && reciboExistente.TipoRecibo == "Egreso")
        {
            tipoReciboCombo.SelectedIndex = 1;
        }

        var fechaPicker = new CalendarDatePicker
        {
            Header = "Fecha del Recibo *",
            Date = reciboExistente?.Fecha != null ? new DateTimeOffset(reciboExistente.Fecha) : DateTimeOffset.Now,
            MaxDate = DateTimeOffset.Now.AddYears(1)
        };

        var recibimosDe = new TextBox
        {
            Header = "Recibimos de / Entregamos a *",
            PlaceholderText = "Nombre completo o razón social",
            Text = reciboExistente?.RecibimosDe ?? "",
            MaxLength = 200
        };
        
        // Campo de cédula (para recibos de egreso principalmente)
        var cedulaBox = new TextBox
        {
            Header = "Cédula",
            PlaceholderText = "000-0000000-0",
            Text = reciboExistente?.Cedula ?? "",
            MaxLength = 20,
            Visibility = (esEdicion && reciboExistente?.TipoRecibo == "Egreso") ? Visibility.Visible : Visibility.Collapsed
        };

        // Evento para mostrar/ocultar campo de cédula según el tipo
        tipoReciboCombo.SelectionChanged += (s, e) =>
        {
            var selectedItem = tipoReciboCombo.SelectedItem as ComboBoxItem;
            var tipoSeleccionado = selectedItem?.Tag?.ToString() ?? "Ingreso";
            
            if (tipoSeleccionado == "Egreso")
            {
                cedulaBox.Visibility = Visibility.Visible;
                recibimosDe.Header = "Entregamos a *";
                recibimosDe.PlaceholderText = "Nombre de la persona que recibe";
            }
            else
            {
                cedulaBox.Visibility = Visibility.Collapsed;
                recibimosDe.Header = "Recibimos de *";
                recibimosDe.PlaceholderText = "Nombre completo o razón social";
            }
        };

        var montoBox = new NumberBox
        {
            Header = "Monto (RD$) *",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0,
            SmallChange = 0.01,
            LargeChange = 100.0,
            Value = reciboExistente?.Monto != null ? (double)reciboExistente.Monto : 0
        };

        var montoLetras = new TextBox
        {
            Header = "Monto en Letras",
            PlaceholderText = "Se generará automáticamente",
            IsReadOnly = true,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
            TextWrapping = TextWrapping.Wrap,
            Text = reciboExistente?.MontoEnLetras ?? ""
        };

        montoBox.ValueChanged += (s, args) =>
        {
            if (montoBox.Value > 0)
            {
                montoLetras.Text = ConvertirNumeroALetras((decimal)montoBox.Value);
            }
            else
            {
                montoLetras.Text = "";
            }
        };

        if (reciboExistente?.Monto > 0)
        {
            montoLetras.Text = ConvertirNumeroALetras(reciboExistente.Monto);
        }

        var conceptoBox = new TextBox
        {
            Header = "Por Concepto de *",
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
                tipoReciboCombo,
                fechaPicker,
                recibimosDe,
                cedulaBox,
                montoBox,
                montoLetras,
                conceptoBox,
                new TextBlock { Text = "Tipo de Pago *", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
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
                await ShowInfoDialog("Error", "Debe ingresar el nombre de quien recibe/entrega");
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

            var selectedItem = tipoReciboCombo.SelectedItem as ComboBoxItem;
            var tipoRecibo = selectedItem?.Tag?.ToString() ?? "Ingreso";

            return new Recibo
            {
                TipoRecibo = tipoRecibo,
                Fecha = fechaPicker.Date.Value.DateTime,
                RecibimosDe = recibimosDe.Text.Trim(),
                Cedula = string.IsNullOrWhiteSpace(cedulaBox.Text) ? null : cedulaBox.Text.Trim(),
                Monto = (decimal)montoBox.Value,
                MontoEnLetras = string.IsNullOrWhiteSpace(montoLetras.Text) ? null : montoLetras.Text.Trim(),
                Concepto = string.IsNullOrWhiteSpace(conceptoBox.Text) ? null : conceptoBox.Text.Trim(),
                EsEfectivo = efectivoCheck.IsChecked.GetValueOrDefault(),
                EsTransferencia = transferenciaCheck.IsChecked.GetValueOrDefault(),
                EsCheque = chequeCheck.IsChecked.GetValueOrDefault(),
                NumeroFacturaNCF = string.IsNullOrWhiteSpace(ncfBox.Text) ? null : ncfBox.Text.Trim(),
                NumeroCheque = string.IsNullOrWhiteSpace(numeroChequeBox.Text) ? null : numeroChequeBox.Text.Trim(),
                Banco = string.IsNullOrWhiteSpace(bancoBox.Text) ? null : bancoBox.Text.Trim()
            };
        }

        return null;
    }

    private string ConvertirNumeroALetras(decimal numero)
    {
        if (numero == 0) return "Cero pesos 00/100";
        if (numero < 0) return "Número inválido";

        int parteEntera = (int) numero;
        int centavos = (int) Math.Round((numero - parteEntera) * 100);

        string resultado = ConvertirEnteroALetras(parteEntera);
        return $"{resultado} pesos {centavos:00}/100";
    }

    /// <summary>
    /// Convierte un número entero a su representación en letras
    /// </summary>
    private string ConvertirEnteroALetras(int numero)
    {
        if (numero == 0) return "Cero";

        if (numero < 0) return "Número inválido";

        // Manejo de millones
        if (numero >= 1000000)
        {
            int millones = numero / 1000000;
            int resto = numero % 1000000;
            
            string textoMillones = millones == 1 
                ? "Un millón" 
                : ConvertirEnteroALetras(millones) + " millones";
            
            if (resto > 0)
            {
                return textoMillones + " " + ConvertirEnteroALetras(resto);
            }
            return textoMillones;
        }

        // Manejo de miles
        if (numero >= 1000)
        {
            int miles = numero / 1000;
            int resto = numero % 1000;
            
            string textoMiles = miles == 1 
                ? "Mil" 
                : ConvertirEnteroALetras(miles) + " mil";
            
            if (resto > 0)
            {
                return textoMiles + " " + ConvertirEnteroALetras(resto);
            }
            return textoMiles;
        }

        // Manejo de centenas
        if (numero >= 100)
        {
            return ConvertirCentenas(numero);
        }

        // Manejo de números menores a 100
        return ConvertirDecenas(numero);
    }

    /// <summary>
    /// Convierte números de 100 a 999 a letras
    /// </summary>
    private string ConvertirCentenas(int numero)
    {
        string[] centenas = { 
            "", "Ciento", "Doscientos", "Trescientos", "Cuatrocientos", 
            "Quinientos", "Seiscientos", "Setecientos", "Ochocientos", "Novecientos" 
        };

        int c = numero / 100;
        int resto = numero % 100;

        // Caso especial: 100 exacto
        if (numero == 100)
        {
            return "Cien";
        }

        string resultado = centenas[c];
        
        if (resto > 0)
        {
            resultado += " " + ConvertirDecenas(resto);
        }

        return resultado;
    }

    /// <summary>
    /// Convierte números de 0 a 99 a letras
    /// </summary>
    private string ConvertirDecenas(int numero)
    {
        string[] unidades = { 
            "", "Uno", "Dos", "Tres", "Cuatro", 
            "Cinco", "Seis", "Siete", "Ocho", "Nueve" 
        };
        
        string[] decenas = { 
            "", "Diez", "Veinte", "Treinta", "Cuarenta", 
            "Cincuenta", "Sesenta", "Setenta", "Ochenta", "Noventa" 
        };
        
        string[] especiales = { 
            "Diez", "Once", "Doce", "Trece", "Catorce", "Quince", 
            "Dieciséis", "Diecisiete", "Dieciocho", "Diecinueve" 
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
            // Casos especiales para veinte
            int u = numero % 10;
            if (u == 0)
                return "Veinte";
            else
                return "Veinti" + unidades[u].ToLower();
        }
        
        if (numero < 100)
        {
            int d = numero / 10;
            int u = numero % 10;
            
            if (u == 0)
                return decenas[d];
            else
                return decenas[d] + " y " + unidades[u];
        }

        return "";
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
