using System;

namespace RamaFemenina.Models
{
    /// <summary>
    /// Parámetros para Recibo de Ingresos
    /// </summary>
    public class ReciboParametros
    {
        public int NumeroRecibo { get; set; }
        public DateTime Fecha { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string MontoEnLetras { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string NumeroCheque { get; set; } = string.Empty;
    }

    /// <summary>
    /// Parámetros para Recibo de Ingreso Completo
    /// </summary>
    public class ReciboCompletoParametros
    {
        public int NumeroRecibo { get; set; }
        public DateTime Fecha { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string MontoEnLetras { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string NumeroCheque { get; set; } = string.Empty;
        public string Banco { get; set; } = string.Empty;
        public string NCF { get; set; } = string.Empty;
        public bool Efectivo { get; set; }
        public bool Cheque { get; set; }
        public bool Transferencia { get; set; }
    }

    /// <summary>
    /// Parámetros para Recibo de Desembolso (Caja Chica)
    /// </summary>
    public class DesembolsoParametros
    {
        public int NumeroRecibo { get; set; }
        public DateTime Fecha { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string MontoEnLetras { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string CargoA { get; set; } = string.Empty;
    }

    /// <summary>
    /// Clase contenedora para todos los parámetros de reportes
    /// </summary>
    public class ReportParameters
    {
        public string? IdPaciente { get; set; }
        public int? Anio { get; set; }
        public ReciboParametros? ReciboParms { get; set; }
        public ReciboCompletoParametros? ReciboCompletoParms { get; set; }
        public DesembolsoParametros? DesembolsoParms { get; set; }
    }
}
