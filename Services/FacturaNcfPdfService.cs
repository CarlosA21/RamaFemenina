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
    /// Servicio para generar facturas con NCF en PDF
    /// Tamaño: Media Carta (8.5" x 5.5") - Horizontal
    /// </summary>
    public class FacturaNcfPdfService
    {
        // Tamaño Media Carta: 8.5" ancho x 5.5" alto (612 x 396 puntos)
        private static readonly PageSize MEDIA_CARTA = new PageSize(612f, 396f);
        
        private const float MARGIN_LEFT = 25f;
        private const float MARGIN_RIGHT = 25f;
        private const float MARGIN_TOP = 15f;
        private const float MARGIN_BOTTOM = 15f;

        /// <summary>
        /// Genera un PDF de la factura NCF y lo retorna como array de bytes
        /// </summary>
        public async Task<byte[]> GenerarFacturaPdfAsync(FacturaNcf factura, string logoPath = null)
        {
            return await Task.Run(() =>
            {
                using var memoryStream = new MemoryStream();
                
                var writer = new PdfWriter(memoryStream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf, MEDIA_CARTA);
                
                document.SetMargins(MARGIN_TOP, MARGIN_RIGHT, MARGIN_BOTTOM, MARGIN_LEFT);

                var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                if (string.IsNullOrEmpty(logoPath))
                {
                    logoPath = BuscarYConvertirLogo();
                }

                // SECCIÓN 1: ENCABEZADO
                AgregarEncabezadoFactura(document, factura, fontBold, fontRegular, logoPath);

                // SECCIÓN 2: DATOS DEL CLIENTE
                AgregarDatosCliente(document, factura, fontBold, fontRegular);

                // SECCIÓN 3: TABLA DE CONCEPTOS
                AgregarTablaConceptos(document, factura, fontBold, fontRegular);

                // SECCIÓN 4: PIE CON TÉRMINOS Y TOTALES
                AgregarPieFactura(document, factura, fontBold, fontRegular);

                document.Close();
                return memoryStream.ToArray();
            });
        }

        private string BuscarYConvertirLogo()
        {
            string appDirectory = AppContext.BaseDirectory;
            
            string[] posiblesPng = {
                IOPath.Combine(appDirectory, "Assets", "icono2.png"),
                IOPath.Combine(appDirectory, "Assets", "logo.png"),
                IOPath.Combine(appDirectory, "icono2.png"),
                IOPath.Combine(appDirectory, "logo.png")
            };

            foreach (var rutaPng in posiblesPng)
            {
                if (File.Exists(rutaPng))
                    return rutaPng;
            }

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
                        string rutaPng = IOPath.ChangeExtension(rutaIco, ".png");
                        if (File.Exists(rutaPng))
                            return rutaPng;

                        using (var icon = new Icon(rutaIco))
                        using (var bitmap = icon.ToBitmap())
                        {
                            bitmap.Save(rutaPng, ImageFormat.Png);
                            return rutaPng;
                        }
                    }
                    catch { }
                }
            }

            return null;
        }

        private void AgregarEncabezadoFactura(Document document, FacturaNcf factura, PdfFont fontBold, PdfFont fontRegular, string logoPath)
        {
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 1.2f, 2.5f, 1.8f }));
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Logo en cuadro superior izquierdo
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                try
                {
                    var logo = new PdfImage(ImageDataFactory.Create(logoPath));
                    // Tamaño más grande para llenar mejor el cuadro
                    logo.ScaleToFit(75f, 75f);
                    
                    var celdaLogo = new Cell()
                        .Add(logo)
                        .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1f))
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(8)
                        .SetHeight(80f);
                    
                    table.AddCell(celdaLogo);
                }
                catch
                {
                    // Si falla, mostrar cuadro vacío
                    table.AddCell(new Cell()
                        .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1f))
                        .SetHeight(80f));
                }
            }
            else
            {
                // Cuadro vacío si no hay logo
                table.AddCell(new Cell()
                    .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1f))
                    .SetHeight(80f));
            }

            // Datos empresa (columna central)
            var empresaCell = new Cell()
                .Add(new Paragraph("Rama Femenina")
                    .SetFont(fontBold)
                    .SetFontSize(9)
                    .SetMarginBottom(0))
                .Add(new Paragraph("Contra el Cáncer, Inc.")
                    .SetFont(fontBold)
                    .SetFontSize(8.5f)
                    .SetMarginBottom(1))
                .Add(new Paragraph("Desde 1964")
                    .SetFont(fontRegular)
                    .SetFontSize(6)
                    .SetItalic()
                    .SetMarginBottom(2))
                .Add(new Paragraph("Calle Dr. Flavio D. Espinal, esq. A #1, Reparto Ciret, Santiago, R.D.")
                    .SetFont(fontRegular)
                    .SetFontSize(6)
                    .SetMarginBottom(1))
                .Add(new Paragraph("Tels.: 809-582-3939 / 809-226-1178")
                    .SetFont(fontRegular)
                    .SetFontSize(6)
                    .SetMarginBottom(2))
                .Add(new Paragraph("RNC: 4-30-10692-5")
                    .SetFont(fontBold)
                    .SetFontSize(7)
                    .SetMarginBottom(1))
                .Add(new Paragraph($"Fecha: {factura.Fecha:dd/MM/yyyy}")
                    .SetFont(fontRegular)
                    .SetFontSize(7))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPaddingLeft(8)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            table.AddCell(empresaCell);

            // NCF y Validez (columna derecha)
            var ncfCell = new Cell()
                .Add(new Paragraph("Facturas que generan crédito y sustentan costos y/o gastos")
                    .SetFont(fontRegular)
                    .SetFontSize(6)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(3))
                .Add(new Paragraph($"NCF: {factura.NCF}")
                    .SetFont(fontBold)
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(2))
                .Add(new Paragraph($"Válida hasta: {factura.ValidaHasta:dd/MM/yyyy}")
                    .SetFont(fontRegular)
                    .SetFontSize(7)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            table.AddCell(ncfCell);

            document.Add(table);
            
            document.Add(new Paragraph()
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1.5f))
                .SetMarginTop(2)
                .SetMarginBottom(3));
        }

        private void AgregarDatosCliente(Document document, FacturaNcf factura, PdfFont fontBold, PdfFont fontRegular)
        {
            // Primera línea compacta: RNC cliente y Teléfono
            var linea1 = new Paragraph()
                .Add(new Text($"RNC cliente: {factura.RncCliente}").SetFont(fontRegular).SetFontSize(7))
                .Add(new Text("                                                                                 ").SetFont(fontRegular).SetFontSize(7))
                .Add(new Text($"{factura.TelefonoCliente}").SetFont(fontRegular).SetFontSize(7))
                .SetMarginBottom(2);

            document.Add(linea1);

            // Segunda línea: Nombre del cliente
            document.Add(new Paragraph(factura.NombreCliente)
                .SetFont(fontBold)
                .SetFontSize(8)
                .SetMarginBottom(2));

            // Tercera línea: Dirección + "THIS IS A TEST"
            var linea3 = new Paragraph(factura.DireccionCliente)
                .SetFont(fontRegular)
                .SetFontSize(7)
                .SetMarginBottom(4);

            document.Add(linea3);

            // Línea separadora
            document.Add(new Paragraph()
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetMarginBottom(3));
        }

        private void AgregarTablaConceptos(Document document, FacturaNcf factura, PdfFont fontBold, PdfFont fontRegular)
        {
            var tabla = new Table(UnitValue.CreatePercentArray(new float[] { 1f, 3.5f, 1.2f, 1.2f, 1.2f }));
            tabla.SetWidth(UnitValue.CreatePercentValue(100));

            // Encabezados
            string[] headers = { "Cantidad", "Descripción", "Precio", "Subtotal", "Total RD$" };
            foreach (var header in headers)
            {
                tabla.AddCell(new Cell()
                    .Add(new Paragraph(header)
                        .SetFont(fontBold)
                        .SetFontSize(7)
                        .SetTextAlignment(TextAlignment.CENTER))
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                    .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                    .SetPadding(2));
            }

            // Fila del concepto principal
            tabla.AddCell(new Cell()
                .Add(new Paragraph("1")
                    .SetFont(fontRegular)
                    .SetFontSize(7)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(2));

            tabla.AddCell(new Cell()
                .Add(new Paragraph(factura.Concepto)
                    .SetFont(fontRegular)
                    .SetFontSize(7))
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(2));

            tabla.AddCell(new Cell()
                .Add(new Paragraph($"RD$ {factura.Monto:N2}")
                    .SetFont(fontRegular)
                    .SetFontSize(7)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(2));

            tabla.AddCell(new Cell()
                .Add(new Paragraph($"RD$ {factura.Monto:N2}")
                    .SetFont(fontRegular)
                    .SetFontSize(7)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(2));

            tabla.AddCell(new Cell()
                .Add(new Paragraph($"RD$ {factura.Monto:N2}")
                    .SetFont(fontBold)
                    .SetFontSize(7)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(2));

            document.Add(tabla);
        }

        private void AgregarPieFactura(Document document, FacturaNcf factura, PdfFont fontBold, PdfFont fontRegular)
        {
            document.Add(new Paragraph("\n").SetFontSize(4));

            // Tabla inferior: Términos de pago y Totales
            var tablaPie = new Table(UnitValue.CreatePercentArray(new float[] { 1f, 1f }));
            tablaPie.SetWidth(UnitValue.CreatePercentValue(100));

            // Columna izquierda: Términos de pago
            var terminosCell = new Cell()
                .Add(new Paragraph("Términos de pago")
                    .SetFont(fontBold)
                    .SetFontSize(7)
                    .SetMarginBottom(3))
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(5);

            // Checkboxes más compactos
            var checkCheque = factura.EsCheque ? "☑" : "☐";
            var checkEfectivo = factura.EsEfectivo ? "☑" : "☐";
            var checkCredito = factura.EsCredito ? "☑" : "☐";

            terminosCell.Add(new Paragraph($"{checkCheque} Cheque:")
                .SetFont(fontRegular)
                .SetFontSize(6)
                .SetMarginBottom(1));
            terminosCell.Add(new Paragraph($"    No.: {factura.NumeroCheque ?? ""}")
                .SetFont(fontRegular)
                .SetFontSize(6)
                .SetMarginBottom(1));
            terminosCell.Add(new Paragraph($"    Banco: {factura.Banco ?? ""}")
                .SetFont(fontRegular)
                .SetFontSize(6)
                .SetMarginBottom(2));

            terminosCell.Add(new Paragraph($"{checkEfectivo} Pago en efectivo:")
                .SetFont(fontRegular)
                .SetFontSize(6)
                .SetMarginBottom(1));
            terminosCell.Add(new Paragraph($"    Efectivo: RD$ {(factura.EsEfectivo ? factura.Monto : 0):N2}")
                .SetFont(fontRegular)
                .SetFontSize(6)
                .SetMarginBottom(1));
            terminosCell.Add(new Paragraph($"    Cambio: RD$ 0.00")
                .SetFont(fontRegular)
                .SetFontSize(6)
                .SetMarginBottom(2));

            terminosCell.Add(new Paragraph($"{checkCredito} Factura a Crédito:")
                .SetFont(fontRegular)
                .SetFontSize(6));

            tablaPie.AddCell(terminosCell);

            // Columna derecha: Totales
            var totalesCell = new Cell()
                .Add(new Paragraph($"Total exento: RD$ {factura.Monto:N2}")
                    .SetFont(fontRegular)
                    .SetFontSize(7)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(2))
                .Add(new Paragraph($"Total Gravado: RD$ 0.00")
                    .SetFont(fontRegular)
                    .SetFontSize(7)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(2))
                .Add(new Paragraph($"Itbis: RD$ 0.00")
                    .SetFont(fontRegular)
                    .SetFontSize(7)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(4))
                .Add(new Paragraph($"Total Neto: RD$ {factura.Monto:N2}")
                    .SetFont(fontBold)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(5)
                .SetVerticalAlignment(VerticalAlignment.TOP);

            tablaPie.AddCell(totalesCell);

            document.Add(tablaPie);

            // Línea negra gruesa separadora
            document.Add(new Paragraph()
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 3f))
                .SetMarginTop(4)
                .SetMarginBottom(10));

            // Tabla con 2 columnas para las firmas lado a lado
            var tablaFirmas = new Table(UnitValue.CreatePercentArray(new float[] { 1f, 1f }));
            tablaFirmas.SetWidth(UnitValue.CreatePercentValue(100));

            // Columna izquierda: Recibido por
            var celdaRecibidoPor = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER);

            var lineaRecibidoPor = new Paragraph()
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1f))
                .SetWidth(UnitValue.CreatePercentValue(70))
                .SetMarginBottom(2)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);

            celdaRecibidoPor.Add(lineaRecibidoPor);
            celdaRecibidoPor.Add(new Paragraph("Recibido por")
                .SetFont(fontBold)
                .SetFontSize(7)
                .SetTextAlignment(TextAlignment.CENTER));

            tablaFirmas.AddCell(celdaRecibidoPor);

            // Columna derecha: Por Rama Femenina
            var celdaRamaFemenina = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER);

            var lineaRamaFemenina = new Paragraph()
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1f))
                .SetWidth(UnitValue.CreatePercentValue(70))
                .SetMarginBottom(2)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);

            celdaRamaFemenina.Add(lineaRamaFemenina);
            celdaRamaFemenina.Add(new Paragraph("Por Rama Femenina Contra El Cancer")
                .SetFont(fontBold)
                .SetFontSize(7)
                .SetTextAlignment(TextAlignment.CENTER));

            tablaFirmas.AddCell(celdaRamaFemenina);

            document.Add(tablaFirmas);
        }

        public async Task<string> GuardarFacturaPdfAsync(FacturaNcf factura, string rutaDestino, string logoPath = null)
        {
            var pdfBytes = await GenerarFacturaPdfAsync(factura, logoPath);
            await File.WriteAllBytesAsync(rutaDestino, pdfBytes);
            return rutaDestino;
        }

        public async Task AbrirFacturaPdfAsync(FacturaNcf factura, string logoPath = null)
        {
            var tempPath = IOPath.Combine(IOPath.GetTempPath(), $"Factura_NCF_{factura.NCF}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            await GuardarFacturaPdfAsync(factura, tempPath, logoPath);
            
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            };
            
            System.Diagnostics.Process.Start(processStartInfo);
        }
    }

    /// <summary>
    /// Modelo para Factura con NCF
    /// </summary>
    public class FacturaNcf
    {
        public string NCF { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public DateTime ValidaHasta { get; set; } = DateTime.Now.AddMonths(1);
        
        // Datos del cliente
        public string RncCliente { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string TelefonoCliente { get; set; } = string.Empty;
        public string DireccionCliente { get; set; } = string.Empty;
        
        // Concepto y monto
        public string Concepto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        
        // Forma de pago
        public bool EsCheque { get; set; }
        public bool EsEfectivo { get; set; }
        public bool EsCredito { get; set; }
        public string? NumeroCheque { get; set; }
        public string? Banco { get; set; }
    }
}
