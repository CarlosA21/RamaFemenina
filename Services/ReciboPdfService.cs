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
    /// Tamaño: Media Carta (8.5" x 5.5") - Horizontal para Epson LX-350
    /// </summary>
    public class ReciboPdfService
    {
        // Tamaño Media Carta: 5.5" ancho x 8.5" alto (396 x 612 puntos) en vertical
        private static readonly PageSize MEDIA_CARTA = new PageSize(500f, 612f);

        private const float MARGIN_LEFT = 15f;
        private const float MARGIN_RIGHT = 15f;
        private const float MARGIN_TOP = 8f;  // Ajustado para aprovechar más el área
        private const float MARGIN_BOTTOM = 8f;  // Ajustado

        /// <summary>
        /// Genera un PDF del recibo y lo retorna como array de bytes
        /// </summary>
        public async Task<byte[]> GenerarReciboPdfAsync(Recibo recibo, string logoPath = null)
        {
            return await Task.Run(() =>
            {
                using var memoryStream = new MemoryStream();

                // Crear documento PDF en tamaño Media Carta (5.5" x 8.5")
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

            // PRIORIDAD 1: Buscar icono2.png (logo principal para recibos)
            string[] posiblesImagesPng = {
                IOPath.Combine(appDirectory, "Assets", "icono2.png"),
                IOPath.Combine(appDirectory, "icono2.png")
            };

            foreach (var rutaImages in posiblesImagesPng)
            {
                if (File.Exists(rutaImages))
                {
                    System.Diagnostics.Debug.WriteLine($"[ReciboPdfService] ✅ Logo images.png encontrado: {rutaImages}");
                    return rutaImages;
                }
            }

            // PRIORIDAD 2: Buscar otros PNG
            string[] posiblesPng = {
                IOPath.Combine(appDirectory, "Assets", "icono2.png"),
                IOPath.Combine(appDirectory, "Assets", "logo.ico"),
                IOPath.Combine(appDirectory, "icono2.ico"),
                IOPath.Combine(appDirectory, "logo.ico")
            };

            foreach (var rutaPng in posiblesPng)
            {
                if (File.Exists(rutaPng))
                {
                    System.Diagnostics.Debug.WriteLine($"[ReciboPdfService] ✅ Logo PNG encontrado: {rutaPng}");
                    return rutaPng;
                }
            }

            // PRIORIDAD 3: Buscar JPG
            string[] posiblesJpg = {
                IOPath.Combine(appDirectory, "Assets", "images.jpg"),
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

            // PRIORIDAD 4: Buscar ICO y convertir
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
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 1.5f, 2.5f }));
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Logo principal con texto al lado (images.png o el logo disponible)
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
                    logo.ScaleToFit(50f, 50f);
                    celdaLogo.Add(logo);

                    // Celda del texto al lado del logo
                    var celdaTexto = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetPadding(0)
                        .SetMargin(0)
                        .SetPaddingLeft(2f);
                    celdaTexto.Add(new Paragraph("Rama Femenina")
                        .SetFont(fontBold).SetFontSize(9).SetMargin(0).SetMarginBottom(0.5f));
                    celdaTexto.Add(new Paragraph("Contra el Cáncer, Inc.")
                        .SetFont(fontBold).SetFontSize(9).SetMargin(0).SetMarginBottom(1f));
                    // Mover ligeramente hacia la izquierda para centrar bajo la línea anterior
                    celdaTexto.Add(new Paragraph("Desde 1951")
                        .SetFont(fontBold).SetFontSize(8).SetMargin(0).SetMarginLeft(10f));

                    // Agregar ambas celdas a la tabla interna
                    tablaLogoTexto.AddCell(celdaLogo);
                    tablaLogoTexto.AddCell(celdaTexto);

                    // Agregar la tabla interna a la celda principal
                    table.AddCell(new Cell()
                        .Add(tablaLogoTexto)
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetPaddingLeft(0));
                }
                catch
                {
                    // Si falla, agregar celda vacía
                    table.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                }
            }
            else
            {
                table.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            }

            // Datos contacto (derecha)
            table.AddCell(new Cell()
                .Add(new Paragraph("Calle Dr. Flavio D. Espinal, esq. A #1, Reparto Oquet, Santiago, R.D.")
                    .SetFont(fontBold)
                    .SetFontSize(8.5f)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(0.5f))
                .Add(new Paragraph("Tels.: 809-582-3939 / 809-226-1178")
                    .SetFont(fontBold)
                    .SetFontSize(8.5f)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(0.5f))
                .Add(new Paragraph("RNC: 4-30-10692-5")
                    .SetFont(fontBold)
                    .SetFontSize(8.5f)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            document.Add(table);

            // Línea separadora
            document.Add(new Paragraph()
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1.5f))
                .SetMarginTop(3)
                .SetMarginBottom(3));
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
                    .SetFontSize(10))  // Reducido+
                    .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph($"Recibo de Ingreso")
                    .SetFont(fontBold)
                    .SetFontSize(10)  // Reducido
                    .SetMarginTop(2))  // Reducido
                     .SetTextAlignment(TextAlignment.CENTER)

                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            // Número y fecha
            table.AddCell(new Cell()
                .Add(new Paragraph($"No. {recibo.NumeroRecibo}")
                    .SetFont(fontBold)
                    .SetFontSize(11)  // Reducido
                    .SetTextAlignment(TextAlignment.RIGHT))
                .Add(new Paragraph($"Fecha: {recibo.FechaFormateada}")
                    .SetFont(fontRegular)
                    .SetFontSize(8.5f)  // Reducido
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginTop(2))  // Reducido
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            document.Add(table);

            document.Add(new Paragraph()
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1f))
                .SetMarginTop(2)  // Reducido
                .SetMarginBottom(4));  // Reducido
        }

        /// <summary>
        /// Datos del recibo
        /// </summary>
        private void AgregarDatosRecibo(Document document, Recibo recibo, PdfFont fontBold, PdfFont fontRegular)
        {
            // Recibimos de
            document.Add(new Paragraph()
                .Add(new Text("Hemos recibido de: ").SetFont(fontBold).SetFontSize(8.5f))  // Reducido
                .Add(new Text(recibo.RecibimosDe ?? "").SetFont(fontBold).SetFontSize(8.5f))  // Reducido
                .SetMarginBottom(3));  // Reducido

            // Monto
            document.Add(new Paragraph()
                .Add(new Text("La suma de RD$ ").SetFont(fontBold).SetFontSize(8.5f))  // Reducido
                .Add(new Text($"{recibo.Monto:N2}").SetFont(fontBold).SetFontSize(12)) // Reducido
                .SetMarginBottom(2));  // Reducido

            // Monto en letras
            var montoLetras = !string.IsNullOrEmpty(recibo.MontoEnLetras)
                ? recibo.MontoEnLetras
                : ConvertirNumeroALetras(recibo.Monto);
            document.Add(new Paragraph(montoLetras)
                .SetFont(fontBold)
                .SetFontSize(7.5f)  // Reducido
                .SetMarginBottom(3));  // Reducido

            // Concepto
            document.Add(new Paragraph()
                .Add(new Text("Por concepto de: ").SetFont(fontBold).SetFontSize(8.5f))  // Reducido
                .Add(new Text(recibo.Concepto ?? "").SetFont(fontBold).SetFontSize(8.5f))  // Reducido
                .SetMarginBottom(4));  // Reducido

            // SECCIÓN DE MÉTODO DE PAGO CON CUADROS (como en la imagen)
            AgregarSeccionMetodoPago(document, recibo, fontBold, fontRegular);
        }

        /// <summary>
        /// Agrega la sección de método de pago con cuadros/checkboxes como en el formato original
        /// </summary>
        private void AgregarSeccionMetodoPago(Document document, Recibo recibo, PdfFont fontBold, PdfFont fontRegular)
        {
            // Crear tabla compacta con columnas ajustadas al contenido (sin espacios grandes)
            var tablaPago = new Table(UnitValue.CreatePercentArray(new float[] { 0.8f, 0.5f, 0.8f, 0.5f, 1.2f, 2f, 0.8f, 2f }));
            tablaPago.SetWidth(UnitValue.CreatePercentValue(100));
            tablaPago.SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 1f));

            // ====== FILA 1: Efectivo | [X] | Transf. | [ ] | No. fact. NCF: | ______ | (colspan 2 vacío) ======

            // Celda "Efectivo"
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph("Efectivo").SetFont(fontBold).SetFontSize(8))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Checkbox Efectivo
            tablaPago.AddCell(new Cell()
                .Add(CrearCuadroCheckbox((recibo.EsEfectivo ?? false)))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Celda "Transf."
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph("Transf.").SetFont(fontBold).SetFontSize(8))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Checkbox Transferencia
            tablaPago.AddCell(new Cell()
                .Add(CrearCuadroCheckbox((recibo.EsTransferencia ?? false)))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // No. fact. NCF:
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph("No. fact. NCF:").SetFont(fontBold).SetFontSize(8))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Valor NCF con línea inferior
            var ncfValue = !string.IsNullOrEmpty(recibo.NumeroFacturaNCF) ? recibo.NumeroFacturaNCF : "";
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph(ncfValue)
                    .SetFont(fontRegular)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.LEFT))
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetBorderTop(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderLeft(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderRight(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Espacios vacíos (colspan 2) para la primera fila
            tablaPago.AddCell(new Cell(1, 2)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            // ====== FILA 2: Cheque | [ ] | No.: | ______ | Banco: | ______ ======

            // Celda "Cheque"
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph("Cheque").SetFont(fontBold).SetFontSize(8))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Checkbox Cheque
            tablaPago.AddCell(new Cell()
                .Add(CrearCuadroCheckbox((recibo.EsCheque ?? false)))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // No.: (para cheque)
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph("No.:").SetFont(fontBold).SetFontSize(8))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Número de cheque con línea
            var numCheque = (recibo.EsCheque ?? false) ? (recibo.NumeroCheque ?? "") : "";
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph(numCheque)
                    .SetFont(fontRegular)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.LEFT))
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetBorderTop(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderLeft(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderRight(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Banco:
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph("Banco:").SetFont(fontBold).SetFontSize(8))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Nombre del banco con línea
            var banco = !string.IsNullOrEmpty(recibo.Banco) ? recibo.Banco : "";
            tablaPago.AddCell(new Cell()
                .Add(new Paragraph(banco)
                    .SetFont(fontBold)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.LEFT))
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetBorderTop(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderLeft(iText.Layout.Borders.Border.NO_BORDER)
                .SetBorderRight(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            // Espacios vacíos finales (colspan 2) - alineación con fila superior
            tablaPago.AddCell(new Cell(1, 2)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

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
            // Espacio antes del pie (más espacio para bajar la firma)
            document.Add(new Paragraph("\n\n"));  // Tres saltos de línea para más separación

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