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
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RamaFemenina.Models;
using RamaFemenina.Data;

namespace RamaFemenina
{
    public sealed partial class CajaChicaPage : Page, INotifyPropertyChanged
    {
        private readonly RamaFemeninaContext _context;
        private bool _isItemSelected;
        private bool _datosYaCargados = false;

        public bool IsItemSelected
        {
            get => _isItemSelected;
            set
            {
                if (_isItemSelected != value)
                {
                    _isItemSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<CajaChica> DesembolsosCollection { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CajaChicaPage()
        {
            DesembolsosCollection = new ObservableCollection<CajaChica>();
            
            InitializeComponent();
            
            // Habilitar caché de navegación
            NavigationCacheMode = NavigationCacheMode.Enabled;
            
            var app = Application.Current as App;
            _context = app!.Services.GetRequiredService<RamaFemeninaContext>();
            
            // Cargar datos solo si no se han cargado antes
            if (!_datosYaCargados)
            {
                _ = CargarDesembolsosAsync();
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
                _ = CargarDesembolsosAsync();
            }
        }

        private async Task CargarDesembolsosAsync()
        {
            try
            {
                DesembolsosCollection.Clear();
                var desembolsos = await _context.CajaChicas.OrderByDescending(c => c.Fecha).ToListAsync();
                
                foreach (var desembolso in desembolsos)
                {
                    DesembolsosCollection.Add(desembolso);
                }

                ActualizarLista();
                
                // Controlar visibilidad
                var hayDesembolsos = DesembolsosCollection.Count > 0;
                if (this.FindName("ListViewScroller") is UIElement listScroller)
                    listScroller.Visibility = hayDesembolsos ? Visibility.Visible : Visibility.Collapsed;
                EmptyState.Visibility = hayDesembolsos ? Visibility.Collapsed : Visibility.Visible;
                
                // Marcar que los datos ya fueron cargados
                _datosYaCargados = true;
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al cargar desembolsos: {ex.Message}");
            }
        }

        private void ActualizarEstadisticas(IEnumerable<CajaChica> desembolsos)
        {
            try
            {
                var listaDesembolsos = desembolsos.ToList();
                
                // Total de desembolsos
                if (this.FindName("txtTotalDesembolsos") is TextBlock totalText)
                    totalText.Text = listaDesembolsos.Count.ToString();
                    
                if (this.FindName("txtContador") is Microsoft.UI.Xaml.Documents.Run contadorRun)
                    contadorRun.Text = listaDesembolsos.Count.ToString();
                
                // Calcular totales
                var montoTotal = listaDesembolsos.Sum(d => d.Monto);
                var promedio = listaDesembolsos.Count > 0 ? montoTotal / listaDesembolsos.Count : 0;
                
                if (this.FindName("txtMontoTotal") is TextBlock montoText)
                    montoText.Text = $"RD$ {montoTotal:N2}";
                
                if (this.FindName("txtPromedio") is TextBlock promedioText)
                    promedioText.Text = $"RD$ {promedio:N2}";
            }
            catch
            {
                // Ignorar errores de estadísticas
            }
        }

        private void ActualizarLista(string searchText = "")
        {
            if (DesembolsosCollection == null || DesembolsosListView == null) return;
            
            var desembolsos = DesembolsosCollection.AsEnumerable();

            // Filtro de búsqueda en tiempo real
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                desembolsos = desembolsos.Where(d =>
                    d.NumeroRecibo.ToString().Contains(searchText) ||
                    (d.PagadoA != null && d.PagadoA.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (d.ConCargoA != null && d.ConCargoA.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (d.Concepto != null && d.Concepto.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    d.Monto.ToString().Contains(searchText));
            }

            var lista = desembolsos.ToList();
            DesembolsosListView.ItemsSource = lista;
            ActualizarEstadisticas(lista);
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ActualizarLista(sender.Text);
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsItemSelected = DesembolsosListView.SelectedItem != null;
            
            // Actualizar estado de botones directamente
            var haySeleccion = IsItemSelected;
            
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

        private async void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var resultado = await MostrarDialogoDesembolso(null);
            if (resultado != null)
            {
                try
                {
                    _context.CajaChicas.Add(resultado);
                    await _context.SaveChangesAsync();

                    await CargarDesembolsosAsync();
                    await ShowInfoDialog("Éxito", $"Desembolso creado correctamente.\nNo. Recibo: {resultado.NumeroRecibo}\nMonto: ${resultado.Monto:N2}");
                }
                catch (Exception ex)
                {
                    await ShowInfoDialog("Error", $"Error al guardar desembolso: {ex.Message}");
                }
            }
        }

        private async void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            var desembolsoSeleccionado = DesembolsosListView.SelectedItem as CajaChica;
            if (desembolsoSeleccionado == null)
            {
                await ShowInfoDialog("Error", "Debe seleccionar un desembolso");
                return;
            }

            var resultado = await MostrarDialogoDesembolso(desembolsoSeleccionado);
            if (resultado != null)
            {
                try
                {
                    var desembolso = await _context.CajaChicas.FindAsync(desembolsoSeleccionado.IdRecibo);
                    if (desembolso != null)
                    {
                        desembolso.Fecha = resultado.Fecha;
                        desembolso.PagadoA = resultado.PagadoA;
                        desembolso.Monto = resultado.Monto;
                        desembolso.NumeroRecibo = resultado.NumeroRecibo;
                        desembolso.ConCargoA = resultado.ConCargoA;
                        desembolso.Concepto = resultado.Concepto;

                        await _context.SaveChangesAsync();
                        await CargarDesembolsosAsync();
                        await ShowInfoDialog("Éxito", "Desembolso actualizado correctamente");
                    }
                }
                catch (Exception ex)
                {
                    await ShowInfoDialog("Error", $"Error al actualizar desembolso: {ex.Message}");
                }
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            var desembolsoSeleccionado = DesembolsosListView.SelectedItem as CajaChica;
            if (desembolsoSeleccionado == null)
            {
                await ShowInfoDialog("Error", "Debe seleccionar un desembolso");
                return;
            }

            var messagePanel = new StackPanel { Spacing = 12 };
            
            messagePanel.Children.Add(new TextBlock
            {
                Text = "¿Está seguro que desea eliminar este desembolso?",
                TextWrapping = TextWrapping.Wrap
            });

            messagePanel.Children.Add(new TextBlock
            {
                Text = $"No. Recibo: {desembolsoSeleccionado.NumeroRecibo}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            messagePanel.Children.Add(new TextBlock
            {
                Text = $"Pagado a: {desembolsoSeleccionado.PagadoA}",
                TextWrapping = TextWrapping.Wrap
            });

            messagePanel.Children.Add(new TextBlock
            {
                Text = $"Monto: ${desembolsoSeleccionado.Monto:N2}",
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
                    var desembolso = await _context.CajaChicas.FindAsync(desembolsoSeleccionado.IdRecibo);
                    if (desembolso != null)
                    {
                        _context.CajaChicas.Remove(desembolso);
                        await _context.SaveChangesAsync();
                        await CargarDesembolsosAsync();
                        await ShowInfoDialog("Éxito", "Desembolso eliminado correctamente");
                    }
                }
                catch (Exception ex)
                {
                    await ShowInfoDialog("Error", $"Error al eliminar desembolso: {ex.Message}");
                }
            }
        }

        private async void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            var desembolsoSeleccionado = DesembolsosListView.SelectedItem as CajaChica;
            if (desembolsoSeleccionado == null)
            {
                await ShowInfoDialog("Error", "Debe seleccionar un desembolso");
                return;
            }

            try
            {
                PrintDocument printDoc = new PrintDocument();
                printDoc.PrintPage += (s, ev) => PrintDesembolso(s, ev, desembolsoSeleccionado);

                // Imprimir
                printDoc.Print();
                await ShowInfoDialog("Éxito", "Desembolso enviado a la impresora correctamente");
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Error", $"Error al imprimir: {ex.Message}");
            }
        }

        private void PrintDesembolso(object sender, PrintPageEventArgs e, CajaChica desembolso)
        {
            try
            {
                e.Graphics.PageUnit = GraphicsUnit.Millimeter;

                Font titleFont = new Font("Arial", 16, System.Drawing.FontStyle.Bold);
                Font font = new Font("Arial", 11, System.Drawing.FontStyle.Regular);
                Font boldFont = new Font("Arial", 11, System.Drawing.FontStyle.Bold);
                Brush brush = Brushes.Black;

                float y = 20;

                // Título
                e.Graphics.DrawString("DESEMBOLSO DE CAJA CHICA", titleFont, brush, 15, y);
                y += 15;

                // Número de recibo
                e.Graphics.DrawString($"No. Recibo: {desembolso.NumeroRecibo}", boldFont, brush, 15, y);
                e.Graphics.DrawString($"Fecha: {desembolso.FechaFormateada}", font, brush, 120, y);
                y += 10;

                // Separador
                e.Graphics.DrawLine(new Pen(Brushes.Gray), 15, y, 180, y);
                y += 8;

                // Pagado a
                e.Graphics.DrawString("Pagado a:", font, brush, 15, y);
                e.Graphics.DrawString(desembolso.PagadoA, boldFont, brush, 50, y);
                y += 10;

                // Monto
                e.Graphics.DrawString("La suma de:", font, brush, 15, y);
                e.Graphics.DrawString($"RD$ {desembolso.Monto:N2}", boldFont, brush, 50, y);
                y += 10;

                // Con cargo a
                e.Graphics.DrawString("Con cargo a:", font, brush, 15, y);
                e.Graphics.DrawString(desembolso.ConCargoA ?? "", font, brush, 50, y);
                y += 10;

                // Concepto
                e.Graphics.DrawString("Por concepto de:", font, brush, 15, y);
                y += 8;
                e.Graphics.DrawString(desembolso.Concepto ?? "", font, brush, 15, y);

                titleFont.Dispose();
                font.Dispose();
                boldFont.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en PrintDesembolso: {ex.Message}");
            }
        }

        private async Task<CajaChica> MostrarDialogoDesembolso(CajaChica desembolsoExistente)
        {
            bool esEdicion = desembolsoExistente != null;

            var numeroReciboBox = new NumberBox
            {
                Header = "No. Recibo *",
                PlaceholderText = "Número de recibo",
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Minimum = 0,
                Value = desembolsoExistente?.NumeroRecibo ?? 0
            };

            var fechaPicker = new CalendarDatePicker
            {
                Header = "Fecha *",
                Date = desembolsoExistente?.Fecha != null ? new DateTimeOffset(desembolsoExistente.Fecha) : DateTimeOffset.Now,
                MaxDate = DateTimeOffset.Now.AddYears(1)
            };

            var pagadoABox = new TextBox
            {
                Header = "Pagado a *",
                PlaceholderText = "Nombre completo o razón social",
                Text = desembolsoExistente?.PagadoA ?? "",
                MaxLength = 200
            };

            var montoBox = new NumberBox
            {
                Header = "La suma de RD$ *",
                PlaceholderText = "0.00",
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
                Minimum = 0,
                SmallChange = 0.01,
                LargeChange = 100.0,
                Value = desembolsoExistente?.Monto != null ? (double)desembolsoExistente.Monto : 0
            };

            var conCargoABox = new TextBox
            {
                Header = "Con cargo a",
                PlaceholderText = "Cuenta o departamento",
                Text = desembolsoExistente?.ConCargoA ?? "",
                MaxLength = 200
            };

            var conceptoBox = new TextBox
            {
                Header = "Por concepto de *",
                PlaceholderText = "Descripción detallada del desembolso",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 100,
                Text = desembolsoExistente?.Concepto ?? ""
            };

            var formPanel = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    numeroReciboBox,
                    fechaPicker,
                    pagadoABox,
                    montoBox,
                    conCargoABox,
                    conceptoBox
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
                Title = esEdicion ? "Editar Desembolso" : "Nuevo Desembolso",
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
                if (numeroReciboBox.Value <= 0 || double.IsNaN(numeroReciboBox.Value))
                {
                    await ShowInfoDialog("Error", "Debe ingresar un número de recibo válido");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(pagadoABox.Text))
                {
                    await ShowInfoDialog("Error", "Debe ingresar a quién se le pagó");
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

                return new CajaChica
                {
                    NumeroRecibo = (int)numeroReciboBox.Value,
                    Fecha = fechaPicker.Date.Value.DateTime,
                    PagadoA = pagadoABox.Text.Trim(),
                    Monto = (decimal)montoBox.Value,
                    ConCargoA = string.IsNullOrWhiteSpace(conCargoABox.Text) ? null : conCargoABox.Text.Trim(),
                    Concepto = string.IsNullOrWhiteSpace(conceptoBox.Text) ? null : conceptoBox.Text.Trim()
                };
            }

            return null;
        }
    }
}
