using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Layout.Borders;

namespace RamaFemenina.Services
{
    public class FacturaService
    {
        public async Task<byte[]> GenerarFacturaAsync(FacturaParametros parametros)
        {
            return await Task.FromResult(GenerarPdfFactura(parametros));
        }

        private byte[] GenerarPdfFactura(FacturaParametros parametros)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            // Configurar márgenes
            document.SetMargins(30, 30, 30, 30);

            // Crear encabezado principal
            CrearEncabezadoFactura(document, parametros);

            // Crear sección de información del cliente
            CrearSeccionCliente(document, parametros);

            // Crear tabla de productos/servicios
            CrearTablaProductos(document, parametros);

            // Crear sección de términos de pago y totales
            CrearSeccionPagoYTotales(document, parametros);

            // Crear pie de página
            CrearPieFactura(document);

            document.Close();
            return memoryStream.ToArray();
        }

        private void CrearEncabezadoFactura(Document document, FacturaParametros parametros)
        {
            // Tabla principal del encabezado (2 columnas)
            var headerTable = new Table(new float[] { 60, 40 });
            headerTable.SetWidth(UnitValue.CreatePercentValue(100));

            // Columna izquierda - Logo y datos de la empresa
            var leftCell = new Cell();
            leftCell.SetBorder(Border.NO_BORDER);
            leftCell.SetPadding(10);

            // Logo placeholder (símbolo médico)
            var logoText = new Paragraph("?")
                .SetFontSize(48)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(5);
            leftCell.Add(logoText);

            // Nombre de la empresa
            var empresaNombre = new Paragraph("Rama Femenina")
                .SetFontSize(18)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(2);
            leftCell.Add(empresaNombre);

            var empresaSubtitulo = new Paragraph("Contra el Cáncer, Inc.")
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(2);
            leftCell.Add(empresaSubtitulo);

            var empresaDesde = new Paragraph("Desde 1994")
                .SetFontSize(10)
                .SetItalic()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10);
            leftCell.Add(empresaDesde);

            // Dirección y contacto
            var direccion = new Paragraph("Calle Pedro Francisco Bonó No. 33, Santiago, R.D.")
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(2);
            leftCell.Add(direccion);

            var telefono = new Paragraph("Tels.: 809-582-9939 / 809-226-1178 *Fax: 809-582-9939")
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(5);
            leftCell.Add(telefono);

            // RNC
            var rncBox = new Paragraph("RNC: 4-30-10592-5")
                .SetFontSize(10)
                .SetBold()
                .SetTextAlignment(TextAlignment.LEFT)
                .SetBorder(new SolidBorder(1))
                .SetPadding(3);
            leftCell.Add(rncBox);

            var fechaBox = new Paragraph($"Fecha: {parametros.Fecha:dd/MM/yyyy}")
                .SetFontSize(10)
                .SetBold()
                .SetTextAlignment(TextAlignment.LEFT)
                .SetBorder(new SolidBorder(1))
                .SetPadding(3)
                .SetMarginTop(2);
            leftCell.Add(fechaBox);

            headerTable.AddCell(leftCell);

            // Columna derecha - Información de factura
            var rightCell = new Cell();
            rightCell.SetBorder(Border.NO_BORDER);
            rightCell.SetPadding(10);

            // NCF
            var ncfBox = new Paragraph("NCF")
                .SetFontSize(12)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(new SolidBorder(2))
                .SetPadding(5)
                .SetMarginBottom(5);
            rightCell.Add(ncfBox);

            var ncfNumero = new Paragraph(parametros.NCF ?? "")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(new SolidBorder(1))
                .SetPadding(3)
                .SetMarginBottom(10);
            rightCell.Add(ncfNumero);

