using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace RamaFemenina.Models
{
    [Table("Recibo")]
    public class Recibo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("NumeroRecibo")]
        public int NumeroRecibo { get; set; }
        
        [Required]
        [Column("Fecha")]
        public DateTime Fecha { get; set; }
        
        // Tipo de recibo: "Ingreso" o "Egreso"
        [Required]
        [Column("TipoRecibo")]
        public string TipoRecibo { get; set; } = "Ingreso";
        
        [Required]
        [Column("RecibimosDe")]
        public string RecibimosDe { get; set; } = string.Empty;
        
        // Nuevo campo para Recibos de Egreso (Cédula en vez de Cheque No.)
        [Column("Cedula")]
        public string? Cedula { get; set; }
        
        [Column("Monto")]
        public decimal Monto { get; set; }
        
        [Column("MontoEnLetras")]
        public string? MontoEnLetras { get; set; }
        
        [Column("Concepto")]
        public string? Concepto { get; set; }
        
        // Tipo de pago
        [Column("EsEfectivo")]
        public bool EsEfectivo { get; set; }
        
        [Column("EsTransferencia")]
        public bool EsTransferencia { get; set; }
        
        [Column("EsCheque")]
        public bool EsCheque { get; set; }
        
        // Datos de transferencia
        [Column("NumeroFacturaNCF")]
        public string? NumeroFacturaNCF { get; set; }
        
        // Datos de cheque
        [Column("NumeroCheque")]
        public string? NumeroCheque { get; set; }
        
        [Column("Banco")]
        public string? Banco { get; set; }

        // Propiedades computadas para la UI
        [NotMapped]
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
        
        [NotMapped]
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

        [NotMapped]
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

        [NotMapped]
        public string DetallesPago
        {
            get
            {
                if (EsTransferencia && !string.IsNullOrEmpty(NumeroFacturaNCF))
                    return $"NCF: {NumeroFacturaNCF}";
                if (EsCheque && !string.IsNullOrEmpty(NumeroCheque))
                    return $"Cheque: {NumeroCheque} - {Banco ?? ""}";
                return string.Empty;
            }
        }
        
        [NotMapped]
        public SolidColorBrush TipoReciboColor
        {
            get
            {
                return TipoRecibo == "Ingreso" 
                    ? new SolidColorBrush(Colors.Green) 
                    : new SolidColorBrush(Colors.Red);
            }
        }
        
        [NotMapped]
        public string TipoReciboIcono
        {
            get
            {
                return TipoRecibo == "Ingreso" ? "?" : "?";
            }
        }
    }
}
