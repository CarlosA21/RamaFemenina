using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using RamaFemenina.Data;
using RamaFemenina.Models;
using Microsoft.EntityFrameworkCore;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Geom;
using iText.Layout.Borders;
using IOPath = System.IO.Path; // Alias para evitar conflicto con iText.Kernel.Geom.Path

namespace RamaFemenina.Services
{
    /// <summary>
    /// Servicio para generar reportes en PDF usando iText7
    /// Reemplaza completamente Crystal Reports
    /// </summary>
    public class PdfReportService
    {
        private readonly RamaFemeninaContext _context;
        private PdfFont _boldFont;
        private PdfFont _regularFont;
        private PdfFont _italicFont;

        public PdfReportService(RamaFemeninaContext context)
        {
            _context = context;
            InitializeFonts();
        }

        private void InitializeFonts()
        {
            _boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            _regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            _italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);
        }

        #region Métodos Principales de Reportes

        /// <summary>
        /// Opción 1: Reporte por área
        /// </summary>
        public async Task<string> GenerarReporteAreaAsync()
        {
            Debug.WriteLine("[PDF] ??? Generando Reporte por Área ???");

            // Consultar directamente las áreas de los pacientes (SIN filtro por año)
            var pacientesPorArea = await _context.Pacientes
                .Where(p => !string.IsNullOrEmpty(p.area))
                .GroupBy(p => p.area)
                .Select(g => new
                {
                    Area = g.Key,
                    Cantidad = g.Count()
                })
                .ToListAsync();

            var total = pacientesPorArea.Sum(p => p.Cantidad);
            var data = pacientesPorArea.Select(p => (object)(new
            {
                p.Area,
                p.Cantidad,
                Porciento = total > 0 ? (decimal)p.Cantidad / total * 100 : 0
            })).ToList();

            return GenerarPdfReporteAreaBasico(data);
        }

        /// <summary>
        /// Opción 2: Reporte de Fallecidas
        /// </summary>
        public async Task<string> GenerarReporteFallecidasAsync()
        {
            Debug.WriteLine("[PDF] ??? Generando Reporte de Fallecidas ???");

            var fallecidas = await _context.Pacientes
                .Where(p => p.observaciones != null && p.observaciones.Contains("falleci"))
                .ToListAsync();

            return GenerarPdfReporteFallecidas(fallecidas);
        }

        /// <summary>
        /// Opción 3: Reporte de Donaciones por Paciente
        /// </summary>
        public async Task<string> GenerarReporteDonacionesPacienteAsync(string idPaciente)
        {
            Debug.WriteLine($"[PDF] ??? Generando Reporte Donaciones - Paciente: {idPaciente} ???");

            var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.cedula == idPaciente);
            if (paciente == null)
                throw new Exception($"Paciente {idPaciente} no encontrado");

            var donaciones = await _context.Donaciones
                .Where(d => d.idPaciente == idPaciente)
                .OrderByDescending(d => d.Fecha)
                .ToListAsync();

