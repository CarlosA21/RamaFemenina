using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using RamaFemenina.Models;
using RamaFemenina.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace RamaFemenina.Services
{
    public class SimpleReportService
    {
        private readonly RamaFemeninaContext _context;
        
        // Caché de configuraciones para mejorar rendimiento
        private static readonly ConcurrentDictionary<string, byte[]> _pdfCache = new();
        private static PdfFont? _cachedFont;
        private static readonly object _fontLock = new object();

        public SimpleReportService(RamaFemeninaContext context)
        {
            _context = context;
        }

        #region Métodos Principales de Reportes (Optimizados)

        // Opción 1: Reporte de Área (Optimizado)
        public async Task<byte[]> GenerarReporteAreaAsync()
        {
            var pacientesData = await _context.Pacientes
                .AsNoTracking()
                .Select(p => new { p.area })
                .ToListAsync();
            
            return await Task.Run(() => GenerarPdfReporteArea(pacientesData));
        }

        // Opción 2: Reporte de Fallecidas (Optimizado)
        public async Task<byte[]> GenerarReporteFalleccidasAsync()
        {
            var pacientesFallecidas = await _context.Pacientes
                .AsNoTracking()
                .Where(p => p.observaciones != null && p.observaciones.ToLower().Contains("fallecida"))
                .Select(p => new 
                { 
                    p.cedula, 
                    p.nombre, 
                    p.telefono, 
                    p.celular, 
                    p.area, 
                    p.observaciones 
                })
                .ToListAsync();
            
            return await Task.Run(() => GenerarPdfReporteFallecidas(pacientesFallecidas));
        }

        // Opción 3: Reporte Donaciones por Paciente (Optimizado)
        public async Task<byte[]> GenerarReporteDonacionesPacienteAsync(string idPaciente)
        {
            var pacienteTask = _context.Pacientes
                .AsNoTracking()
                .Where(p => p.cedula == idPaciente)
                .Select(p => new { p.cedula, p.nombre, p.telefono, p.celular, p.area })
                .FirstOrDefaultAsync();

            var donacionesTask = _context.Donaciones
                .AsNoTracking()
                .Where(d => d.idPaciente == idPaciente)
                .OrderBy(d => d.Fecha)
                .Select(d => new
                {
                    d.Fecha,
                    d.procedimiento,
                    d.montoSolicitado,
                    d.total,
                    d.observacion
                })
                .ToListAsync();

            await Task.WhenAll(pacienteTask, donacionesTask);
            
            return await Task.Run(() => GenerarPdfReporteDonacionesPaciente(
                donacionesTask.Result, 
                pacienteTask.Result));
        }

        // Opción 4: Reporte Activas (Optimizado)
        public async Task<byte[]> GenerarReporteActivasAsync()
        {
            var pacientesActivas = await _context.Pacientes
                .AsNoTracking()
                .Where(p => p.observaciones == null || !p.observaciones.ToLower().Contains("fallecida"))
                .OrderBy(p => p.nombre)
                .Select(p => new
                {
                    p.cedula,
                    p.nombre,
                    p.telefono,
                    p.celular,
                    p.area,
                    p.sexo,
                    p.nrecord
                })
                .ToListAsync();
            
            return await Task.Run(() => GenerarPdfReporteActivas(pacientesActivas));
        }

        // Opción 5: Reporte Fallecidas Detallado (Optimizado)
        public async Task<byte[]> GenerarReporteFallecidasDetalladoAsync()
        {
            return await GenerarReporteFalleccidasAsync();
        }

        /// <summary>
        /// Opción 6: Reporte de área por Año (Optimizado)
        /// Replica EXACTAMENTE la estructura del reporte Crystal Reports
        /// </summary>
        public async Task<byte[]> GenerarReporteAreaPorAnioAsync(int anio)
        {
            var donacionesPorArea = await _context.Donaciones
                .AsNoTracking()
                .Where(d => d.Fecha.Year == anio)
                .GroupBy(d => d.procedimiento)
                .Select(g => new
                {
                    Area = g.Key ?? "Sin especificar",
                    Cantidad = g.Count(),
                    TotalSolicitado = g.Sum(d => d.montoSolicitado),
                    TotalRecibido = g.Sum(d => d.total)
                })
                .OrderByDescending(x => x.Cantidad)
                .ToListAsync();

            var datosPorGenero = await _context.Pacientes
                .AsNoTracking()
                .Where(p => p.observaciones == null || !p.observaciones.ToLower().Contains("fallecida"))
                .GroupBy(p => p.sexo ?? "No especificado")
                .Select(g => new
                {
                    Genero = g.Key,
                    Cantidad = g.Count()
                })
                .ToListAsync();
            
            return await Task.Run(() => GenerarPdfReporteAreaPorAnioEstiloCrystal(
                donacionesPorArea, 
                datosPorGenero, 
                anio));
        }

        // Opción 7: Recibo de Ingresos
        public Task<byte[]> GenerarReciboIngresosAsync(ReciboParametros parametros)
        {
            return Task.Run(() => GenerarPdfReciboIngresos(parametros));
        }

        // Opción 8: Recibo de Ingreso Completo
        public Task<byte[]> GenerarReciboIngresoCompletoAsync(ReciboCompletoParametros parametros)
        {
            return Task.Run(() => GenerarPdfReciboIngresoCompleto(parametros));
        }

        // Opción 9: Recibo de Desembolso
        public Task<byte[]> GenerarReciboDesembolsoAsync(DesembolsoParametros parametros)
        {
            return Task.Run(() => GenerarPdfReciboDesembolso(parametros));
        }

        #endregion

        #region Métodos de Generación PDF

        private PdfFont GetCachedFont()
        {
            if (_cachedFont == null)
            {
                lock (_fontLock)
                {
                    _cachedFont ??= PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                }
            }
            return _cachedFont;
        }

        private byte[] GenerarPdfReporteArea(IEnumerable<dynamic> pacientesData)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            writer.SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);
            
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            AgregarEncabezadoReporte(document, "REPORTE POR ÁREA");

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 60, 20, 20 }));
            table.SetWidth(UnitValue.CreatePercentValue(100));
            table.UseAllAvailableWidth();

            AgregarHeaderCelda(table, "Área");
            AgregarHeaderCelda(table, "Cantidad");
            AgregarHeaderCelda(table, "Porcentaje");

            var lista = pacientesData.ToList();
            var agrupados = new Dictionary<string, int>();
            foreach (var item in lista)
            {
                string area = item.area ?? "Sin Área";
                if (!agrupados.ContainsKey(area))
                    agrupados[area] = 0;
                agrupados[area]++;
            }

            int totalPacientes = lista.Count;

            foreach (var grupo in agrupados.OrderBy(x => x.Key))
            {
                decimal porcentaje = totalPacientes > 0 ? (decimal)grupo.Value / totalPacientes * 100 : 0;
                
                table.AddCell(CreateCell(grupo.Key));
                table.AddCell(CreateCell(grupo.Value.ToString()));
                table.AddCell(CreateCell($"{porcentaje:F1}%"));
            }

            table.AddCell(CreateBoldCell("TOTAL"));
            table.AddCell(CreateBoldCell(totalPacientes.ToString()));
            table.AddCell(CreateBoldCell("100.0%"));

            document.Add(table);
            document.Close();
            return memoryStream.ToArray();
        }

        private byte[] GenerarPdfReporteFallecidas(IEnumerable<dynamic> pacientesFallecidas)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            writer.SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);
            
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            AgregarEncabezadoReporte(document, "REPORTE DE PACIENTES FALLECIDAS");

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 15, 30, 15, 15, 25 }));
            table.SetWidth(UnitValue.CreatePercentValue(100));
            table.UseAllAvailableWidth();

            string[] headers = { "Cédula", "Nombre", "Teléfono", "Área", "Observaciones" };
            foreach (var header in headers)
            {
                AgregarHeaderCelda(table, header);
            }

            int count = 0;
            foreach (var paciente in pacientesFallecidas)
            {
                count++;
                table.AddCell(CreateCell(paciente.cedula ?? ""));
                table.AddCell(CreateCell(paciente.nombre ?? ""));
                table.AddCell(CreateCell(paciente.telefono ?? paciente.celular ?? "N/A"));
                table.AddCell(CreateCell(paciente.area ?? "Sin Área"));
                table.AddCell(CreateCell(paciente.observaciones ?? ""));
            }

            document.Add(table);

            var total = new Paragraph($"\nTotal de pacientes fallecidas: {count}")
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(20);
            document.Add(total);

            document.Close();
            return memoryStream.ToArray();
        }

        private byte[] GenerarPdfReporteDonacionesPaciente(IEnumerable<dynamic> donaciones, dynamic paciente)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            writer.SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);
            
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            document.Add(new Paragraph("REPORTE DE DONACIONES")
                .SetFontSize(18)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            if (paciente != null)
            {
                document.Add(new Paragraph($"Paciente: {paciente.nombre}")
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                document.Add(new Paragraph($"Cédula: {paciente.cedula}"));
                document.Add(new Paragraph($"Teléfono: {paciente.telefono ?? paciente.celular ?? "No disponible"}"));
                document.Add(new Paragraph($"Área: {paciente.area ?? "Sin Área"}"));
                document.Add(new Paragraph("\n"));
            }

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 12, 25, 13, 13, 12, 25 }));
            table.SetWidth(UnitValue.CreatePercentValue(100));

            string[] headers = { "Fecha", "Procedimiento", "Solicitado", "Recibido", "Estado", "Observación" };
            foreach (var header in headers)
            {
                AgregarHeaderCelda(table, header);
            }

            decimal totalSolicitado = 0;
            decimal totalRecibido = 0;

            foreach (var donacion in donaciones)
            {
                totalSolicitado += donacion.montoSolicitado;
                totalRecibido += donacion.total;

                var estado = donacion.total >= donacion.montoSolicitado ? "Completado" :
                            donacion.total > 0 ? "Parcial" : "Pendiente";

                table.AddCell(CreateCell(donacion.Fecha.ToString("dd/MM/yyyy")));
                table.AddCell(CreateCell(donacion.procedimiento ?? ""));
                table.AddCell(CreateCell($"${donacion.montoSolicitado:N2}"));
                table.AddCell(CreateCell($"${donacion.total:N2}"));
                table.AddCell(CreateCell(estado));
                table.AddCell(CreateCell(donacion.observacion ?? ""));
            }

            document.Add(table);

            document.Add(new Paragraph($"\nTOTAL SOLICITADO: ${totalSolicitado:N2}").SetBold());
            document.Add(new Paragraph($"TOTAL RECIBIDO: ${totalRecibido:N2}").SetBold());
            document.Add(new Paragraph($"DIFERENCIA: ${(totalSolicitado - totalRecibido):N2}").SetBold());

            document.Close();
            return memoryStream.ToArray();
        }

        private byte[] GenerarPdfReporteActivas(IEnumerable<dynamic> pacientesActivas)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            writer.SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);
            
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            AgregarEncabezadoReporte(document, "REPORTE DE PACIENTES ACTIVAS");

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 13, 25, 12, 12, 12, 8, 18 }));
            table.SetWidth(UnitValue.CreatePercentValue(100));

            string[] headers = { "Cédula", "Nombre", "Teléfono", "Celular", "Área", "Sexo", "Record" };
            foreach (var header in headers)
            {
                AgregarHeaderCelda(table, header);
            }

            int count = 0;
            foreach (var paciente in pacientesActivas)
            {
                count++;
                table.AddCell(CreateCell(paciente.cedula ?? ""));
                table.AddCell(CreateCell(paciente.nombre ?? ""));
                table.AddCell(CreateCell(paciente.telefono ?? "N/A"));
                table.AddCell(CreateCell(paciente.celular ?? "N/A"));
                table.AddCell(CreateCell(paciente.area ?? "Sin Área"));
                table.AddCell(CreateCell(paciente.sexo ?? "N/E"));
                table.AddCell(CreateCell(paciente.nrecord ?? ""));
            }

            document.Add(table);

            var total = new Paragraph($"\nTotal de pacientes activas: {count}")
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(20);
            document.Add(total);

            document.Close();
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Genera PDF con el MISMO diseño que el reporte Crystal Reports
        /// </summary>
        private byte[] GenerarPdfReporteAreaPorAnioEstiloCrystal(
            IEnumerable<dynamic> donacionesPorArea,
            IEnumerable<dynamic> datosPorGenero,
            int anio)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            writer.SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);
            
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, iText.Kernel.Geom.PageSize.LETTER);
            document.SetMargins(36, 36, 36, 36);

            var font = GetCachedFont();

            // SECCIÓN 1: ENCABEZADO
            var logoTable = new Table(UnitValue.CreatePercentArray(new float[] { 15, 85 }));
            logoTable.SetWidth(UnitValue.CreatePercentValue(100));
            
            var logoCell = new Cell()
                .Add(new Paragraph("[LOGO]")
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetBorder(new iText.Layout.Borders.SolidBorder(1))
                .SetHeight(60)
                .SetVerticalAlignment(iText.Layout.Properties.VerticalAlignment.MIDDLE);
            
            logoTable.AddCell(logoCell);
            
            var tituloCell = new Cell()
                .Add(new Paragraph("Rama femenina contra el cáncer")
                    .SetFont(font)
                    .SetFontSize(18)
                    .SetBold()
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetVerticalAlignment(iText.Layout.Properties.VerticalAlignment.MIDDLE);
            
            logoTable.AddCell(tituloCell);
            document.Add(logoTable);
            
            document.Add(new Paragraph($"Fecha de informe: {DateTime.Now:dd/MM/yyyy}")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetMarginTop(10));

            // SECCIÓN 2: SUBTÍTULO
            document.Add(new Paragraph($"Casos atendidos por tipo de afección en: {anio}")
                .SetFont(font)
                .SetFontSize(14)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(20)
                .SetMarginBottom(10));

            // SECCIÓN 3: ÁREA DE GRÁFICA
            var graficaBox = new Paragraph("[ Área reservada para gráfica ]")
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(new iText.Layout.Borders.DashedBorder(1))
                .SetPadding(30)
                .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY)
                .SetMarginTop(10)
                .SetMarginBottom(20);
            
            document.Add(graficaBox);

            // SECCIÓN 4: TABLA DE DETALLES
            var tablaDetalles = new Table(UnitValue.CreatePercentArray(new float[] { 50, 25, 25 }));
            tablaDetalles.SetWidth(UnitValue.CreatePercentValue(100));
            tablaDetalles.UseAllAvailableWidth();
            tablaDetalles.SetMarginTop(10);

            AgregarHeaderCeldaPersonalizado(tablaDetalles, "Área afectada");
            AgregarHeaderCeldaPersonalizado(tablaDetalles, "Cantidad");
            AgregarHeaderCeldaPersonalizado(tablaDetalles, "Porciento");

            int totalCasos = donacionesPorArea.Sum(d => (int)d.Cantidad);

            foreach (var item in donacionesPorArea)
            {
                decimal porcentaje = totalCasos > 0 
                    ? ((decimal)item.Cantidad / totalCasos) * 100 
                    : 0;

                tablaDetalles.AddCell(CreateCeldaConBorde(item.Area));
                tablaDetalles.AddCell(CreateCeldaConBordeCentrada(item.Cantidad.ToString()));
                tablaDetalles.AddCell(CreateCeldaConBordeCentrada($"{porcentaje:F1}%"));
            }

            document.Add(tablaDetalles);

            // SECCIÓN 5: SUBREPORTE GÉNERO
            document.Add(new Paragraph("Distribución por Género")
                .SetFont(font)
                .SetFontSize(12)
                .SetBold()
                .SetMarginTop(30)
                .SetMarginBottom(10));

            var tablaGenero = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }));
            tablaGenero.SetWidth(UnitValue.CreatePercentValue(60));
            tablaGenero.SetMarginTop(10);

            AgregarHeaderCeldaPersonalizado(tablaGenero, "Género");
            AgregarHeaderCeldaPersonalizado(tablaGenero, "Cantidad");

            int totalPacientes = datosPorGenero.Sum(g => (int)g.Cantidad);

            foreach (var genero in datosPorGenero)
            {
                tablaGenero.AddCell(CreateCeldaConBorde(genero.Genero));
                tablaGenero.AddCell(CreateCeldaConBordeCentrada(genero.Cantidad.ToString()));
            }

            tablaGenero.AddCell(CreateCeldaConBordeBold("TOTAL"));
            tablaGenero.AddCell(CreateCeldaConBordeBoldCentrada(totalPacientes.ToString()));

            document.Add(tablaGenero);

            // SECCIÓN 6: PIE DE PÁGINA
            document.Add(new Paragraph($"Total de casos atendidos en {anio}: {totalCasos}")
                .SetFont(font)
                .SetFontSize(10)
                .SetBold()
                .SetMarginTop(20)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"Reporte generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                .SetFontSize(8)
                .SetItalic()
                .SetMarginTop(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.DARK_GRAY));

            document.Close();
            return memoryStream.ToArray();
        }

        private byte[] GenerarPdfReciboIngresos(ReciboParametros parametros)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            writer.SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);
            
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            document.Add(new Paragraph("RECIBO DE INGRESOS")
                .SetFontSize(20)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            document.Add(new Paragraph("RAMA FEMENINA CONTRA EL CÁNCER")
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(30));

            document.Add(new Paragraph($"RECIBO No. {parametros.NumeroRecibo:000000}").SetBold().SetFontSize(14));
            document.Add(new Paragraph($"Fecha: {parametros.Fecha:dd/MM/yyyy}"));
            document.Add(new Paragraph("\n"));
            document.Add(new Paragraph($"Recibimos de: {parametros.Nombre}"));
            
            if (!string.IsNullOrEmpty(parametros.Cedula))
                document.Add(new Paragraph($"Cédula: {parametros.Cedula}"));

            document.Add(new Paragraph("\n"));
            
            var montoParrafo = new Paragraph($"LA SUMA DE: ${parametros.Monto:N2}")
                .SetFontSize(16)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(new iText.Layout.Borders.SolidBorder(1))
                .SetPadding(10);
            document.Add(montoParrafo);

            if (!string.IsNullOrEmpty(parametros.MontoEnLetras))
                document.Add(new Paragraph($"En letras: {parametros.MontoEnLetras}").SetItalic());

            document.Add(new Paragraph($"\nPor concepto de: {parametros.Concepto}"));

            if (!string.IsNullOrEmpty(parametros.NumeroCheque))
                document.Add(new Paragraph($"Cheque No.: {parametros.NumeroCheque}"));

            document.Add(new Paragraph("\n\n\n"));
            document.Add(new Paragraph("_________________________     _________________________"));
            document.Add(new Paragraph("        Firma Autorizada                    Sello"));

            document.Close();
            return memoryStream.ToArray();
        }

        private byte[] GenerarPdfReciboIngresoCompleto(ReciboCompletoParametros parametros)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            writer.SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);
            
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            document.Add(new Paragraph("RECIBO DE INGRESO")
                .SetFontSize(20)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(30));

            document.Add(new Paragraph($"RECIBO No. {parametros.NumeroRecibo:000000}").SetBold());
            document.Add(new Paragraph($"Fecha: {parametros.Fecha:dd/MM/yyyy}"));
            document.Add(new Paragraph($"Recibimos de: {parametros.Nombre}"));

            var montoParrafo = new Paragraph($"LA SUMA DE: ${parametros.Monto:N2}")
                .SetFontSize(16)
                .SetBold()
                .SetBorder(new iText.Layout.Borders.SolidBorder(1))
                .SetPadding(10);
            document.Add(montoParrafo);

            document.Add(new Paragraph($"Por concepto de: {parametros.Concepto}"));

            var formaPago = new StringBuilder("Forma de pago: ");
            formaPago.Append(parametros.Efectivo ? "[X] Efectivo " : "[ ] Efectivo ");
            formaPago.Append(parametros.Cheque ? "[X] Cheque " : "[ ] Cheque ");
            formaPago.Append(parametros.Transferencia ? "[X] Transferencia" : "[ ] Transferencia");

            document.Add(new Paragraph(formaPago.ToString()));

            if (parametros.Cheque && !string.IsNullOrEmpty(parametros.NumeroCheque))
            {
                document.Add(new Paragraph($"Cheque No.: {parametros.NumeroCheque}"));
                document.Add(new Paragraph($"Banco: {parametros.Banco}"));
            }

            document.Close();
            return memoryStream.ToArray();
        }

        private byte[] GenerarPdfReciboDesembolso(DesembolsoParametros parametros)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            writer.SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);
            
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            document.Add(new Paragraph("COMPROBANTE DE DESEMBOLSO")
                .SetFontSize(20)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(30));

            document.Add(new Paragraph($"VOUCHER No. {parametros.NumeroRecibo:000000}").SetBold());
            document.Add(new Paragraph($"Fecha: {parametros.Fecha:dd/MM/yyyy}"));
            document.Add(new Paragraph($"Pagado a: {parametros.Nombre}"));
            document.Add(new Paragraph($"Con cargo a: {parametros.CargoA}"));

            var montoParrafo = new Paragraph($"MONTO: ${parametros.Monto:N2}")
                .SetFontSize(16)
                .SetBold()
                .SetBorder(new iText.Layout.Borders.SolidBorder(1))
                .SetPadding(10);
            document.Add(montoParrafo);

            document.Add(new Paragraph($"Concepto: {parametros.Concepto}"));
            document.Add(new Paragraph("\n\n\n"));
            document.Add(new Paragraph("_________________________     _________________________"));
            document.Add(new Paragraph("      Autorizado por                  Recibido por"));

            document.Close();
            return memoryStream.ToArray();
        }

        #endregion

        #region Métodos Auxiliares para Celdas Personalizadas

        private void AgregarHeaderCeldaPersonalizado(Table table, string texto)
        {
            table.AddHeaderCell(new Cell()
                .Add(new Paragraph(texto))
                .SetFont(GetCachedFont())
                .SetFontSize(10)
                .SetBold()
                .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(new iText.Layout.Borders.SolidBorder(iText.Kernel.Colors.ColorConstants.BLACK, 1))
                .SetPadding(5));
        }

        private Cell CreateCeldaConBorde(string texto)
        {
            return new Cell()
                .Add(new Paragraph(texto ?? ""))
                .SetFont(GetCachedFont())
                .SetFontSize(9)
                .SetBorder(new iText.Layout.Borders.SolidBorder(iText.Kernel.Colors.ColorConstants.GRAY, 0.5f))
                .SetPadding(3);
        }

        private Cell CreateCeldaConBordeCentrada(string texto)
        {
            return new Cell()
                .Add(new Paragraph(texto ?? ""))
                .SetFont(GetCachedFont())
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(new iText.Layout.Borders.SolidBorder(iText.Kernel.Colors.ColorConstants.GRAY, 0.5f))
                .SetPadding(3);
        }

        private Cell CreateCeldaConBordeBold(string texto)
        {
            return new Cell()
                .Add(new Paragraph(texto ?? "").SetBold())
                .SetFont(GetCachedFont())
                .SetFontSize(9)
                .SetBorder(new iText.Layout.Borders.SolidBorder(iText.Kernel.Colors.ColorConstants.BLACK, 1))
                .SetPadding(3)
                .SetBackgroundColor(new iText.Kernel.Colors.DeviceRgb(240, 240, 240));
        }

        private Cell CreateCeldaConBordeBoldCentrada(string texto)
        {
            return new Cell()
                .Add(new Paragraph(texto ?? "").SetBold())
                .SetFont(GetCachedFont())
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(new iText.Layout.Borders.SolidBorder(iText.Kernel.Colors.ColorConstants.BLACK, 1))
                .SetPadding(3)
                .SetBackgroundColor(new iText.Kernel.Colors.DeviceRgb(240, 240, 240));
        }

        #endregion

        #region Métodos Auxiliares Estándar

        private void AgregarEncabezadoReporte(Document document, string titulo)
        {
            document.Add(new Paragraph(titulo)
                .SetFontSize(18)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            document.Add(new Paragraph("RAMA FEMENINA CONTRA EL CÁNCER")
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            document.Add(new Paragraph($"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(30));
        }

        private void AgregarHeaderCelda(Table table, string texto)
        {
            table.AddHeaderCell(new Cell()
                .Add(new Paragraph(texto))
                .SetBold()
                .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY)
                .SetTextAlignment(TextAlignment.CENTER));
        }

        private Cell CreateCell(string texto)
        {
            return new Cell().Add(new Paragraph(texto ?? ""));
        }

        private Cell CreateBoldCell(string texto)
        {
            return new Cell().Add(new Paragraph(texto ?? "").SetBold());
        }

        #endregion

        #region Métodos de Utilidad

        public async Task<string> MostrarPdfAsync(byte[] pdfBytes, string nombreArchivo)
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

        public async Task GuardarPdfAsync(byte[] pdfBytes, string nombreArchivo)
        {
            var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var downloadsFolder = Path.Combine(downloadsPath, "Downloads");
            var filePath = Path.Combine(downloadsFolder, nombreArchivo);

            await File.WriteAllBytesAsync(filePath, pdfBytes);
        }

        #endregion
    }
}
