using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Extensions.DependencyInjection;
using RamaFemenina.Services;
using RamaFemenina.Models;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RamaFemenina
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BlankPage1 : Page
    {
        private ReportManager? _reportManager;
        private FacturaService? _facturaService;

        public BlankPage1()
        {
            this.InitializeComponent();
            InitializeServices();
        }

        private async void InitializeServices()
        {
            try
            {
                var app = (App)Application.Current;
                _reportManager = await ReportManager.CreateAsync(app.Services);
                _facturaService = app.Services.GetService<FacturaService>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inicializando servicios: {ex.Message}");
            }
        }

        #region Event Handlers - Recibos

        private async void GenerarReciboIngresos_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarDatosBasicos()) return;

            var parameters = new ReportParameters
            {
                ReciboParms = new ReciboParametros
                {
                    NumeroRecibo = GetIntValue("txtNumeroRecibo"),
                    Fecha = DateTime.Now,
                    Nombre = GetTextBoxValue("txtNombre"),
                    Cedula = GetTextBoxValue("txtCedula"),
                    Monto = GetDecimalValue("txtMonto"),
                    MontoEnLetras = GetTextBoxValue("txtMontoLetras"),
                    Concepto = GetTextBoxValue("txtConcepto"),
                    NumeroCheque = GetTextBoxValue("txtNumeroCheque")
                }
            };

            await GenerarReporte(7, "Generando recibo de ingresos...", parameters);
        }

        private async void GenerarReciboCompleto_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarDatosBasicos()) return;

            var parameters = new ReportParameters
            {
                ReciboCompletoParms = new ReciboCompletoParametros
                {
                    NumeroRecibo = GetIntValue("txtNumeroRecibo"),
                    Fecha = DateTime.Now,
                    Nombre = GetTextBoxValue("txtNombre"),
                    Monto = GetDecimalValue("txtMonto"),
                    MontoEnLetras = GetTextBoxValue("txtMontoLetras"),
                    Concepto = GetTextBoxValue("txtConcepto"),
                    Efectivo = GetCheckBoxValue("chkEfectivo"),
                    Cheque = GetCheckBoxValue("chkCheque"),
                    Transferencia = GetCheckBoxValue("chkTransferencia"),
                    NumeroCheque = GetTextBoxValue("txtNumeroCheque"),
                    Banco = GetTextBoxValue("txtBanco"),
                    NCF = GetTextBoxValue("txtNCF")
                }
            };

            await GenerarReporte(8, "Generando recibo completo...", parameters);
        }

        private async void GenerarReciboDesembolso_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarDatosBasicos()) return;

            var cargoA = GetTextBoxValue("txtCargoA");
            if (string.IsNullOrEmpty(cargoA))
            {
                ShowMessage("Debe especificar a qué cuenta se carga el desembolso.");
                return;
            }

            var parameters = new ReportParameters
            {
                DesembolsoParms = new DesembolsoParametros
                {
                    NumeroRecibo = GetIntValue("txtNumeroRecibo"),
                    Fecha = DateTime.Now,
                    Nombre = GetTextBoxValue("txtNombre"),
                    Monto = GetDecimalValue("txtMonto"),
                    MontoEnLetras = GetTextBoxValue("txtMontoLetras"),
                    Concepto = GetTextBoxValue("txtConcepto"),
                    CargoA = cargoA
                }
            };

            await GenerarReporte(9, "Generando recibo de desembolso...", parameters);
        }

        #endregion

        #region Event Handlers - Facturas

        private async void GenerarFactura_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarDatosFactura()) return;

            try
            {
                ShowMessage("Generando factura fiscal...");
                SetLoadingState(true);

                var parametros = new FacturaParametros
                {
                    Fecha = DateTime.Now,
                    NCF = GetTextBoxValue("txtFacturaNCF"),
                    ValidaHasta = GetDatePickerValue("dpFacturaValidaHasta"),
                    RncCliente = GetTextBoxValue("txtFacturaRncCliente"),
                    NombreCliente = GetTextBoxValue("txtFacturaNombreCliente"),
                    TelefonoCliente = GetTextBoxValue("txtFacturaTelefono"),
                    DireccionCliente = GetTextBoxValue("txtFacturaDireccion"),
                    
                    // Agregar items de ejemplo (en una implementación real, vendrían de una lista)
                    Items = new System.Collections.Generic.List<FacturaItem>
                    {
                        new FacturaItem 
                        { 
                            Cantidad = 1, 
                            Descripcion = GetTextBoxValue("txtConcepto") ?? "Servicios médicos", 
                            Precio = GetDecimalValue("txtMonto"),
                            Itbis = GetDecimalValue("txtMonto") * 0.18m 
                        }
                    },
                    
                    // Formas de pago
                    EsEfectivo = GetCheckBoxValue("chkEfectivo"),
                    EsCheque = GetCheckBoxValue("chkCheque"),
                    NumeroCheque = GetTextBoxValue("txtNumeroCheque"),
                    Banco = GetTextBoxValue("txtBanco")
                };

                // Calcular totales
                var monto = GetDecimalValue("txtMonto");
                parametros.TotalGravado = monto;
                parametros.TotalItbis = monto * 0.18m;
                parametros.TotalNeto = monto + parametros.TotalItbis;

                if (_facturaService != null)
                {
                    var pdfBytes = await _facturaService.GenerarFacturaAsync(parametros);
                    var nombreArchivo = $"Factura_{parametros.NCF}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    await _facturaService.MostrarFacturaAsync(pdfBytes, nombreArchivo);
                    
                    ShowMessage("Factura generada exitosamente.");
                }
                else
                {
                    ShowMessage("El servicio de facturas no está disponible.");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Error generando factura: {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        #endregion

        #region Métodos de Validación

        private bool ValidarDatosFactura()
        {
            var rncCliente = GetTextBoxValue("txtFacturaRncCliente");
            var nombreCliente = GetTextBoxValue("txtFacturaNombreCliente");
            var ncf = GetTextBoxValue("txtFacturaNCF");

            if (string.IsNullOrEmpty(rncCliente))
            {
                ShowMessage("Debe ingresar el RNC del cliente.");
                return false;
            }

            if (string.IsNullOrEmpty(nombreCliente))
            {
                ShowMessage("Debe ingresar el nombre o razón social del cliente.");
                return false;
            }

            if (string.IsNullOrEmpty(ncf))
            {
                ShowMessage("Debe ingresar el número de comprobante fiscal (NCF).");
                return false;
            }

            // También validar que hay información de servicios/productos
            var monto = GetDecimalValue("txtMonto");
            if (monto <= 0)
            {
                ShowMessage("Debe ingresar un monto válido para la factura.");
                return false;
            }

            return true;
        }

        private bool ValidarDatosBasicos()
        {
            var numeroRecibo = GetIntValue("txtNumeroRecibo");
            var nombre = GetTextBoxValue("txtNombre");
            var monto = GetDecimalValue("txtMonto");
            var concepto = GetTextBoxValue("txtConcepto");

            if (numeroRecibo <= 0)
            {
                ShowMessage("Debe ingresar un número de recibo válido.");
                return false;
            }

            if (string.IsNullOrEmpty(nombre))
            {
                ShowMessage("Debe ingresar el nombre.");
                return false;
            }

            if (monto <= 0)
            {
                ShowMessage("Debe ingresar un monto válido mayor que cero.");
                return false;
            }

            if (string.IsNullOrEmpty(concepto))
            {
                ShowMessage("Debe especificar el concepto del recibo.");
                return false;
            }

            return true;
        }

        #endregion

        #region Métodos Auxiliares

        private void LimpiarFormulario_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("Formulario limpiado.");
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

        private int GetIntValue(string name)
        {
            var text = GetTextBoxValue(name);
            return int.TryParse(text, out int value) ? value : 0;
        }

        private decimal GetDecimalValue(string name)
        {
            var text = GetTextBoxValue(name);
            return decimal.TryParse(text, out decimal value) ? value : 0;
        }

        private bool GetCheckBoxValue(string name)
        {
            try
            {
                if (this.FindName(name) is CheckBox checkBox)
                {
                    return checkBox.IsChecked == true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private DateTime GetDatePickerValue(string name)
        {
            try
            {
                if (this.FindName(name) is DatePicker datePicker)
                {
                    return datePicker.Date.DateTime;
                }
                return DateTime.Now;
            }
            catch
            {
                return DateTime.Now;
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

                if (this.FindName("btnPanel") is Control btnPanel)
                {
                    btnPanel.IsEnabled = !isLoading;
                }
            }
            catch
            {
                // Ignore errors setting loading state
            }
        }

        #endregion
    }
}
