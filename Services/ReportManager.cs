using System;
using System.Threading.Tasks;
using RamaFemenina.Services;
using RamaFemenina.Models;
using Microsoft.Extensions.DependencyInjection;

namespace RamaFemenina.Services
{
    /// <summary>
    /// Gestor optimizado de reportes que unifica PDF Reports y Simple Reports
    /// </summary>
    public class ReportManager
    {
        private readonly CrystalReportService _crystalReportService;
        private readonly SimpleReportService _simpleReportService;

        public ReportManager(
            CrystalReportService crystalReportService,
            SimpleReportService simpleReportService)
        {
            _crystalReportService = crystalReportService;
            _simpleReportService = simpleReportService;
        }

        /// <summary>
        /// Genera reporte usando servicio PDF (configuración automática de BD, parámetros y filtros)
        /// </summary>
        public async Task<string> GenerarReporteCrystalAsync(int opcion, ReportParameters? parameters = null)
        {
            return opcion switch
            {
                1 => await _crystalReportService.GenerarReporteAreaAsync(),
                2 => await _crystalReportService.GenerarReporteFallecidasAsync(),
                3 => await _crystalReportService.GenerarReporteDonacionesPacienteAsync(
                    parameters?.IdPaciente ?? throw new ArgumentException("IdPaciente requerido")),
                4 => await _crystalReportService.GenerarReporteActivasAsync(),
                5 => await _crystalReportService.GenerarReporteFallecidasDetalladoAsync(),
                6 => await _crystalReportService.GenerarReporteAreaPorAnioAsync(
                    parameters?.Anio ?? DateTime.Now.Year),
                7 => await _crystalReportService.GenerarReciboIngresosAsync(
                    parameters?.ReciboParms ?? throw new ArgumentException("ReciboParms requerido")),
                8 => await _crystalReportService.GenerarReciboIngresoCompletoAsync(
                    parameters?.ReciboCompletoParms ?? throw new ArgumentException("ReciboCompletoParms requerido")),
                9 => await _crystalReportService.GenerarReciboDesembolsoAsync(
                    parameters?.DesembolsoParms ?? throw new ArgumentException("DesembolsoParms requerido")),
                _ => throw new ArgumentException($"Opción de reporte no válida: {opcion}")
            };
        }

        /// <summary>
        /// Genera reporte simple en PDF (iText)
        /// </summary>
        public async Task<byte[]> GenerarReporteSimplePdfAsync(int opcion, ReportParameters? parameters = null)
        {
            return opcion switch
            {
                1 => await _simpleReportService.GenerarReporteAreaAsync(),
                2 => await _simpleReportService.GenerarReporteFalleccidasAsync(),
                3 => await _simpleReportService.GenerarReporteDonacionesPacienteAsync(
                    parameters?.IdPaciente ?? throw new ArgumentException("IdPaciente requerido")),
                4 => await _simpleReportService.GenerarReporteActivasAsync(),
                5 => await _simpleReportService.GenerarReporteFallecidasDetalladoAsync(),
                6 => await _simpleReportService.GenerarReporteAreaPorAnioAsync(
                    parameters?.Anio ?? DateTime.Now.Year),
                7 => await _simpleReportService.GenerarReciboIngresosAsync(
                    parameters?.ReciboParms ?? throw new ArgumentException("ReciboParms requerido")),
                8 => await _simpleReportService.GenerarReciboIngresoCompletoAsync(
                    parameters?.ReciboCompletoParms ?? throw new ArgumentException("ReciboCompletoParms requerido")),
                9 => await _simpleReportService.GenerarReciboDesembolsoAsync(
                    parameters?.DesembolsoParms ?? throw new ArgumentException("DesembolsoParms requerido")),
                _ => throw new ArgumentException($"Opción de reporte simple no válida: {opcion}")
            };
        }

