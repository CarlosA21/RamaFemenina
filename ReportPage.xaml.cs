using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using RamaFemenina.Services;
using RamaFemenina.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace RamaFemenina
{
    public sealed partial class ReportPage : Page
    {
        private ReportManager? _reportManager;
        private List<ReportHistoryItem> _reportHistory = new();
        private int _totalReportsGenerated = 0;
        private int _reportsToday = 0;
        private int _reportsThisMonth = 0;
        private DateTime? _lastReportTime = null;

        public ReportPage()
        {
            this.InitializeComponent();
            InitializeServices();
            // Cargar estadísticas después de que los controles estén inicializados
            this.Loaded += ReportPage_Loaded;
        }

        private void ReportPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
        }

        private async void InitializeServices()
        {
            try
            {
                var app = (App)Application.Current;
                _reportManager = await ReportManager.CreateAsync(app.Services);
                ShowMessage("? Sistema de reportes inicializado correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inicializando servicios: {ex.Message}");
                ShowMessage("? Error al inicializar el sistema de reportes");
            }
        }

        private void LoadStatistics()
        {
            try
            {
                // Cargar estadísticas (en producción, esto vendría de una base de datos)
                if (this.FindName("txtTotalReportes") is TextBlock totalReportes)
                    totalReportes.Text = _totalReportsGenerated.ToString();
                
                if (this.FindName("txtReportesHoy") is TextBlock reportesHoy)
                    reportesHoy.Text = _reportsToday.ToString();
                
                if (this.FindName("txtReportesMes") is TextBlock reportesMes)
                    reportesMes.Text = _reportsThisMonth.ToString();
                
                if (this.FindName("txtUltimoReporte") is TextBlock ultimoReporte)
                    ultimoReporte.Text = _lastReportTime?.ToString("HH:mm") ?? "--:--";

                // Mostrar panel de historial vacío si no hay reportes
                if (this.FindName("EmptyHistoryPanel") is StackPanel emptyPanel)
                    emptyPanel.Visibility = _reportHistory.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando estadísticas: {ex.Message}");
            }
        }

        #region Event Handlers - Reportes Generales

        private async void GenerarReporteArea_Click(object sender, RoutedEventArgs e)
        {
            await GenerarReporteConAnimacion(1, "?? Generando análisis por área geográfica...", "Análisis por Área Geográfica");
        }

        private async void GenerarReporteFallecidas_Click(object sender, RoutedEventArgs e)
        {
            await GenerarReporteConAnimacion(2, "?? Generando registro de fallecidas...", "Registro de Fallecidas");
        }

        private async void GenerarReporteActivas_Click(object sender, RoutedEventArgs e)
        {
            await GenerarReporteConAnimacion(4, "? Generando reporte de pacientes activas...", "Pacientes Activas");
        }

        private async void GenerarReporteFallecidasDetallado_Click(object sender, RoutedEventArgs e)
        {
            await GenerarReporteConAnimacion(5, "?? Generando análisis detallado...", "Análisis Detallado de Fallecidas");
        }

        private async void GenerarReporteDonaciones_Click(object sender, RoutedEventArgs e)
        {
            var idPaciente = GetTextBoxValue("txtIdPaciente");
            if (string.IsNullOrEmpty(idPaciente))
            {
                await ShowErrorDialog("Validación", "Debe ingresar la cédula del paciente.");
                return;
            }

            var parameters = new ReportParameters { IdPaciente = idPaciente };
            await GenerarReporteConAnimacion(3, $"?? Generando reporte de donaciones para {idPaciente}...", 
                                            $"Donaciones - {idPaciente}", parameters);
        }

        private async void GenerarReporteAreaAnio_Click(object sender, RoutedEventArgs e)
        {
            int anio = 2024;
            if (this.FindName("nbAnio") is NumberBox numberBox)
            {
                anio = (int)numberBox.Value;
            }

            if (anio < 2000 || anio > 2100)
            {
                await ShowErrorDialog("Validación", "Debe ingresar un año válido entre 2000 y 2100.");
                return;
            }

            var parameters = new ReportParameters { Anio = anio };
            await GenerarReporteConAnimacion(6, $"?? Generando análisis temporal del año {anio}...", 
                                            $"Análisis Temporal {anio}", parameters);
        }

        #endregion

        #region Event Handlers - UI Actions

        private void RefreshStats_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
            ShowMessage("? Estadísticas actualizadas");
        }

        private void ShowHistory_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a la pestaña de historial (implementar lógica de navegación)
            ShowMessage("?? Mostrando historial de reportes");
        }

        private async void ShowHelp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Ayuda - Centro de Reportes",
                Content = new ScrollViewer
                {
                    Content = new StackPanel
                    {
                        Spacing = 12,
                        Children =
                        {
                            new TextBlock 
                            { 
                                Text = "Guía de Uso del Sistema de Reportes",
                                FontWeight = new Windows.UI.Text.FontWeight(600),
                                FontSize = 16
                            },
                            new TextBlock 
                            { 
                                Text = "1. Reportes Generales: Genera reportes predefinidos con un solo clic.",
                                TextWrapping = TextWrapping.Wrap
                            },
                            new TextBlock 
                            { 
                                Text = "2. Reportes Personalizados: Filtra información específica según tus necesidades.",
                                TextWrapping = TextWrapping.Wrap
                            },
                            new TextBlock 
                            { 
                                Text = "3. Filtros Avanzados: Aplica rangos de fechas y selecciona formato de exportación.",
                                TextWrapping = TextWrapping.Wrap
                            },
                            new TextBlock 
                            { 
                                Text = "4. Historial: Accede a reportes previamente generados.",
                                TextWrapping = TextWrapping.Wrap
                            },
                            new TextBlock 
                            { 
                                Text = "\n?? Tip: Todos los reportes se generan en formato PDF optimizado para impresión.",
                                TextWrapping = TextWrapping.Wrap,
                                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"]
                            }
                        }
                    }
                },
                PrimaryButtonText = "Entendido",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("dpFechaInicio") is DatePicker fechaInicio)
                fechaInicio.SelectedDate = null;
            
            if (this.FindName("dpFechaFin") is DatePicker fechaFin)
                fechaFin.SelectedDate = null;
            
            if (this.FindName("cmbFormatoExport") is ComboBox formato)
                formato.SelectedIndex = 0;
            
            ShowMessage("? Filtros limpiados");
        }

        private void txtIdPaciente_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Validación en tiempo real
            var text = GetTextBoxValue("txtIdPaciente");
            if (this.FindName("btnReporteDonaciones") is Button btn)
            {
                btn.IsEnabled = !string.IsNullOrEmpty(text);
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            // El flyout maneja la confirmación
        }

        private void ConfirmClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _reportHistory.Clear();
            if (this.FindName("lvHistory") is ListView historyList)
                historyList.ItemsSource = null;
            
            if (this.FindName("EmptyHistoryPanel") is StackPanel emptyPanel)
                emptyPanel.Visibility = Visibility.Visible;
            
            ShowMessage("? Historial eliminado");
        }

        #endregion

        #region Métodos Auxiliares Mejorados

        private async Task GenerarReporteConAnimacion(int opcion, string mensaje, string nombreReporte, ReportParameters? parameters = null)
        {
            if (_reportManager == null)
            {
                await ShowErrorDialog("Error", "El sistema de reportes no está disponible.");
                return;
            }

            try
            {
                ShowMessage(mensaje);
                SetLoadingState(true);

                // No aplicamos filtros de fecha para evitar errores con ReportParameters
                await _reportManager.MostrarReporteAsync(opcion, parameters);
                
                // Actualizar estadísticas
                _totalReportsGenerated++;
                _reportsToday++;
                _reportsThisMonth++;
                _lastReportTime = DateTime.Now;
                
                // Agregar al historial
                _reportHistory.Insert(0, new ReportHistoryItem
                {
                    Name = nombreReporte,
                    GeneratedDate = DateTime.Now,
                    Type = opcion
                });

                LoadStatistics();
                
                ShowMessage($"? {nombreReporte} generado exitosamente");
                
                // Mostrar notificación de éxito
                await ShowSuccessNotification(nombreReporte);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generando reporte: {ex.Message}");
                ShowMessage($"? Error al generar reporte: {ex.Message}");
                await ShowErrorDialog("Error", $"No se pudo generar el reporte:\n{ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async Task ShowSuccessNotification(string reportName)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "? Reporte Generado",
                    Content = $"El reporte '{reportName}' se ha generado exitosamente.\n\nEl archivo PDF se ha abierto automáticamente.",
                    CloseButtonText = "Aceptar",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error mostrando notificación: {ex.Message}");
            }
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = $"? {title}",
                    Content = message,
                    CloseButtonText = "Aceptar",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error mostrando diálogo: {ex.Message}");
            }
        }

        private string GetTextBoxValue(string name)
        {
            try
            {
                if (this.FindName(name) is TextBox textBox)
                {
                    return textBox.Text?.Trim() ?? "";
                }
                return "";
            }
            catch
            {
                return "";
            }
        }

        private async Task GenerarReporte(int opcion, string mensaje, ReportParameters? parameters = null)
        {
            if (_reportManager == null)
            {
                ShowMessage("El sistema de reportes no está disponible.");
                return;
            }

            try
            {
                ShowMessage(mensaje);
                SetLoadingState(true);

                await _reportManager.MostrarReporteAsync(opcion, parameters);
                
                ShowMessage("Reporte generado exitosamente.");
            }
            catch (Exception ex)
            {
                ShowMessage($"Error generando reporte: {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void ShowMessage(string message)
        {
            try
            {
                if (this.FindName("txtStatus") is TextBlock statusText)
                {
                    statusText.Text = message;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Status: {message}");
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"Status: {message}");
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            try
            {
                if (this.FindName("progressRing") is ProgressRing progressRing)
                {
                    progressRing.IsActive = isLoading;
                }

                // Deshabilitar controles durante la carga
                if (this.FindName("ReportesGrid") is ItemsControl grid)
                {
                    grid.IsEnabled = !isLoading;
                }
            }
            catch
            {
                // Ignore errors setting loading state
            }
        }

        #endregion

        #region Clases Auxiliares

        private class ReportHistoryItem
        {
            public string Name { get; set; } = "";
            public DateTime GeneratedDate { get; set; }
            public int Type { get; set; }
        }

        #endregion
    }
}
