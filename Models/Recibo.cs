using System;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace RamaFemenina.Models
{
    public class Recibo
    {
        public int NumeroRecibo { get; set; }
        public DateTime Fecha { get; set; }
        public string RecibimosDe { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string MontoEnLetras { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        
        // Tipo de pago
        public bool EsEfectivo { get; set; }
        public bool EsTransferencia { get; set; }
        public bool EsCheque { get; set; }
        
        // Datos de transferencia
        public string NumeroFacturaNCF { get; set; } = string.Empty;
        
        // Datos de cheque
        public string NumeroCheque { get; set; } = string.Empty;
        public string Banco { get; set; } = string.Empty;

        // Propiedades computadas para la UI
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
        
        public string TipoPago
        {
            get
            {
                if (EsEfectivo) return "Efectivo";
                if (EsTransferencia) return "Transferencia";
                if (EsCheque) return "Cheque";
                return "No especificado";
            }
        }

        public SolidColorBrush TipoPagoColor
        {
            get
            {
                if (EsEfectivo) return new SolidColorBrush(Colors.Green);
                if (EsTransferencia) return new SolidColorBrush(Colors.Blue);
                if (EsCheque) return new SolidColorBrush(Colors.Orange);
                return new SolidColorBrush(Colors.Gray);
            }
        }

        public string DetallesPago
        {
            get
            {
                if (EsTransferencia && !string.IsNullOrEmpty(NumeroFacturaNCF))
                    return $"NCF: {NumeroFacturaNCF}";
                if (EsCheque && !string.IsNullOrEmpty(NumeroCheque))
                    return $"Cheque: {NumeroCheque} - {Banco}";
                return string.Empty;
            }
        }
    }
}
