using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace RamaFemenina.Models
{
    [Table("inrecibo")]
    public class Recibo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("idrecibo")]
        public int IdRecibo { get; set; }
        
        [Column("nrecibo")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)] // Generado por la BD (sequence + default)
        public int NumeroRecibo { get; set; }
        
        [Required]
        [Column("fecha")]
        public DateTime Fecha { get; set; }
        
        [Required]
        [Column("nombre")]
        [MaxLength(200)]
        public string RecibimosDe { get; set; } = string.Empty;
        
        [Column("cheque")]
        [MaxLength(50)]
        public string? Cedula { get; set; }
        
        [Column("monto")]
        public decimal Monto { get; set; }
        
        // Este campo no existe en tu tabla, lo calcularemos
        [NotMapped]
        public string? MontoEnLetras { get; set; }
        
        [Column("concepto")]
        [MaxLength(200)]
        public string? Concepto { get; set; }
        
        // Tipo de recibo - como no hay columna específica en la BD, 
        // lo manejamos como NotMapped con valor por defecto "Ingreso"
        [NotMapped]
        public string TipoRecibo { get; set; } = "Ingreso";
        
        // Tipo de pago
        [Column("efect")]
        public bool EsEfectivo { get; set; }
        
        [Column("trans")]
        public bool EsTransferencia { get; set; }
        
        [Column("cheq")]
        public bool EsCheque { get; set; }
        
        // Datos de transferencia
        [Column("factura")]
        [MaxLength(100)]
        public string? NumeroFacturaNCF { get; set; }
        
        // Datos de cheque (reutilizamos el campo cheque)
        [NotMapped]
        public string? NumeroCheque 
        { 
            get => Cedula;
            set => Cedula = value;
        }
        
        [Column("banco")]
        [MaxLength(100)]
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
                if (EsCheque && !string.IsNullOrEmpty(Cedula))
                    return $"Cheque: {Cedula} - {Banco ?? ""}";
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
                return TipoRecibo == "Ingreso" ? "??" : "??";
            }
        }
    }
}