            return GenerarPdfReporteDonaciones(paciente, donaciones);
        }

        /// <summary>
        /// Opción 4: Reporte de Pacientes Activas
        /// </summary>
        public async Task<string> GenerarReporteActivasAsync()
        {
            Debug.WriteLine("[PDF] ??? Generando Reporte de Activas ???");

            var activas = await _context.Pacientes
                .Where(p => p.observaciones == null || !p.observaciones.Contains("falleci"))
                .OrderBy(p => p.nombre)
                .ToListAsync();

            return GenerarPdfReporteActivas(activas);
        }

        /// <summary>
        /// Opción 5: Reporte Detallado de Fallecidas
        /// </summary>
        public async Task<string> GenerarReporteFallecidasDetalladoAsync()
        {
            return await GenerarReporteFallecidasAsync();
        }

        /// <summary>
        /// Opción 6: Reporte por Área y Año (EXACTO al Crystal Reports)
        /// </summary>
        public async Task<string> GenerarReporteAreaPorAnioAsync(int anio)
        {
            Debug.WriteLine($"[PDF] ??? Generando Reporte por Área - Año {anio} ???");

            // Query para datos principales (por área)
            var donacionesPorArea = await _context.Donaciones
                .Where(d => d.Fecha.Year == anio)
                .Join(_context.Pacientes, d => d.idPaciente, p => p.cedula, (d, p) => new { d, p })
                .GroupBy(x => x.p.area)
                .Select(g => new
                {
                    Area = g.Key ?? "Sin área",
                    Cantidad = g.Count()
                })
                .ToListAsync();

            var totalCasos = donacionesPorArea.Sum(x => x.Cantidad);
            var datosArea = donacionesPorArea.Select(x => (object)(new
            {
                x.Area,
                x.Cantidad,
                Porciento = totalCasos > 0 ? (decimal)x.Cantidad / totalCasos * 100 : 0
            })).ToList();

            // Query para datos de género
            var datosPorGenero = await _context.Pacientes
                .Join(_context.Donaciones.Where(d => d.Fecha.Year == anio),
                    p => p.cedula,
                    d => d.idPaciente,
                    (p, d) => p)
                .GroupBy(p => p.sexo)
                .Select(g => (object)(new
                {
                    Sexo = g.Key ?? "No especificado",
                    Cantidad = g.Count()
                }))
                .ToListAsync();

            return GenerarPdfReporteAreaPorAnio(datosArea, datosPorGenero, anio);
        }

        /// <summary>
        /// Opción 7: Recibo de Ingresos
        /// </summary>
        public async Task<string> GenerarReciboIngresosAsync(ReciboParametros parametros)
        {
            Debug.WriteLine($"[PDF] ??? Generando Recibo Ingresos #{parametros.NumeroRecibo} ???");
            return await Task.Run(() => GenerarPdfReciboIngresos(parametros));
        }

        /// <summary>
        /// Opción 8: Recibo de Ingreso Completo
        /// </summary>
        public async Task<string> GenerarReciboIngresoCompletoAsync(ReciboCompletoParametros parametros)
        {
            Debug.WriteLine($"[PDF] ??? Generando Recibo Completo #{parametros.NumeroRecibo} ???");
            return await Task.Run(() => GenerarPdfReciboCompleto(parametros));
        }

        /// <summary>
        /// Opción 9: Recibo de Desembolso
        /// </summary>
        public async Task<string> GenerarReciboDesembolsoAsync(DesembolsoParametros parametros)
        {
            Debug.WriteLine($"[PDF] ??? Generando Recibo Desembolso #{parametros.NumeroRecibo} ???");
            return await Task.Run(() => GenerarPdfReciboDesembolso(parametros));
        }

        #endregion

        #region Generadores PDF Específicos

        private string GenerarPdfReporteAreaBasico(List<object> datos)
        {
            var tempPath = IOPath.GetTempPath();
            var pdfFile = IOPath.Combine(tempPath, $"ReporteArea_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            try
            {
                using (var writer = new PdfWriter(pdfFile))
                using (var pdf = new PdfDocument(writer))
                using (var document = new Document(pdf))
                {
                    // Título exacto como en la imagen
                    document.Add(new Paragraph("Reporte por Área")
                        .SetFont(_boldFont)
                        .SetFontSize(16)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(15));

                    // Fecha alineada a la derecha como en la imagen
                    document.Add(new Paragraph($"Fecha: {DateTime.Now:dd/MM/yyyy}")
                        .SetFont(_regularFont)
                        .SetFontSize(10)
                        .SetTextAlignment(TextAlignment.RIGHT)
                        .SetMarginBottom(25));

                    // Tabla con el mismo diseño de la imagen
                    var table = new Table(UnitValue.CreatePercentArray(new float[] { 40, 30, 30 }))
                        .UseAllAvailableWidth();

                    // Encabezados exactos como en la imagen (fondo gris claro)
                    table.AddHeaderCell(CrearCeldaHeaderSimple("Área"));
                    table.AddHeaderCell(CrearCeldaHeaderSimple("Cantidad"));
                    table.AddHeaderCell(CrearCeldaHeaderSimple("Porcentaje"));

                    // Datos con el mismo formato
                    foreach (dynamic item in datos)
                    {
                        table.AddCell(CrearCeldaDatosSimple(item.Area?.ToString() ?? ""));
                        table.AddCell(CrearCeldaDatosSimpleCentrada(item.Cantidad.ToString()));
                        table.AddCell(CrearCeldaDatosSimpleCentrada($"{item.Porciento:F2}%"));
                    }

                    document.Add(table);
                    
                    // Asegurar que todo el contenido se escriba antes del cierre
                    document.Flush();
                }
                
                // IMPORTANTE: Process.Start debe ejecutarse DESPUÉS del using para evitar el error
                // Verificar que el archivo existe antes de abrirlo
                if (File.Exists(pdfFile))
                {
                    Process.Start(new ProcessStartInfo { FileName = pdfFile, UseShellExecute = true });
                    Debug.WriteLine($"[PDF] ? Reporte generado y abierto: {pdfFile}");
                }
                else
                {
                    Debug.WriteLine($"[PDF] ? Error: El archivo PDF no fue creado: {pdfFile}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PDF] ? Error generando reporte: {ex.Message}");
                throw new Exception($"Error al generar el reporte PDF: {ex.Message}", ex);
            }

            return pdfFile;
        }

        // Métodos auxiliares para celdas simples (como en la imagen)
        private Cell CrearCeldaHeaderSimple(string texto)
        {
            return new Cell()
                .Add(new Paragraph(texto).SetFont(_boldFont).SetFontSize(11))
                .SetBackgroundColor(new DeviceRgb(220, 220, 220))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(8)
                .SetBorder(new SolidBorder(ColorConstants.BLACK, 1));
        }

        private Cell CrearCeldaDatosSimple(string texto)
        {
            return new Cell()
                .Add(new Paragraph(texto ?? "").SetFont(_regularFont).SetFontSize(10))
                .SetPadding(8)
                .SetBorder(new SolidBorder(ColorConstants.BLACK, 1));
        }

        private Cell CrearCeldaDatosSimpleCentrada(string texto)
        {
            return new Cell()
                .Add(new Paragraph(texto ?? "").SetFont(_regularFont).SetFontSize(10))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(8)
                .SetBorder(new SolidBorder(ColorConstants.BLACK, 1));
        }

        private string GenerarPdfReporteFallecidas(List<Paciente> fallecidas)
        {
            var tempPath = IOPath.GetTempPath();
            var pdfFile = IOPath.Combine(tempPath, $"ReporteFallecidas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            using (var writer = new PdfWriter(pdfFile))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                document.Add(new Paragraph("Reporte de Pacientes Fallecidas")
                    .SetFont(_boldFont)
                    .SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                var table = new Table(5).UseAllAvailableWidth();
                table.AddHeaderCell(CrearCeldaHeader("Cédula"));
                table.AddHeaderCell(CrearCeldaHeader("Nombre"));
                table.AddHeaderCell(CrearCeldaHeader("Área"));
                table.AddHeaderCell(CrearCeldaHeader("Teléfono"));
                table.AddHeaderCell(CrearCeldaHeader("Observaciones"));

                foreach (var p in fallecidas)
                {
                    table.AddCell(CrearCeldaDatos(p.cedula));
                    table.AddCell(CrearCeldaDatos(p.nombre));
                    table.AddCell(CrearCeldaDatos(p.area ?? ""));
                    table.AddCell(CrearCeldaDatos(p.telefono ?? ""));
                    table.AddCell(CrearCeldaDatos(p.observaciones ?? ""));
                }

                document.Add(table);
            }

            Process.Start(new ProcessStartInfo { FileName = pdfFile, UseShellExecute = true });
            Debug.WriteLine($"[PDF] ? Reporte generado: {pdfFile}");
            return pdfFile;
        }

        private string GenerarPdfReporteDonaciones(Paciente paciente, List<Donaciones> donaciones)
        {
            var tempPath = IOPath.GetTempPath();
            var pdfFile = IOPath.Combine(tempPath, $"ReporteDonaciones_{paciente.cedula}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            using (var writer = new PdfWriter(pdfFile))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                document.Add(new Paragraph($"Reporte de Donaciones - {paciente.nombre}")
                    .SetFont(_boldFont)
                    .SetFontSize(16)
                    .SetMarginBottom(10));

                document.Add(new Paragraph($"Cédula: {paciente.cedula}")
                    .SetFontSize(10)
                    .SetMarginBottom(20));

                var table = new Table(5).UseAllAvailableWidth();
                table.AddHeaderCell(CrearCeldaHeader("Fecha"));
                table.AddHeaderCell(CrearCeldaHeader("Procedimiento"));
                table.AddHeaderCell(CrearCeldaHeader("Monto Solicitado"));
                table.AddHeaderCell(CrearCeldaHeader("Total Donado"));
                table.AddHeaderCell(CrearCeldaHeader("Estado"));

                foreach (var d in donaciones)
                {
                    table.AddCell(CrearCeldaDatos(d.Fecha.ToString("dd/MM/yyyy")));
                    table.AddCell(CrearCeldaDatos(d.procedimiento));
                    table.AddCell(CrearCeldaDatosCentrada($"${d.montoSolicitado:N2}"));
                    table.AddCell(CrearCeldaDatosCentrada($"${d.total:N2}"));
                    table.AddCell(CrearCeldaDatos(d.EstadoTexto));
                }

                document.Add(table);
            }

            Process.Start(new ProcessStartInfo { FileName = pdfFile, UseShellExecute = true });
            Debug.WriteLine($"[PDF] ? Reporte generado: {pdfFile}");
            return pdfFile;
        }

        private string GenerarPdfReporteActivas(List<Paciente> activas)
        {
            var tempPath = IOPath.GetTempPath();
            var pdfFile = IOPath.Combine(tempPath, $"ReporteActivas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            using (var writer = new PdfWriter(pdfFile))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                document.Add(new Paragraph("Reporte de Pacientes Activas")
                    .SetFont(_boldFont)
                    .SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                document.Add(new Paragraph($"Total: {activas.Count} pacientes")
                    .SetFontSize(12)
                    .SetMarginBottom(10));

                var table = new Table(5).UseAllAvailableWidth();
                table.AddHeaderCell(CrearCeldaHeader("Cédula"));
                table.AddHeaderCell(CrearCeldaHeader("Nombre"));
                table.AddHeaderCell(CrearCeldaHeader("Área"));
                table.AddHeaderCell(CrearCeldaHeader("Teléfono"));
                table.AddHeaderCell(CrearCeldaHeader("Celular"));

                foreach (var p in activas)
                {
                    table.AddCell(CrearCeldaDatos(p.cedula));
                    table.AddCell(CrearCeldaDatos(p.nombre));
                    table.AddCell(CrearCeldaDatos(p.area ?? ""));
                    table.AddCell(CrearCeldaDatos(p.telefono ?? ""));
                    table.AddCell(CrearCeldaDatos(p.celular ?? ""));
                }

                document.Add(table);
            }

            Process.Start(new ProcessStartInfo { FileName = pdfFile, UseShellExecute = true });
            Debug.WriteLine($"[PDF] ? Reporte generado: {pdfFile}");
            return pdfFile;
        }

        private string GenerarPdfReporteAreaPorAnio(List<object> datosArea, List<object> datosPorGenero, int anio)
        {
            var tempPath = IOPath.GetTempPath();
            var pdfFile = IOPath.Combine(tempPath, $"ReporteArea_{anio}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            using (var writer = new PdfWriter(pdfFile))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf, PageSize.LETTER))
            {
                document.SetMargins(50, 50, 50, 50);

                AgregarEncabezadoPrincipal(document, anio);
                document.Add(new Paragraph("\n").SetFontSize(10));
                AgregarSeccionGrafica(document);
                document.Add(new Paragraph("\n").SetFontSize(5));
                AgregarTablaAreaAfectada(document, datosArea);
                document.Add(new Paragraph("\n").SetFontSize(10));
                AgregarSeccionGenero(document, datosPorGenero);
                AgregarPieDePagina(document, pdf.GetNumberOfPages());
            }

            Process.Start(new ProcessStartInfo { FileName = pdfFile, UseShellExecute = true });
            Debug.WriteLine($"[PDF] ? Reporte generado: {pdfFile}");
            return pdfFile;
        }

        private string GenerarPdfReciboIngresos(ReciboParametros parametros)
        {
            var tempPath = IOPath.GetTempPath();
            var pdfFile = IOPath.Combine(tempPath, $"ReciboIngresos_{parametros.NumeroRecibo:000000}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            using (var writer = new PdfWriter(pdfFile))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf, PageSize.LETTER))
            {
                document.SetMargins(40, 40, 40, 40);

                document.Add(new Paragraph("RECIBO DE INGRESOS")
                    .SetFont(_boldFont)
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                document.Add(new Paragraph($"No. {parametros.NumeroRecibo:000000}")
                    .SetFont(_boldFont)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.RIGHT));

                document.Add(new Paragraph($"Fecha: {parametros.Fecha:dd/MM/yyyy}")
                    .SetFontSize(11)
                    .SetMarginBottom(20));

                var table = new Table(2).UseAllAvailableWidth();
                
                AgregarFilaRecibo(table, "Recibimos de:", parametros.Nombre);
                AgregarFilaRecibo(table, "Cédula:", parametros.Cedula);
                AgregarFilaRecibo(table, "La suma de:", $"${parametros.Monto:N2}");
                AgregarFilaRecibo(table, "En letras:", parametros.MontoEnLetras);
                AgregarFilaRecibo(table, "Concepto:", parametros.Concepto);
                AgregarFilaRecibo(table, "Cheque No.:", parametros.NumeroCheque);

                document.Add(table);

                document.Add(new Paragraph("\n\n\n").SetMarginTop(40));
                document.Add(new Paragraph("_________________________").SetTextAlignment(TextAlignment.CENTER));
                document.Add(new Paragraph("Firma Autorizada").SetTextAlignment(TextAlignment.CENTER).SetFontSize(10));
            }

            Process.Start(new ProcessStartInfo { FileName = pdfFile, UseShellExecute = true });
            return pdfFile;
        }

        private string GenerarPdfReciboCompleto(ReciboCompletoParametros parametros)
        {
            var tempPath = IOPath.GetTempPath();
            var pdfFile = IOPath.Combine(tempPath, $"ReciboCompleto_{parametros.NumeroRecibo:000000}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            using (var writer = new PdfWriter(pdfFile))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf, PageSize.LETTER))
            {
                document.SetMargins(40, 40, 40, 40);

                document.Add(new Paragraph("RECIBO DE INGRESO")
                    .SetFont(_boldFont)
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                document.Add(new Paragraph($"No. {parametros.NumeroRecibo:000000}")
                    .SetFont(_boldFont)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.RIGHT));

                var table = new Table(2).UseAllAvailableWidth();
                
                AgregarFilaRecibo(table, "Fecha:", parametros.Fecha.ToString("dd/MM/yyyy"));
                AgregarFilaRecibo(table, "Recibimos de:", parametros.Nombre);
                AgregarFilaRecibo(table, "La suma de:", $"${parametros.Monto:N2}");
                AgregarFilaRecibo(table, "En letras:", parametros.MontoEnLetras);
                AgregarFilaRecibo(table, "Concepto:", parametros.Concepto);
                
                var formaPago = new List<string>();
                if (parametros.Efectivo) formaPago.Add("? Efectivo");
                if (parametros.Cheque) formaPago.Add("? Cheque");
                if (parametros.Transferencia) formaPago.Add("? Transferencia");
                
                AgregarFilaRecibo(table, "Forma de Pago:", string.Join("  ", formaPago));
                AgregarFilaRecibo(table, "Banco:", parametros.Banco);
                AgregarFilaRecibo(table, "Cheque/Ref. No.:", parametros.NumeroCheque);
                AgregarFilaRecibo(table, "NCF:", parametros.NCF);

                document.Add(table);
            }

            Process.Start(new ProcessStartInfo { FileName = pdfFile, UseShellExecute = true });
            return pdfFile;
        }

        private string GenerarPdfReciboDesembolso(DesembolsoParametros parametros)
        {
            var tempPath = IOPath.GetTempPath();
            var pdfFile = IOPath.Combine(tempPath, $"ReciboDesembolso_{parametros.NumeroRecibo:000000}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            using (var writer = new PdfWriter(pdfFile))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf, PageSize.LETTER))
            {
                document.SetMargins(40, 40, 40, 40);

                document.Add(new Paragraph("RECIBO DE DESEMBOLSO - CAJA CHICA")
                    .SetFont(_boldFont)
                    .SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                document.Add(new Paragraph($"No. {parametros.NumeroRecibo:000000}")
                    .SetFont(_boldFont)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.RIGHT));

                var table = new Table(2).UseAllAvailableWidth();
                
                AgregarFilaRecibo(table, "Fecha:", parametros.Fecha.ToString("dd/MM/yyyy"));
                AgregarFilaRecibo(table, "Pagado a:", parametros.Nombre);
                AgregarFilaRecibo(table, "La suma de:", $"${parametros.Monto:N2}");
                AgregarFilaRecibo(table, "En letras:", parametros.MontoEnLetras);
                AgregarFilaRecibo(table, "Con cargo a:", parametros.CargoA);
                AgregarFilaRecibo(table, "Concepto:", parametros.Concepto);

                document.Add(table);
            }

            Process.Start(new ProcessStartInfo { FileName = pdfFile, UseShellExecute = true });
            return pdfFile;
        }

        #endregion

        #region Métodos Auxiliares para Reporte de Área por Año

        private void AgregarEncabezadoPrincipal(Document document, int anio)
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 15, 70, 15 }))
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER);

            var logoCell = new Cell()
                .Add(new Paragraph("???").SetFontSize(40).SetTextAlignment(TextAlignment.CENTER))
                .SetBorder(Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            var titleCell = new Cell()
                .Add(new Paragraph("Rama femenina contra el cancer®")
                    .SetFont(_boldFont)
                    .SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetBorder(Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            var dateCell = new Cell()
                .Add(new Paragraph($"Fecha de hoy:\n{DateTime.Now:dd/MM/yyyy}")
                    .SetFont(_regularFont)
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            headerTable.AddCell(logoCell);
            headerTable.AddCell(titleCell);
            headerTable.AddCell(dateCell);

            document.Add(headerTable);

            document.Add(new Paragraph($"Casos atendidos por tipo de afección en:    Año: {anio}")
                .SetFont(_italicFont)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(10));
        }

        private void AgregarSeccionGrafica(Document document)
        {
            var graficaBorder = new SolidBorder(ColorConstants.BLACK, 1);
            var graficaDiv = new Div()
                .SetBorder(graficaBorder)
                .SetHeight(150)
                .SetWidth(UnitValue.CreatePercentValue(100))
                .SetBackgroundColor(ColorConstants.WHITE);

            graficaDiv.Add(new Paragraph("grafica")
                .SetFont(_regularFont)
                .SetFontSize(12)
                .SetMarginTop(10)
                .SetMarginLeft(10));

            document.Add(graficaDiv);
        }

        private void AgregarTablaAreaAfectada(Document document, List<object> datosArea)
        {
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 50, 25, 25 }))
                .UseAllAvailableWidth()
                .SetMarginTop(10);

            table.AddHeaderCell(CrearCeldaHeader("Área afectada"));
            table.AddHeaderCell(CrearCeldaHeader("Cantidad"));
            table.AddHeaderCell(CrearCeldaHeader("Porciento"));

            foreach (dynamic item in datosArea)
            {
                table.AddCell(CrearCeldaDatos(item.Area.ToString()));
                table.AddCell(CrearCeldaDatosCentrada(item.Cantidad.ToString()));
                table.AddCell(CrearCeldaDatosCentrada($"{item.Porciento:F2}%"));
            }

            document.Add(table);
        }

        private void AgregarSeccionGenero(Document document, List<object> datosPorGenero)
        {
            var generoDiv = new Div()
                .SetBorder(new SolidBorder(ColorConstants.LIGHT_GRAY, 1))
                .SetPadding(10)
                .SetBackgroundColor(new DeviceRgb(245, 245, 245));

            generoDiv.Add(new Paragraph("genero")
                .SetFont(_regularFont)
                .SetFontSize(12));

            foreach (dynamic item in datosPorGenero)
            {
                generoDiv.Add(new Paragraph($"{item.Sexo}: {item.Cantidad}")
                    .SetFont(_regularFont)
                    .SetFontSize(10)
                    .SetMarginLeft(20));
            }

            document.Add(generoDiv);
        }

        private void AgregarPieDePagina(Document document, int totalPaginas)
        {
            document.Add(new Paragraph($"1 de página {totalPaginas}")
                .SetFont(_regularFont)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetMarginTop(20));
        }

        #endregion

        #region Métodos Auxiliares Generales

        private Cell CrearCeldaHeader(string texto)
        {
            return new Cell()
                .Add(new Paragraph(texto).SetFont(_boldFont).SetFontSize(10))
                .SetBackgroundColor(new DeviceRgb(200, 200, 200))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(5)
                .SetBorder(new SolidBorder(ColorConstants.BLACK, 1));
        }

        private Cell CrearCeldaDatos(string texto)
        {
            return new Cell()
                .Add(new Paragraph(texto ?? "").SetFont(_regularFont).SetFontSize(9))
                .SetPadding(5)
                .SetBorder(new SolidBorder(ColorConstants.BLACK, 0.5f));
        }

        private Cell CrearCeldaDatosCentrada(string texto)
        {
            return new Cell()
                .Add(new Paragraph(texto ?? "").SetFont(_regularFont).SetFontSize(9))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(5)
                .SetBorder(new SolidBorder(ColorConstants.BLACK, 0.5f));
        }

        private void AgregarFilaRecibo(Table table, string label, string valor)
        {
            table.AddCell(new Cell()
                .Add(new Paragraph(label).SetFont(_boldFont).SetFontSize(11))
                .SetBorder(Border.NO_BORDER)
                .SetPaddingBottom(10));
            
            table.AddCell(new Cell()
                .Add(new Paragraph(valor ?? "").SetFont(_regularFont).SetFontSize(11))
                .SetBorder(Border.NO_BORDER)
                .SetPaddingBottom(10));
        }

        #endregion
    }
}
