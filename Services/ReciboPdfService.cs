using System;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Geom;
using RamaFemenina.Models;

// Alias para evitar conflictos
using PdfImage = iText.Layout.Element.Image;
using DrawingImage = System.Drawing.Image;
using IOPath = System.IO.Path;

namespace RamaFemenina.Services
{
    /// <summary>
    /// Servicio para generar recibos en PDF con diseño profesional
    /// Tamaño: Media Carta (8.5" x 5.5") - Horizontal
    /// </summary>
    public class ReciboPdfService
    {
        // Tamaño Media Carta: 8.5" ancho x 5.5" alto (612 x 396 puntos)
        private static readonly PageSize MEDIA_CARTA = new PageSize(612f, 396f);
        
        private const float MARGIN_LEFT = 30f;
        private const float MARGIN_RIGHT = 30f;
        private const float MARGIN_TOP = 20f;
        private const float MARGIN_BOTTOM = 20f;

        /// <summary>
        /// Genera un PDF del recibo y lo retorna como array de bytes
        /// </summary>
        public async Task<byte[]> GenerarReciboPdfAsync(Recibo recibo, string logoPath = null)
        {
            return await Task.Run(() =>
            {
                using var memoryStream = new MemoryStream();
                
                // Crear documento PDF en tamaño Media Carta (8.5" x 5.5")
                var writer = new PdfWriter(memoryStream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf, MEDIA_CARTA);
                
                // Configurar márgenes reducidos para aprovechar el espacio
                document.SetMargins(MARGIN_TOP, MARGIN_RIGHT, MARGIN_BOTTOM, MARGIN_LEFT);

                // Fuentes
                var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                // Buscar logo
                if (string.IsNullOrEmpty(logoPath))
                {
                    logoPath = BuscarYConvertirLogo();
                }

                // ============================================================
                // SECCIÓN 1: ENCABEZADO COMPACTO
                // ============================================================
                AgregarEncabezadoCompacto(document, fontBold, fontRegular, logoPath);

                // ============================================================
                // SECCIÓN 2: TÍTULO Y NÚMERO DE RECIBO
                // ============================================================
                AgregarTituloRecibo(document, recibo, fontBold, fontRegular);

                // ============================================================
                // SECCIÓN 3: DATOS DEL RECIBO
                // ============================================================
                AgregarDatosRecibo(document, recibo, fontBold, fontRegular);

                // ============================================================
                // SECCIÓN 4: PIE DEL RECIBO
                // ============================================================
                AgregarPieRecibo(document, fontBold, logoPath);

                document.Close();
                return memoryStream.ToArray();
            });
        }

        /// <summary>
        /// Busca el logo en múltiples ubicaciones y formatos
        /// </summary>
        private string BuscarYConvertirLogo()
        {
            string appDirectory = AppContext.BaseDirectory;
            System.Diagnostics.Debug.WriteLine($"[ReciboPdfService] 📁 Directorio de aplicación: {appDirectory}");

            // Buscar PNG
            string[] posiblesPng = {
                IOPath.Combine(appDirectory, "Assets", "icono2.png"),
                IOPath.Combine(appDirectory, "Assets", "logo.png"),
                IOPath.Combine(appDirectory, "icono2.png"),
                IOPath.Combine(appDirectory, "logo.png")
            };

            foreach (var rutaPng in posiblesPng)
            {
                if (File.Exists(rutaPng))
                {
                    System.Diagnostics.Debug.WriteLine($"[ReciboPdfService] ✅ Logo PNG encontrado: {rutaPng}");
                    return rutaPng;
                }
            }

            // Buscar JPG
            string[] posiblesJpg = {
                IOPath.Combine(appDirectory, "Assets", "icono2.jpg"),
                IOPath.Combine(appDirectory, "Assets", "logo.jpg"),
                IOPath.Combine(appDirectory, "icono2.jpg")
            };

            foreach (var rutaJpg in posiblesJpg)
            {
                if (File.Exists(rutaJpg))
                {
                    System.Diagnostics.Debug.WriteLine($"[ReciboPdfService] ✅ Logo JPG encontrado: {rutaJpg}");
                    return rutaJpg;
                }
            }

            // Buscar ICO y convertir
            string[] posiblesIco = {
                IOPath.Combine(appDirectory, "Assets", "icono2.ico"),
                IOPath.Combine(appDirectory, "icono2.ico")
            };

            foreach (var rutaIco in posiblesIco)
            {
                if (File.Exists(rutaIco))
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[ReciboPdfService] 🔄 Convirtiendo ICO a PNG...");
                        string rutaPngConvertido = ConvertirIcoAPng(rutaIco);
                        
                        if (!string.IsNullOrEmpty(rutaPngConvertido) && File.Exists(rutaPngConvertido))
                        {
                            System.Diagnostics.Debug.WriteLine($"[ReciboPdfService] ✅ Logo convertido: {rutaPngConvertido}");
                            return rutaPngConvertido;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ReciboPdfService] ⚠️ Error convirtiendo ICO: {ex.Message}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[ReciboPdfService] ❌ No se encontró ningún logo");
            return null;
        }

        /// <summary>
        /// Convierte un archivo ICO a PNG
        /// </summary>
        private string ConvertirIcoAPng(string rutaIco)
        {
            try
            {
                string rutaPng = IOPath.ChangeExtension(rutaIco, ".png");
                
                if (File.Exists(rutaPng))
                {
                    return rutaPng;
                }

                using (var icon = new Icon(rutaIco))
                using (var bitmap = icon.ToBitmap())
                {
                    bitmap.Save(rutaPng, ImageFormat.Png);
                    return rutaPng;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReciboPdfService] ❌ Error en ConvertirIcoAPng: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Encabezado compacto para Media Carta
        /// </summary>
        private void AgregarEncabezadoCompacto(Document document, PdfFont fontBold, PdfFont fontRegular, string logoPath)
        {
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 1f, 3f, 2.5f }));
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Logo
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                try
                {
                    var logo = new PdfImage(ImageDataFactory.Create(logoPath));
                    logo.ScaleToFit(40f, 40f);
                    
                    table.AddCell(new Cell()
                        .Add(logo)
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetPaddingLeft(0));
                }
                catch
                {
                    table.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                }
            }
            else
            {
                table.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            }