            var validaHasta = new Paragraph($"Válida hasta: {parametros.ValidaHasta:dd/MM/yyyy}")
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20);
            rightCell.Add(validaHasta);

            headerTable.AddCell(rightCell);

            document.Add(headerTable);

            // Línea separadora
            document.Add(new Paragraph()
                .SetBorderBottom(new SolidBorder(2))
                .SetMarginBottom(10));
        }

        private void CrearSeccionCliente(Document document, FacturaParametros parametros)
        {
            // Tabla de información del cliente
            var clienteTable = new Table(new float[] { 25, 50, 25 });
            clienteTable.SetWidth(UnitValue.CreatePercentValue(100));
            clienteTable.SetMarginBottom(15);

            // RNC cliente
            var rncClienteCell = new Cell();
            rncClienteCell.Add(new Paragraph("RNC cliente:").SetFontSize(9).SetBold());
            rncClienteCell.Add(new Paragraph(parametros.RncCliente ?? "").SetFontSize(9));
            rncClienteCell.SetBorder(new SolidBorder(1));
            rncClienteCell.SetPadding(5);
            clienteTable.AddCell(rncClienteCell);

            // Nombre del cliente (celda vacía en el diseño original)
            var nombreCell = new Cell();
            nombreCell.SetBorder(new SolidBorder(1));
            clienteTable.AddCell(nombreCell);

            // Teléfono
            var telefonoCell = new Cell();
            telefonoCell.Add(new Paragraph("Teléfono:").SetFontSize(9).SetBold());
            telefonoCell.Add(new Paragraph(parametros.TelefonoCliente ?? "").SetFontSize(9));
            telefonoCell.SetBorder(new SolidBorder(1));
            telefonoCell.SetPadding(5);
            clienteTable.AddCell(telefonoCell);

            // Segunda fila
            var clienteCell = new Cell();
            clienteCell.Add(new Paragraph("Cliente:").SetFontSize(9).SetBold());
            clienteCell.Add(new Paragraph(parametros.NombreCliente ?? "").SetFontSize(9));
            clienteCell.SetBorder(new SolidBorder(1));
            clienteCell.SetPadding(5);
            clienteTable.AddCell(clienteCell);

            var emptyCell = new Cell();
            emptyCell.SetBorder(new SolidBorder(1));
            clienteTable.AddCell(emptyCell);

            var empty2Cell = new Cell();
            empty2Cell.SetBorder(new SolidBorder(1));
            clienteTable.AddCell(empty2Cell);

            // Tercera fila - Dirección
            var direccionCell = new Cell(1, 3); // rows=1, cols=3 para colspan
            direccionCell.Add(new Paragraph("Dirección:").SetFontSize(9).SetBold());
            direccionCell.Add(new Paragraph(parametros.DireccionCliente ?? "").SetFontSize(9));
            direccionCell.SetBorder(new SolidBorder(1));
            direccionCell.SetPadding(5);
            clienteTable.AddCell(direccionCell);

            document.Add(clienteTable);
        }

        private void CrearTablaProductos(Document document, FacturaParametros parametros)
        {
            // Tabla de productos
            var productosTable = new Table(new float[] { 15, 35, 15, 15, 10, 15 });
            productosTable.SetWidth(UnitValue.CreatePercentValue(100));
            productosTable.SetMarginBottom(15);

            // Encabezados
            var headers = new string[] { "Cantidad", "Descripción", "Precio", "Subtotal", "Itbis", "Total RD$" };
            foreach (var header in headers)
            {
                var headerCell = new Cell();
                headerCell.Add(new Paragraph(header).SetFontSize(10).SetBold().SetTextAlignment(TextAlignment.CENTER));
                headerCell.SetBackgroundColor(ColorConstants.LIGHT_GRAY);
                headerCell.SetBorder(new SolidBorder(1));
                headerCell.SetPadding(5);
                productosTable.AddCell(headerCell);
            }

            // Productos/Servicios
            if (parametros.Items != null && parametros.Items.Count > 0)
            {
                foreach (var item in parametros.Items)
                {
                    productosTable.AddCell(new Cell().Add(new Paragraph(item.Cantidad.ToString()).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER)).SetBorder(new SolidBorder(1)).SetPadding(3));
                    productosTable.AddCell(new Cell().Add(new Paragraph(item.Descripcion).SetFontSize(9)).SetBorder(new SolidBorder(1)).SetPadding(3));
                    productosTable.AddCell(new Cell().Add(new Paragraph($"${item.Precio:N2}").SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(new SolidBorder(1)).SetPadding(3));
                    productosTable.AddCell(new Cell().Add(new Paragraph($"${item.Subtotal:N2}").SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(new SolidBorder(1)).SetPadding(3));
                    productosTable.AddCell(new Cell().Add(new Paragraph($"${item.Itbis:N2}").SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(new SolidBorder(1)).SetPadding(3));
                    productosTable.AddCell(new Cell().Add(new Paragraph($"${item.Total:N2}").SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(new SolidBorder(1)).SetPadding(3));
                }
            }
            else
            {
                // Filas vacías para llenar
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        var emptyCell = new Cell();
                        emptyCell.SetHeight(25);
                        emptyCell.SetBorder(new SolidBorder(1));
                        productosTable.AddCell(emptyCell);
                    }
                }
            }

            document.Add(productosTable);
        }

        private void CrearSeccionPagoYTotales(Document document, FacturaParametros parametros)
        {
            // Tabla inferior con términos de pago y totales
            var bottomTable = new Table(new float[] { 50, 50 });
            bottomTable.SetWidth(UnitValue.CreatePercentValue(100));

            // Columna izquierda - Términos de pago
            var leftBottomCell = new Cell();
            leftBottomCell.SetBorder(new SolidBorder(1));
            leftBottomCell.SetPadding(10);

            leftBottomCell.Add(new Paragraph("Términos de pago").SetFontSize(10).SetBold().SetMarginBottom(5));
            
            // Checkboxes para formas de pago
            var chequeCheck = parametros.EsCheque ? "?" : "?";
            var efectivoCheck = parametros.EsEfectivo ? "?" : "?";
            var creditoCheck = parametros.EsCredito ? "?" : "?";

            leftBottomCell.Add(new Paragraph($"{chequeCheck} Cheque:").SetFontSize(9));
            leftBottomCell.Add(new Paragraph($"No.: {parametros.NumeroCheque ?? ""}").SetFontSize(9).SetMarginLeft(15));
            leftBottomCell.Add(new Paragraph($"Banco: {parametros.Banco ?? ""}").SetFontSize(9).SetMarginLeft(15));
            
            leftBottomCell.Add(new Paragraph($"{efectivoCheck} Pago en efectivo:").SetFontSize(9).SetMarginTop(5));
            leftBottomCell.Add(new Paragraph($"Efectivo: {parametros.MontoEfectivo:C}").SetFontSize(9).SetMarginLeft(15));
            leftBottomCell.Add(new Paragraph($"Cambio: {parametros.Cambio:C}").SetFontSize(9).SetMarginLeft(15));
            
            leftBottomCell.Add(new Paragraph($"{creditoCheck} Factura a Crédito:").SetFontSize(9).SetMarginTop(5));

            bottomTable.AddCell(leftBottomCell);

            // Columna derecha - Totales
            var rightBottomCell = new Cell();
            rightBottomCell.SetBorder(new SolidBorder(1));
            rightBottomCell.SetPadding(10);

            rightBottomCell.Add(new Paragraph($"Total exento: ${parametros.TotalExento:N2}").SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
            rightBottomCell.Add(new Paragraph($"Total Gravado: ${parametros.TotalGravado:N2}").SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
            rightBottomCell.Add(new Paragraph($"Itbis: ${parametros.TotalItbis:N2}").SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
            rightBottomCell.Add(new Paragraph($"Total Neto: ${parametros.TotalNeto:N2}").SetFontSize(10).SetBold().SetTextAlignment(TextAlignment.RIGHT).SetMarginTop(10));

            bottomTable.AddCell(rightBottomCell);

            document.Add(bottomTable);
        }

        private void CrearPieFactura(Document document)
        {
            // Tabla de pie con firmas
            var footerTable = new Table(new float[] { 50, 50 });
            footerTable.SetWidth(UnitValue.CreatePercentValue(100));
            footerTable.SetMarginTop(20);

            var leftFooterCell = new Cell();
            leftFooterCell.Add(new Paragraph("Por Rama Femenina contra el cáncer").SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));
            leftFooterCell.Add(new Paragraph("\n\n_______________________").SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));
            leftFooterCell.SetBorder(new SolidBorder(1));
            leftFooterCell.SetPadding(15);
            leftFooterCell.SetHeight(60);

            var rightFooterCell = new Cell();
            rightFooterCell.Add(new Paragraph("Recibido por").SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));
            rightFooterCell.Add(new Paragraph("\n\n_______________________").SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));
            rightFooterCell.SetBorder(new SolidBorder(1));
            rightFooterCell.SetPadding(15);
            rightFooterCell.SetHeight(60);

            footerTable.AddCell(leftFooterCell);
            footerTable.AddCell(rightFooterCell);

            document.Add(footerTable);
        }

        public async Task<string> MostrarFacturaAsync(byte[] pdfBytes, string nombreArchivo)
        {
            var tempPath = Path.GetTempPath();
            var filePath = Path.Combine(tempPath, nombreArchivo);

            await File.WriteAllBytesAsync(filePath, pdfBytes);

            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            };

            System.Diagnostics.Process.Start(processStartInfo);
            return filePath;
        }

        public async Task GuardarFacturaAsync(byte[] pdfBytes, string nombreArchivo)
        {
            var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var downloadsFolder = Path.Combine(downloadsPath, "Downloads");
            var filePath = Path.Combine(downloadsFolder, nombreArchivo);

            await File.WriteAllBytesAsync(filePath, pdfBytes);
        }
    }

    #region Clases de Parámetros para Factura

    public class FacturaParametros
    {
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string? NCF { get; set; }
        public DateTime ValidaHasta { get; set; } = DateTime.Now.AddYears(1);
        
        // Cliente
        public string? RncCliente { get; set; }
        public string? NombreCliente { get; set; }
        public string? TelefonoCliente { get; set; }
        public string? DireccionCliente { get; set; }
        
        // Items
        public List<FacturaItem> Items { get; set; } = new List<FacturaItem>();
        
        // Totales
        public decimal TotalExento { get; set; }
        public decimal TotalGravado { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal TotalNeto { get; set; }
        
        // Forma de pago
        public bool EsCheque { get; set; }
        public bool EsEfectivo { get; set; }
        public bool EsCredito { get; set; }
        public string? NumeroCheque { get; set; }
        public string? Banco { get; set; }
        public decimal MontoEfectivo { get; set; }
        public decimal Cambio { get; set; }
    }

    public class FacturaItem
    {
        public int Cantidad { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public decimal Subtotal => Cantidad * Precio;
        public decimal Itbis { get; set; }
        public decimal Total => Subtotal + Itbis;
    }

    #endregion
}