        /// <summary>
        /// Muestra un reporte (usa servicio PDF por defecto)
        /// </summary>
        public async Task<string> MostrarReporteAsync(int opcion, ReportParameters? parameters = null)
        {
            // Por defecto, usa el servicio PDF que se muestra automáticamente
            return await GenerarReporteCrystalAsync(opcion, parameters);
        }

        /// <summary>
        /// Muestra un reporte simple PDF (guarda temporal y abre)
        /// </summary>
        public async Task<string> MostrarReporteSimplePdfAsync(int opcion, ReportParameters? parameters = null)
        {
            var pdfBytes = await GenerarReporteSimplePdfAsync(opcion, parameters);
            var nombreArchivo = $"Reporte_{opcion}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return await _simpleReportService.MostrarPdfAsync(pdfBytes, nombreArchivo);
        }

        /// <summary>
        /// Obtiene lista de reportes disponibles
        /// </summary>
        public string[] ObtenerReportesCrystalDisponibles()
        {
            return _crystalReportService.ObtenerReportesDisponibles().ToArray();
        }

        /// <summary>
        /// Método estático para crear instancia desde IServiceProvider (versión asíncrona)
        /// </summary>
        public static Task<ReportManager> CreateAsync(IServiceProvider serviceProvider)
        {
            var crystalService = serviceProvider.GetRequiredService<CrystalReportService>();
            var simpleService = serviceProvider.GetRequiredService<SimpleReportService>();
            return Task.FromResult(new ReportManager(crystalService, simpleService));
        }

        /// <summary>
        /// Método estático para crear instancia desde IServiceProvider (versión síncrona)
        /// </summary>
        public static ReportManager Create(IServiceProvider serviceProvider)
        {
            var crystalService = serviceProvider.GetRequiredService<CrystalReportService>();
            var simpleService = serviceProvider.GetRequiredService<SimpleReportService>();
            return new ReportManager(crystalService, simpleService);
        }

        #region Métodos de Conveniencia

        /// <summary>
        /// Genera reporte de área (Opción 1)
        /// </summary>
        public Task<string> GenerarReporteAreaAsync() 
            => GenerarReporteCrystalAsync(1);

        /// <summary>
        /// Genera reporte de fallecidas (Opción 2)
        /// </summary>
        public Task<string> GenerarReporteFallecidasAsync() 
            => GenerarReporteCrystalAsync(2);

        /// <summary>
        /// Genera reporte de donaciones por paciente (Opción 3)
        /// </summary>
        public Task<string> GenerarReporteDonacionesPacienteAsync(string idPaciente)
            => GenerarReporteCrystalAsync(3, new ReportParameters { IdPaciente = idPaciente });

        /// <summary>
        /// Genera reporte de activas (Opción 4)
        /// </summary>
        public Task<string> GenerarReporteActivasAsync()
            => GenerarReporteCrystalAsync(4);

        /// <summary>
        /// Genera reporte de área por año (Opción 6)
        /// </summary>
        public Task<string> GenerarReporteAreaPorAnioAsync(int anio)
            => GenerarReporteCrystalAsync(6, new ReportParameters { Anio = anio });

        /// <summary>
        /// Genera recibo de ingresos (Opción 7)
        /// </summary>
        public Task<string> GenerarReciboIngresosAsync(ReciboParametros parametros)
            => GenerarReporteCrystalAsync(7, new ReportParameters { ReciboParms = parametros });

        /// <summary>
        /// Genera recibo completo (Opción 8)
        /// </summary>
        public Task<string> GenerarReciboIngresoCompletoAsync(ReciboCompletoParametros parametros)
            => GenerarReporteCrystalAsync(8, new ReportParameters { ReciboCompletoParms = parametros });

        /// <summary>
        /// Genera recibo de desembolso (Opción 9)
        /// </summary>
        public Task<string> GenerarReciboDesembolsoAsync(DesembolsoParametros parametros)
            => GenerarReporteCrystalAsync(9, new ReportParameters { DesembolsoParms = parametros });

        #endregion
    }
}