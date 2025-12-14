using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using RamaFemenina.Data;
using RamaFemenina.Models;
using Microsoft.Extensions.Configuration;

namespace RamaFemenina.Services
{
    /// <summary>
    /// Servicio para generar reportes en PDF (sin Crystal Reports)
    /// Ahora usa PdfReportService para generar todos los reportes
    /// </summary>
    public class CrystalReportService
    {
        private readonly PdfReportService _pdfService;

        public CrystalReportService(RamaFemeninaContext context, IConfiguration configuration)
        {
            _pdfService = new PdfReportService(context);
            
            Debug.WriteLine($"[REPORT] ? Servicio de reportes PDF inicializado");
        }

        #region Métodos Principales de Reportes

        /// <summary>
        /// Opción 1: Reporte por área
        /// </summary>
        public async Task<string> GenerarReporteAreaAsync()
        {
            return await _pdfService.GenerarReporteAreaAsync();
        }

        /// <summary>
        /// Opción 2: Reporte de Fallecidas
        /// </summary>
        public async Task<string> GenerarReporteFallecidasAsync()
        {
            return await _pdfService.GenerarReporteFallecidasAsync();
        }

        /// <summary>
        /// Opción 3: Reporte de Donaciones por Paciente
        /// </summary>
        public async Task<string> GenerarReporteDonacionesPacienteAsync(int idPaciente)
        {
            return await _pdfService.GenerarReporteDonacionesPacienteAsync(idPaciente);
        }

        /// <summary>
        /// Opción 4: Reporte de Pacientes Activas
        /// </summary>
        public async Task<string> GenerarReporteActivasAsync()
        {
            return await _pdfService.GenerarReporteActivasAsync();
        }

        /// <summary>
        /// Opción 5: Reporte Detallado de Fallecidas
        /// </summary>
        public async Task<string> GenerarReporteFallecidasDetalladoAsync()
        {
            return await _pdfService.GenerarReporteFallecidasDetalladoAsync();
        }

        /// <summary>
        /// Opción 6: Reporte por área y Año
        /// </summary>
        public async Task<string> GenerarReporteAreaPorAnioAsync(int anio)
        {
            return await _pdfService.GenerarReporteAreaPorAnioAsync(anio);
        }

        /// <summary>
        /// Opción 7: Recibo de Ingresos
        /// </summary>
        public async Task<string> GenerarReciboIngresosAsync(ReciboParametros parametros)
        {
            return await _pdfService.GenerarReciboIngresosAsync(parametros);
        }

        /// <summary>
        /// Opción 8: Recibo de Ingreso Completo
        /// </summary>
        public async Task<string> GenerarReciboIngresoCompletoAsync(ReciboCompletoParametros parametros)
        {
            return await _pdfService.GenerarReciboIngresoCompletoAsync(parametros);
        }

        /// <summary>
        /// Opción 9: Recibo de Desembolso
        /// </summary>
        public async Task<string> GenerarReciboDesembolsoAsync(DesembolsoParametros parametros)
        {
            return await _pdfService.GenerarReciboDesembolsoAsync(parametros);
        }

        #endregion

        #region Métodos de Utilidad

        /// <summary>
        /// Obtiene la lista de reportes disponibles
        /// </summary>
        public List<string> ObtenerReportesDisponibles()
        {
            return new List<string>
            {
                "1. Reporte por área",
                "2. Reporte de Fallecidas",
                "3. Reporte de Donaciones por Paciente",
                "4. Reporte de Pacientes Activas",
                "5. Reporte Detallado de Fallecidas",
                "6. Reporte por área y Año",
                "7. Recibo de Ingresos",
                "8. Recibo de Ingreso Completo",
                "9. Recibo de Desembolso"
            };
        }

        #endregion
    }
}
