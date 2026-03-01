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
    /// Tamaño: Media Carta (5.5" x 8.5") - Vertical (Portrait)
    /// </summary>
    public class FacturaNcfPdfService
    {
        // Tamaño Media Carta: 5.5" ancho x 8.5" alto (396 x 612 puntos)
        private static readonly PageSize MEDIA_CARTA = new PageSize(500f, 612f);
        
        // Sin márgenes para impresoras matriciales Epson LX-350
        private const float MARGIN_LEFT = 0f;
        private const float MARGIN_RIGHT = 0f;
        private const float MARGIN_TOP = 0f;
        private const float MARGIN_BOTTOM = 0f;

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
                
                // Configuración específica para impresoras matriciales
                pdf.GetCatalog().SetViewerPreferences(
                    new iText.Kernel.Pdf.PdfViewerPreferences()
                        .SetPrintScaling(iText.Kernel.Pdf.PdfViewerPreferences.PdfViewerPreferencesConstants.NONE)
                        .SetPickTrayByPDFSize(true)
                        .SetDuplex(iText.Kernel.Pdf.PdfViewerPreferences.PdfViewerPreferencesConstants.SIMPLEX)
                );

                // Establecer tamaño exacto Media Carta sin márgenes
                pdf.SetDefaultPageSize(MEDIA_CARTA);
                var page = pdf.AddNewPage(MEDIA_CARTA);
                
                // Forzar que no haya margen de recorte
                page.SetTrimBox(new iText.Kernel.Geom.Rectangle(0, 0, MEDIA_CARTA.GetWidth(), MEDIA_CARTA.GetHeight()));
                page.SetMediaBox(new iText.Kernel.Geom.Rectangle(0, 0, MEDIA_CARTA.GetWidth(), MEDIA_CARTA.GetHeight()));

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
            
            // Priorizar el icono oficial de la factura
            string[] posiblesImgs = {
                IOPath.Combine(appDirectory, "Assets", "icono2.jpg"),
                IOPath.Combine(appDirectory, "icono2.jpg"),
                IOPath.Combine(appDirectory, "Assets", "icono2.png"),
                IOPath.Combine(appDirectory, "icono2.png")
            };

            foreach (var ruta in posiblesImgs)
            {
                if (File.Exists(ruta))
                    return ruta;
            }

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
            // Fila superior: logo (izquierda) y título (derecha)
            var filaSuperior = new Table(UnitValue.CreatePercentArray(new float[] { 1.2f, 2.8f })).UseAllAvailableWidth();

            // Columna izquierda: Logo y nombre en la misma línea
            var izquierdaCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                try
                {
                    // Crear una tabla interna para alinear logo y texto horizontalmente
                    var tablaLogoTexto = new Table(UnitValue.CreatePercentArray(new float[] { 0.4f, 1f })).UseAllAvailableWidth();
                    
                    // Celda del logo
                    var celdaLogo = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetPadding(0)
                        .SetMargin(0);
                    var logo = new PdfImage(ImageDataFactory.Create(logoPath));
                    logo.ScaleToFit(45f, 45f);
                    celdaLogo.Add(logo);
                    
                    // Celda del texto al lado del logo
                    var celdaTexto = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetPadding(0)
                        .SetMargin(0)
                        .SetPaddingLeft(2f);
                    celdaTexto.Add(new Paragraph("Rama Femenina")
                        .SetFont(fontBold).SetFontSize(8).SetMargin(0).SetMarginBottom(0.5f));
                    celdaTexto.Add(new Paragraph("Contra el Cáncer, Inc.")
                        .SetFont(fontBold).SetFontSize(8).SetMargin(0).SetMarginBottom(1f));
                    // Ajustar ligeramente a la izquierda para centrar con la línea anterior
                    celdaTexto.Add(new Paragraph("Desde 1951")
                        .SetFont(fontBold).SetFontSize(9).SetMargin(0).SetMarginLeft(12f));
                    
                    tablaLogoTexto.AddCell(celdaLogo);
                    tablaLogoTexto.AddCell(celdaTexto);
                    izquierdaCell.Add(tablaLogoTexto);
                }
                catch
                {
                    izquierdaCell.Add(new Paragraph(" ").SetFontSize(1));
                }
            }
            // Quitar espaciador para pegar el encabezado al borde superior
            filaSuperior.AddCell(izquierdaCell);

            // Columna derecha: Título grande multilinea
            var derechaCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            // Ajustar título según prefijo del NCF
            string tituloEncabezado;
            var ncfPrefijo = (factura.NCF ?? string.Empty).Trim().ToUpperInvariant();
            if (ncfPrefijo.StartsWith("B14"))
                tituloEncabezado = "Comprobantes para Regímenes Especiales";
            else if (ncfPrefijo.StartsWith("B15"))
                tituloEncabezado = "Comprobantes Gubernamentales";
            else
                tituloEncabezado = "Facturas de Crédito Fiscal";
            derechaCell.Add(new Paragraph(tituloEncabezado)
                .SetFont(fontBold).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT));
            filaSuperior.AddCell(derechaCell);

            // Añadir sin margen superior ni padding adicional para que el contenido quede arriba
            document.Add(filaSuperior.SetMarginTop(0f).SetMarginBottom(0f).SetPaddingLeft(6f).SetPaddingRight(6f));

            // Segunda fila: izquierda datos (dirección, tels, RNC, fecha) y derecha NCF / Valida hasta
            var filaInferior = new Table(UnitValue.CreatePercentArray(new float[] { 2.2f, 1.8f })).UseAllAvailableWidth();

            var datosIzquierda = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            datosIzquierda.Add(new Paragraph("Calle Dr. Flavio D. Espinal, esq. A #1, Reparto Oquet, Santiago, R.D.")
                .SetFont(fontRegular).SetFontSize(8).SetMarginBottom(0.6f));
            datosIzquierda.Add(new Paragraph("Tels.: 809-582-3939 / 809-226-1178")
                .SetFont(fontRegular).SetFontSize(8).SetMarginBottom(0.6f));
            datosIzquierda.Add(new Paragraph("RNC: 4-30-10692-5")
                .SetFont(fontBold).SetFontSize(8).SetMarginBottom(0.6f));
            datosIzquierda.Add(new Paragraph($"Fecha: {factura.Fecha:dd/MM/yyyy}")
                .SetFont(fontRegular).SetFontSize(8));
            filaInferior.AddCell(datosIzquierda);

            var datosDerecha = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            datosDerecha.Add(new Paragraph($"NCF: {factura.NCF}")
                .SetFont(fontBold).SetFontSize(12).SetTextAlignment(TextAlignment.RIGHT).SetMarginBottom(1));
            datosDerecha.Add(new Paragraph($"Válida hasta: {factura.ValidaHasta:dd/MM/yyyy}")
                .SetFont(fontBold).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT));
            filaInferior.AddCell(datosDerecha);

            document.Add(filaInferior.SetMarginTop(0f).SetMarginBottom(0.5f).SetPaddingLeft(6f).SetPaddingRight(6f));

            // Línea separadora inferior
            document.Add(new Paragraph()
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.8f))
                .SetMarginTop(0.2f)
                .SetMarginBottom(0.2f));
        }

        private void AgregarDatosCliente(Document document, FacturaNcf factura, PdfFont fontBold, PdfFont fontRegular)
        {
            // Marco (caja) que contiene los datos del cliente
            var cajaCliente = new Table(1).UseAllAvailableWidth();
            cajaCliente.SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.6f));

            var contenidoCell = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(6);

            // Primera línea: RNC cliente (izquierda) y Teléfono (derecha)
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 1f, 1f })).UseAllAvailableWidth();

            var rncCell = new Cell()
                .Add(new Paragraph($"RNC cliente: {factura.RncCliente}")
                    .SetFont(fontBold)
                    .SetFontSize(7))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(0)
                .SetMargin(0);

            var telText = string.IsNullOrWhiteSpace(factura.TelefonoCliente) ? string.Empty : factura.TelefonoCliente;
            var telCell = new Cell()
                .Add(new Paragraph(telText)
                    .SetFont(fontBold)
                    .SetFontSize(7)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(0)
                .SetMargin(0);

            headerTable.AddCell(rncCell);
            headerTable.AddCell(telCell);
            headerTable.SetMarginBottom(1.2f);

            contenidoCell.Add(headerTable);

            // Segunda línea: Nombre del cliente
            contenidoCell.Add(new Paragraph(factura.NombreCliente)
                .SetFont(fontBold)
                .SetFontSize(7)
                .SetMarginBottom(1.2f));

            // Tercera línea: Dirección
            contenidoCell.Add(new Paragraph(factura.DireccionCliente)
                .SetFont(fontRegular)
                .SetFontSize(6)
                .SetMarginBottom(0));

            cajaCliente.AddCell(contenidoCell);
            document.Add(cajaCliente);

            // Línea separadora inferior (para conservar el estilo original)
            document.Add(new Paragraph()
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetMarginTop(0.5f)
                .SetMarginBottom(0.5f));
        }

        private void AgregarTablaConceptos(Document document, FacturaNcf factura, PdfFont fontBold, PdfFont fontRegular)
        {
            var tabla = new Table(UnitValue.CreatePercentArray(new float[] { 1f, 3.5f, 1.2f, 1.2f, 1.0f, 1.2f }));
            tabla.SetWidth(UnitValue.CreatePercentValue(100));

            // Encabezados
            string[] headers = { "Cantidad", "Descripción", "Precio", "Subtotal", "Itbis", "Total RD$" };
            foreach (var header in headers)
            {
                tabla.AddCell(new Cell()
                    .Add(new Paragraph(header)
                        .SetFont(fontBold)
                        .SetFontSize(6.5f)
                        .SetTextAlignment(TextAlignment.CENTER))
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                    .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                    .SetPadding(1.5f));
            }

            // Fila del concepto principal
            tabla.AddCell(new Cell()
                .Add(new Paragraph("1")
                    .SetFont(fontRegular)
                    .SetFontSize(8f)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(1.2f));

            tabla.AddCell(new Cell()
                .Add(new Paragraph(factura.Concepto)
                    .SetFont(fontRegular)
                    .SetFontSize(8f))
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(1.2f));

            // Precio
            tabla.AddCell(new Cell()
                .Add(new Paragraph($"RD$ {(factura.Gravado > 0 ? factura.Gravado : factura.Exento):N2}")
                    .SetFont(fontRegular)
                    .SetFontSize(8f)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(1.2f));

            // Subtotal
            tabla.AddCell(new Cell()
                .Add(new Paragraph($"RD$ {(factura.Gravado > 0 ? factura.Gravado : factura.Exento):N2}")
                    .SetFont(fontRegular)
                    .SetFontSize(8f)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(1.2f));

            // Itbis
            tabla.AddCell(new Cell()
                .Add(new Paragraph($"RD$ {factura.Itbis:N2}")
                    .SetFont(fontRegular)
                    .SetFontSize(8f)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(1.2f));

            // Total
            tabla.AddCell(new Cell()
                .Add(new Paragraph($"RD$ {(factura.Exento + factura.Gravado + factura.Itbis):N2}")
                    .SetFont(fontBold)
                    .SetFontSize(8f)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(1.2f));

            document.Add(tabla);
        }

        private void AgregarPieFactura(Document document, FacturaNcf factura, PdfFont fontBold, PdfFont fontRegular)
        {
            // Tabla inferior: Términos de pago y Totales
            var tablaPie = new Table(UnitValue.CreatePercentArray(new float[] { 1f, 1f }))
                .UseAllAvailableWidth()
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.6f))
                .SetMarginBottom(0f);

            // Columna izquierda: Términos de pago - DIVIDIDO EN DOS COLUMNAS
            var terminosCell = new Cell()
                /*.Add(new Paragraph("Términos de pago")
                    .SetFont(fontBold)
                    .SetFontSize(6.5f)
                    .SetMarginBottom(0.5f))*/
                .SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.4f))
                .SetPadding(1f)
                .SetBorderRight(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.6f))
                .SetBorderTop(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderLeft(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderBottom(iText.Layout.Borders.Border.NO_BORDER);

            // Checkboxes compactos
            var checkCheque = factura.EsCheque ? "☑" : "☐";
            var checkEfectivo = factura.EsEfectivo ? "☑" : "☐";
            var checkCredito = factura.EsCredito ? "☑" : "☐";

            // Crear tabla interna de 2 columnas para los términos de pago
            var tablaTerminosInterna = new Table(UnitValue.CreatePercentArray(new float[] { 1f, 1f }))
                .UseAllAvailableWidth()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetMarginTop(0f);

            // COLUMNA 1 (IZQUIERDA): Cheque
            var columna1 = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(0f)
                .SetPaddingTop(0f)
                .SetVerticalAlignment(VerticalAlignment.TOP);

            columna1.Add(new Paragraph($"{checkCheque} Cheque:")
                .SetFont(fontBold)
                .SetFontSize(7.5f)
                .SetMarginBottom(0.4f));
            columna1.Add(new Paragraph($"No.: {factura.NumeroCheque ?? ""}")
                .SetFont(fontRegular)
                .SetFontSize(5.8f)
                .SetMarginBottom(0.4f)
                .SetMarginLeft(6))
            ;
            columna1.Add(new Paragraph($"Banco: {factura.Banco ?? ""}")
                .SetFont(fontRegular)
                .SetFontSize(5.8f)
                .SetMarginLeft(6)
                .SetMarginBottom(0f));

            tablaTerminosInterna.AddCell(columna1);

            // COLUMNA 2 (DERECHA): Pago en efectivo arriba, Factura a Crédito abajo
            var columna2 = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(0f)
                .SetPaddingTop(0f)
                .SetVerticalAlignment(VerticalAlignment.TOP);

            // Primero: Pago en efectivo
            columna2.Add(new Paragraph($"{checkEfectivo} Pago en efectivo:")
                .SetFont(fontBold)
                .SetFontSize(7.5f)
                .SetMarginBottom(0.4f));
            columna2.Add(new Paragraph($"Efectivo: RD$ {(factura.EsEfectivo ? (factura.Exento + factura.Gravado + factura.Itbis) : 0):N2}")
                .SetFont(fontRegular)
                .SetFontSize(5.8f)
                .SetMarginBottom(0.4f)
                .SetMarginLeft(6));
            columna2.Add(new Paragraph($"Cambio: RD$ 0.00")
                .SetFont(fontRegular)
                .SetFontSize(5.8f)
                .SetMarginBottom(0.4f)
                .SetMarginLeft(6));

            // Segundo: Factura a Crédito (abajo)
            columna2.Add(new Paragraph($"{checkCredito} Factura a Crédito:")
                .SetFont(fontBold)
                .SetFontSize(7.5f)
                .SetMarginBottom(0f));

            tablaTerminosInterna.AddCell(columna2);

            terminosCell.Add(tablaTerminosInterna);

            tablaPie.AddCell(terminosCell);

            // Columna derecha: Totales
            var totalesCell = new Cell()
                .Add(new Paragraph($"Total exento: RD$ {factura.Exento:N2}")
                    .SetFont(fontRegular)
                    .SetFontSize(9f)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(1f))
                .Add(new Paragraph($"Total Gravado: RD$ {factura.Gravado:N2}")
                    .SetFont(fontRegular)
                    .SetFontSize(9f)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(1f))
                .Add(new Paragraph($"Itbis: RD$ {factura.Itbis:N2}")
                    .SetFont(fontRegular)
                    .SetFontSize(9f)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(2f))
                .Add(new Paragraph($"Total Neto: RD$ {(factura.Exento + factura.Gravado + factura.Itbis):N2}")
                    .SetFont(fontBold)
                    .SetFontSize(9.5f)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(4f)
                .SetVerticalAlignment(VerticalAlignment.TOP);

            tablaPie.AddCell(totalesCell);

            document.Add(tablaPie);

            // Espacio adicional antes de las firmas
            

            // Firmas

            var tablaFirmas = new Table(UnitValue.CreatePercentArray(new float[] { 1f, 1f }))
                .SetWidth(UnitValue.CreatePercentValue(100))
                .SetMarginTop(25f);
                

            var celdaRecibidoPor = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER);

            var lineaRecibidoPor = new Paragraph()
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1f))
                .SetWidth(UnitValue.CreatePercentValue(65))
                .SetMarginBottom(1.2f)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);

            celdaRecibidoPor.Add(lineaRecibidoPor);
            celdaRecibidoPor.Add(new Paragraph("Recibido por")
                .SetFont(fontBold)
                .SetFontSize(6.2f)
                .SetTextAlignment(TextAlignment.CENTER));

            tablaFirmas.AddCell(celdaRecibidoPor);

            var celdaRamaFemenina = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER);

            var lineaRamaFemenina = new Paragraph()
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1f))
                .SetWidth(UnitValue.CreatePercentValue(65))
                .SetMarginBottom(1.2f)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);

            celdaRamaFemenina.Add(lineaRamaFemenina);
            celdaRamaFemenina.Add(new Paragraph("Por Rama Femenina Contra El Cancer")
                .SetFont(fontBold)
                .SetFontSize(6.2f)
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
        public decimal Exento { get; set; }
        public decimal Gravado { get; set; }
        public decimal Itbis { get; set; }
        
        // Forma de pago
        public bool EsCheque { get; set; }
        public bool EsEfectivo { get; set; }
        public bool EsCredito { get; set; }
        public string? NumeroCheque { get; set; }
        public string? Banco { get; set; }
    }
}
