using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
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

public sealed partial class ChequesPage : Page, INotifyPropertyChanged
{
    private readonly RamaFemeninaContext _context;
    private bool _isChequeSelected;
    private bool _datosYaCargados = false;
    
    // Configuración de posiciones de impresión (en milímetros)
    private float nombreX = 15;
    private float nombreY = 35;
    private float fechaX = 150;
    private float fechaY = 20;
    private float letraX = 15;
    private float letraY = 50;
    private float montoX = 150;
    private float montoY = 50;
    private float conceptoX = 15;
    private float conceptoY = 65;
    
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

    public ObservableCollection<Cheques> ChequesCollection { get; set; }
    public ObservableCollection<Cheques> ChequesFiltrados { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ChequesPage()
    {
        InitializeComponent();
        
        // Habilitar caché de navegación
        NavigationCacheMode = NavigationCacheMode.Enabled;
        
        var app = Application.Current as App;
        _context = app!.Services.GetRequiredService<RamaFemeninaContext>();
        
        ChequesCollection = new ObservableCollection<Cheques>();
        ChequesFiltrados = new ObservableCollection<Cheques>();
        
        // Cargar datos solo si no se han cargado antes
        if (!_datosYaCargados)
        {
            _ = CargarChequesAsync();
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
            _ = CargarChequesAsync();
        }
    }

    private async Task CargarChequesAsync()
    {
        try
        {
            ChequesCollection.Clear();
            var cheques = await _context.Cheques.OrderByDescending(c => c.Fecha).ToListAsync();
            
            foreach (var cheque in cheques)
            {
                ChequesCollection.Add(cheque);
            }

            ActualizarListaFiltrada();
            ActualizarEstadisticas();
            
            // Controlar visibilidad
            var hayCheques = ChequesCollection.Count > 0;
            if (this.FindName("ListViewScroller") is UIElement listScroller)
                listScroller.Visibility = hayCheques ? Visibility.Visible : Visibility.Collapsed;
            EmptyState.Visibility = hayCheques ? Visibility.Collapsed : Visibility.Visible;
            
            // Marcar que los datos ya fueron cargados
            _datosYaCargados = true;
        }
        catch (Exception ex)
        {
            await ShowInfoDialog("Error", $"Error al cargar cheques: {ex.Message}");
        }
    }

    private void ActualizarEstadisticas()
    {
        try
        {
            // Total de cheques
            if (this.FindName("txtTotalCheques") is TextBlock totalText)
                totalText.Text = ChequesCollection.Count.ToString();
                
            if (this.FindName("txtContador") is Microsoft.UI.Xaml.Documents.Run contadorRun)
                contadorRun.Text = ChequesCollection.Count.ToString();
            
            // Calcular totales
            var montoTotal = ChequesCollection.Sum(c => c.monto);
            var promedio = ChequesCollection.Count > 0 ? montoTotal / ChequesCollection.Count : 0;
            
            // Cheques de este mes
            var primerDiaMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var chequesEsteMes = ChequesCollection.Where(c => c.Fecha >= primerDiaMes).ToList();
            var montoEsteMes = chequesEsteMes.Sum(c => c.monto);
            
            if (this.FindName("txtMontoTotal") is TextBlock totalMontoText)
                totalMontoText.Text = $"RD$ {montoTotal:N2}";
            
            if (this.FindName("txtPromedio") is TextBlock promedioText)
                promedioText.Text = $"RD$ {promedio:N2}";
            
            if (this.FindName("txtEsteMes") is TextBlock mesText)
                mesText.Text = $"RD$ {montoEsteMes:N2}";
        }
        catch
        {
            // Ignorar errores de estadísticas
        }
    }

    private void ActualizarListaFiltrada(string searchText = "")
    {
        if (ChequesFiltrados == null || ChequesCollection == null) return;
        
        ChequesFiltrados.Clear();
        
        var chequesFiltrados = string.IsNullOrWhiteSpace(searchText)
            ? ChequesCollection
            : ChequesCollection.Where(c =>
                (c.numero != null && c.numero.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (c.nombre != null && c.nombre.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (c.concepto != null && c.concepto.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                c.monto.ToString().Contains(searchText) ||
                c.idCheque.ToString().Contains(searchText));

        foreach (var cheque in chequesFiltrados)
        {
            ChequesFiltrados.Add(cheque);
        }

        ChequesListView.ItemsSource = ChequesFiltrados;
        
        // Actualizar contador con resultados filtrados
        if (this.FindName("txtContador") is Microsoft.UI.Xaml.Documents.Run contadorRun)
            contadorRun.Text = ChequesFiltrados.Count.ToString();
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
            try
            {
                _context.Cheques.Add(resultado);
                await _context.SaveChangesAsync();

                await CargarChequesAsync();
                await ShowInfoDialog("Éxito", $"Cheque registrado correctamente.\nID: {resultado.idCheque}\nN°: {resultado.numero}\nMonto: ${resultado.monto:N2}");
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al guardar cheque: {ex.Message}");
            }
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
            try
            {
                var cheque = await _context.Cheques.FindAsync(chequeSeleccionado.idCheque);
                if (cheque != null)
                {
                    cheque.numero = resultado.numero;
                    cheque.nombre = resultado.nombre;
                    cheque.monto = resultado.monto;
                    cheque.concepto = resultado.concepto;
                    cheque.Fecha = resultado.Fecha;

                    await _context.SaveChangesAsync();
                    await CargarChequesAsync();
                    await ShowInfoDialog("Éxito", "Cheque actualizado correctamente");
                }
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al actualizar cheque: {ex.Message}");
            }
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

        var confirmDialog = new ContentDialog
        {
            Title = "Confirmar Eliminación",
            Content = $"¿Está seguro que desea eliminar este cheque?\n\n" +
                      $"N° Cheque: {chequeSeleccionado.numero}\n" +
                      $"Páguese a: {chequeSeleccionado.nombre}\n" +
                      $"Monto: ${chequeSeleccionado.monto:N2}\n\n" +
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
                var cheque = await _context.Cheques.FindAsync(chequeSeleccionado.idCheque);
                if (cheque != null)
                {
                    _context.Cheques.Remove(cheque);
                    await _context.SaveChangesAsync();
                    await CargarChequesAsync();
                    await ShowInfoDialog("Éxito", "Cheque eliminado correctamente");
                }
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al eliminar cheque: {ex.Message}");
            }
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
            printDoc.PrintPage += (s, ev) => PrintCheque(s, ev, chequeSeleccionado);

            // Verificar que la impresora es válida
            if (!printDoc.PrinterSettings.IsValid)
            {
                await ShowInfoDialog("Error", $"La impresora '{impresoraSeleccionada}' no está disponible.");
                return;
            }

            // Imprimir
            printDoc.Print();
            await ShowInfoDialog("Éxito", $"Cheque enviado a la impresora '{impresoraSeleccionada}' correctamente");
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

    private void PrintCheque(object sender, PrintPageEventArgs e, Cheques cheque)
    {
        try
        {
            // Configurar unidades en milímetros
            e.Graphics.PageUnit = GraphicsUnit.Millimeter;

            // Fuente para imprimir
            Font font = new Font("Courier New", 11, System.Drawing.FontStyle.Regular);
            Brush brush = Brushes.Black;

            // Imprimir nombre
            e.Graphics.DrawString(cheque.nombre, font, brush, nombreX, nombreY);

            // Imprimir fecha (cada dígito separado) - MODERNIZADO
            DrawDateDigitsSeparated(e.Graphics, cheque.Fecha, fechaX, fechaY, font, brush);

            // Imprimir monto en letras
            string montoEnLetras = ConvertirNumeroALetras(cheque.monto);
            e.Graphics.DrawString(montoEnLetras, font, brush, letraX, letraY);

            // Imprimir monto numérico
            e.Graphics.DrawString(cheque.monto.ToString("N2"), font, brush, montoX, montoY);

            // Imprimir concepto
            e.Graphics.DrawString(cheque.concepto, font, brush, conceptoX, conceptoY);

            font.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en PrintCheque: {ex.Message}");
        }
    }

    /// <summary>
    /// Dibuja cada dígito de la fecha por separado en la posición especificada.
    /// Formato: DD/MM/YYYY con espaciado de 6mm entre dígitos.
    /// </summary>
    private void DrawDateDigitsSeparated(Graphics graphics, DateTime fecha, float startX, float startY, Font font, Brush brush)
    {
        const float digitSpacing = 6f; // Espaciado entre dígitos en milímetros
        
        // Formatear cada componente con dos dígitos (día y mes) o cuatro (año)
        string dia = fecha.Day.ToString("D2");
        string mes = fecha.Month.ToString("D2");
        string year = fecha.Year.ToString("D4");
        
        float currentX = startX;
        
        // Dibujar día (2 dígitos)
        foreach (char digit in dia)
        {
            graphics.DrawString(digit.ToString(), font, brush, currentX, startY);
            currentX += digitSpacing;
        }
        
        // Dibujar mes (2 dígitos)
        foreach (char digit in mes)
        {
            graphics.DrawString(digit.ToString(), font, brush, currentX, startY);
            currentX += digitSpacing;
        }
        
        // Dibujar año (4 dígitos)
        foreach (char digit in year)
        {
            graphics.DrawString(digit.ToString(), font, brush, currentX, startY);
            currentX += digitSpacing;
        }
    }

    private async Task<bool> MostrarDialogoConfiguracionImpresion()
    {
        var infoText = new TextBlock
        {
            Text = "Ajuste las posiciones de los campos en el cheque (en milímetros).",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };

        // Crear controles para cada posición
        var nombreXBox = new NumberBox { Header = "Nombre - Posición X (mm)", Value = nombreX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var nombreYBox = new NumberBox { Header = "Nombre - Posición Y (mm)", Value = nombreY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        
        var fechaXBox = new NumberBox { Header = "Fecha - Posición X (mm)", Value = fechaX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var fechaYBox = new NumberBox { Header = "Fecha - Posición Y (mm)", Value = fechaY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        
        var letraXBox = new NumberBox { Header = "Monto en Letras - Posición X (mm)", Value = letraX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var letraYBox = new NumberBox { Header = "Monto en Letras - Posición Y (mm)", Value = letraY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        
        var montoXBox = new NumberBox { Header = "Monto Numérico - Posición X (mm)", Value = montoX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var montoYBox = new NumberBox { Header = "Monto Numérico - Posición Y (mm)", Value = montoY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        
        var conceptoXBox = new NumberBox { Header = "Concepto - Posición X (mm)", Value = conceptoX, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        var conceptoYBox = new NumberBox { Header = "Concepto - Posición Y (mm)", Value = conceptoY, Minimum = 0, Maximum = 300, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };

        var formPanel = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                infoText,
                nombreXBox, nombreYBox,
                fechaXBox, fechaYBox,
                letraXBox, letraYBox,
                montoXBox, montoYBox,
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
            // Guardar valores personalizados
            nombreX = (float)nombreXBox.Value;
            nombreY = (float)nombreYBox.Value;
            fechaX = (float)fechaXBox.Value;
            fechaY = (float)fechaYBox.Value;
            letraX = (float)letraXBox.Value;
            letraY = (float)letraYBox.Value;
            montoX = (float)montoXBox.Value;
            montoY = (float)montoYBox.Value;
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

    private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
    {
        _datosYaCargados = false;
        await CargarChequesAsync();
    }

    private async Task<Cheques> MostrarDialogoCheque(Cheques chequeExistente)
    {
        bool esEdicion = chequeExistente != null;

        var numeroBox = new TextBox
        {
            Header = "Número de Cheque *",
            PlaceholderText = "000000",
            Text = chequeExistente?.numero ?? "",
            MaxLength = 20
        };

        var nombreBox = new TextBox
        {
            Header = "Páguese Contra Este Cheque a la Orden de *",
            PlaceholderText = "Nombre completo o razón social",
            Text = chequeExistente?.nombre ?? "",
            MaxLength = 200
        };

        var montoBox = new NumberBox
        {
            Header = "Monto (RD$) *",
            PlaceholderText = "0.00",
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Minimum = 0,
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
            TextWrapping = TextWrapping.Wrap
        };

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

        if (chequeExistente?.monto > 0)
        {
            montoLetrasBox.Text = ConvertirNumeroALetras(chequeExistente.monto);
        }

        var fechaPicker = new CalendarDatePicker
        {
            Header = "Fecha *",
            Date = chequeExistente?.Fecha != null ? new DateTimeOffset(chequeExistente.Fecha) : DateTimeOffset.Now,
            MaxDate = DateTimeOffset.Now.AddYears(1)
        };

        var conceptoBox = new TextBox
        {
            Header = "Concepto de Pago *",
            PlaceholderText = "Descripción del concepto de pago",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            Text = chequeExistente?.concepto ?? ""
        };

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

            // Verificar número duplicado (solo en creación o si cambió el número)
            if (!esEdicion || numeroBox.Text != chequeExistente.numero)
            {
                var existeNumero = await _context.Cheques
                    .AnyAsync(c => c.numero == numeroBox.Text.Trim() && 
                                   (esEdicion ? c.idCheque != chequeExistente.idCheque : true));
                
                if (existeNumero)
                {
                    await ShowInfoDialog("Error", "Ya existe un cheque con este número");
                    return null;
                }
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
        if (numero == 0) return "Cero pesos 00/100";
        if (numero < 0) return "Número inválido";

        int parteEntera = (int)numero;
        int centavos = (int)Math.Round((numero - parteEntera) * 100);

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

    private void ChequesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsChequeSelected = ChequesListView.SelectedItem != null;
        
        // Actualizar estado de botones directamente
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
        else if (title.Contains("Actualizado") || title.Contains("??"))
        {
            iconGlyph = "\uE72C"; // Refresh
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