            // Nombre empresa
            table.AddCell(new Cell()
                .Add(new Paragraph("Rama Femenina")
                    .SetFont(fontBold)
                    .SetFontSize(10)
                    .SetMarginBottom(2))
                .Add(new Paragraph("Contra el Cáncer, Inc.")
                    .SetFont(fontBold)
                    .SetFontSize(10))
                .Add(new Paragraph("Desde 1964")
                    .SetFont(fontRegular)
                    .SetFontSize(7)
                    .SetMarginTop(2))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetPaddingLeft(5));

            // Datos contacto
            table.AddCell(new Cell()
                .Add(new Paragraph("Calle Pedro Francisco Bonó No. 33, Santiago, R.D.")
                    .SetFont(fontRegular)
                    .SetFontSize(7)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(1))
                .Add(new Paragraph("Tels.: 809-582-3939 / 809-226-1178")
                    .SetFont(fontRegular)
                    .SetFontSize(7)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(1))
                .Add(new Paragraph("RNC: 4-30-10692-5")
                    .SetFont(fontBold)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            document.Add(table);
            
            // Línea separadora
            document.Add(new Paragraph()
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1.5f))
                .SetMarginTop(5)
                .SetMarginBottom(5));
        }

        /// <summary>
        /// Título y número de recibo
        /// </summary>
        private void AgregarTituloRecibo(Document document, Recibo recibo, PdfFont fontBold, PdfFont fontRegular)
        {
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 2.5f, 1.5f }));
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Título
            table.AddCell(new Cell()
                .Add(new Paragraph("Programa Social Para Pacientes Oncológicos")
                    .SetFont(fontBold)
                    .SetFontSize(10))
                .Add(new Paragraph($"Recibo de Ingreso")
                    .SetFont(fontBold)
                    .SetFontSize(12)
                    .SetMarginTop(3))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            // Número y fecha
            table.AddCell(new Cell()
                .Add(new Paragraph($"No. {recibo.NumeroRecibo}")
                    .SetFont(fontBold)
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .Add(new Paragraph($"Fecha: {recibo.FechaFormateada}")
                    .SetFont(fontRegular)
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginTop(3))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            document.Add(table);
            
            document.Add(new Paragraph()
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1f))
                .SetMarginTop(3)
                .SetMarginBottom(8));
        }

        /// <summary>
        /// Datos del recibo
        /// </summary>
        private void AgregarDatosRecibo(Document document, Recibo recibo, PdfFont fontBold, PdfFont fontRegular)
        {
            // Recibimos de
            document.Add(new Paragraph()
                .Add(new Text("Hemos recibido de: ").SetFont(fontBold).SetFontSize(9))
                .Add(new Text(recibo.RecibimosDe ?? "").SetFont(fontRegular).SetFontSize(9))
                .SetMarginBottom(5));

            // Monto
            document.Add(new Paragraph()
                .Add(new Text("La suma de RD$ ").SetFont(fontBold).SetFontSize(9))
                .Add(new Text($"{recibo.Monto:N2}").SetFont(fontBold).SetFontSize(11).SetFontColor(new DeviceRgb(0, 100, 0)))
                .SetMarginBottom(3));

            // Monto en letras
            var montoLetras = !string.IsNullOrEmpty(recibo.MontoEnLetras) 
                ? recibo.MontoEnLetras 
                : ConvertirNumeroALetras(recibo.Monto);
            document.Add(new Paragraph(montoLetras)
                .SetFont(fontRegular)
                .SetFontSize(8)
                .SetMarginBottom(5));

            // Concepto
            document.Add(new Paragraph()
                .Add(new Text("Por concepto de: ").SetFont(fontBold).SetFontSize(9))
                .Add(new Text(recibo.Concepto ?? "").SetFont(fontRegular).SetFontSize(9))
                .SetMarginBottom(8));

            // SECCIÓN DE MÉTODO DE PAGO CON CUADROS (como en la imagen)
            AgregarSeccionMetodoPago(document, recibo, fontBold, fontRegular);
        }

        /// <summary>
        /// Agrega la sección de método de pago con cuadros/checkboxes como en el formato original
        /// </summary>
        private void AgregarSeccionMetodoPago(Document document, Recibo recibo, PdfFont fontBold, PdfFont fontRegular)
        {
            // Crear tabla con 2 filas y más columnas para incluir el No. de transferencia
            var tablaPago = new Table(UnitValue.CreatePercentArray(new float[] { 0.7f, 0.8f, 0.7f, 0.8f, 0.5f, 1.2f, 1.2f, 2f }));
            tablaPago.SetWidth(UnitValue.CreatePercentValue(100));

            // ====== FILA 1: Efectivo | Checkbox | Transf. | Checkbox | No.: | Valor | No. fact. NCF | Valor ======
            
            // Celda "Efectivo"
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph("Efectivo").SetFont(fontRegular).SetFontSize(8))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Checkbox Efectivo
            tablaPago.AddCell(new Cell()
                .Add(CrearCuadroCheckbox(recibo.EsEfectivo))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Celda "Transf."
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph("Transf.").SetFont(fontRegular).SetFontSize(8))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Checkbox Transferencia
            tablaPago.AddCell(new Cell()
                .Add(CrearCuadroCheckbox(recibo.EsTransferencia))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // No.: (para transferencia)
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph("No.:").SetFont(fontRegular).SetFontSize(8))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Número de cotejo/comprobante de transferencia con línea
            // Mostrar el número si es transferencia Y tiene valor
            var numTransferencia = recibo.EsTransferencia ? (recibo.NumeroCheque ?? "") : "";
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph(numTransferencia)
                    .SetFont(fontRegular)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetBorderTop(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderLeft(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderRight(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // No. fact. NCF:
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph("No. fact. NCF:").SetFont(fontRegular).SetFontSize(8))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Valor NCF con línea inferior
            var ncfValue = !string.IsNullOrEmpty(recibo.NumeroFacturaNCF) ? recibo.NumeroFacturaNCF : "";
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph(ncfValue)
                    .SetFont(fontRegular)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetBorderTop(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderLeft(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderRight(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // ====== FILA 2: Cheque | Checkbox | (colspan) | No.: | Valor | Banco: | Valor ======
            
            // Celda "Cheque"
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph("Cheque").SetFont(fontRegular).SetFontSize(8))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Checkbox Cheque
            tablaPago.AddCell(new Cell()
                .Add(CrearCuadroCheckbox(recibo.EsCheque))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Espacio vacío (colspan 2)
            tablaPago.AddCell(new Cell(1, 2)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            // No.: (para cheque)
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph("No.:").SetFont(fontRegular).SetFontSize(8))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Número de cheque con línea
            // Mostrar el número si es cheque Y tiene valor
            var numCheque = recibo.EsCheque ? (recibo.NumeroCheque ?? "") : "";
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph(numCheque)
                    .SetFont(fontRegular)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetBorderTop(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderLeft(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderRight(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Banco:
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph("Banco:").SetFont(fontRegular).SetFontSize(8))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Nombre del banco con línea
            // Mostrar el banco si tiene valor (aplica para cheques y transferencias)
            var banco = !string.IsNullOrEmpty(recibo.Banco) ? recibo.Banco : "";
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph(banco)
                    .SetFont(fontRegular)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetBorderTop(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderLeft(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderRight(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            document.Add(tablaPago);
        }

        /// <summary>
        /// Crea un cuadro/checkbox visual (similar a los checkboxes del formato original)
        /// </summary>
        private Paragraph CrearCuadroCheckbox(bool marcado)
        {
            // Crear un pequeño cuadro con borde
            var cuadro = new Paragraph()
                .SetWidth(12f)
                .SetHeight(12f)
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1f))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetMarginLeft(5)
                .SetMargin(0)
                .SetPadding(0);

            // Si está marcado, agregar una "X" más visible
            if (marcado)
            {
                cuadro.Add(new Text("X")
                    .SetFontSize(9)
                    .SetBold());
            }

            return cuadro;
        }

        /// <summary>
        /// Pie con firma y logo de la organización
        /// </summary>
        private void AgregarPieRecibo(Document document, PdfFont fontBold, string logoPath = null)
        {
            // Espacio antes del pie
            document.Add(new Paragraph("\n\n"));

            // Tabla para la firma alineada a la derecha
            var tablaFirma = new Table(1);
            tablaFirma.SetWidth(UnitValue.CreatePercentValue(40));
            tablaFirma.SetHorizontalAlignment(HorizontalAlignment.RIGHT);

            tablaFirma.AddCell(new Cell()
                .Add(new Paragraph()
                    .SetBorderTop(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1f))
                    .SetMarginBottom(3))
                .Add(new Paragraph("Recibido por")
                    .SetFont(fontBold)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            document.Add(tablaFirma);
        }

        /// <summary>
        /// Crea una celda sin borde
        /// </summary>
        private Cell CrearCeldaSinBorde(string texto)
        {
            return new Cell()
                .Add(new Paragraph(texto))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);
        }

        /// <summary>
        /// Convierte un número decimal a su representación en letras
        /// </summary>
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

            if (numero >= 1000000)
            {
                int millones = numero / 1000000;
                int resto = numero % 1000000;
                string textoMillones = millones == 1 ? "Un millón" : ConvertirEnteroALetras(millones) + " millones";
                if (resto > 0) return textoMillones + " " + ConvertirEnteroALetras(resto);
                return textoMillones;
            }

            if (numero >= 1000)
            {
                int miles = numero / 1000;
                int resto = numero % 1000;
                string textoMiles = miles == 1 ? "Mil" : ConvertirEnteroALetras(miles) + " mil";
                if (resto > 0) return textoMiles + " " + ConvertirEnteroALetras(resto);
                return textoMiles;
            }

            if (numero >= 100) return ConvertirCentenas(numero);
            return ConvertirDecenas(numero);
        }

        /// <summary>
        /// Convierte números de 100 a 999 a letras
        /// </summary>
        private string ConvertirCentenas(int numero)
        {
            string[] cientos = {
                "", "Ciento", "Doscientos", "Trescientos", "Cuatrocientos",
                "Quinientos", "Seiscientos", "Setecientos", "Ochocientos", "Novecientos"
            };

            int c = numero / 100;
            int resto = numero % 100;

            if (numero == 100) return "Cien";
            string resultado = cientos[c];
            if (resto > 0) resultado += " " + ConvertirDecenas(resto);
            return resultado;
        }

        /// <summary>
        /// Convierte números de 0 a 99 a letras
        /// </summary>
        private string ConvertirDecenas(int numero)
        {
            string[] unidades = { "", "Uno", "Dos", "Tres", "Cuatro", "Cinco", "Seis", "Siete", "Ocho", "Nueve" };
            string[] decenas = { "", "Diez", "Veinte", "Treinta", "Cuarenta", "Cincuenta", "Sesenta", "Setenta", "Ochenta", "Noventa" };
            string[] especiales = { "Diez", "Once", "Doce", "Trece", "Catorce", "Quince", "Dieciséis", "Diecisiete", "Dieciocho", "Diecinueve" };

            if (numero < 10) return unidades[numero];
            if (numero < 20) return especiales[numero - 10];
            if (numero < 30)
            {
                int u = numero % 10;
                return u == 0 ? "Veinte" : "Veinti" + unidades[u].ToLower();
            }
            if (numero < 100)
            {
                int d = numero / 10;
                int u = numero % 10;
                return u == 0 ? decenas[d] : decenas[d] + " y " + unidades[u];
            }
            return "";
        }

        /// <summary>
        /// Guarda el PDF en un archivo
        /// </summary>
        public async Task<string> GuardarReciboPdfAsync(Recibo recibo, string rutaDestino, string logoPath = null)
        {
            var pdfBytes = await GenerarReciboPdfAsync(recibo, logoPath);
            await File.WriteAllBytesAsync(rutaDestino, pdfBytes);
            return rutaDestino;
        }

        /// <summary>
        /// Abre el PDF en el visor predeterminado del sistema
        /// </summary>
        public async Task AbrirReciboPdfAsync(Recibo recibo, string logoPath = null)
        {
            var tempPath = IOPath.Combine(IOPath.GetTempPath(), $"Recibo_{recibo.NumeroRecibo}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            await GuardarReciboPdfAsync(recibo, tempPath, logoPath);
            
            // Abrir con el visor predeterminado de Windows
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            };
            
            System.Diagnostics.Process.Start(processStartInfo);
        }
    }
}
