using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace RamaFemenina.Models
{
    [Table("factura")]
    public class Factura
    {
        // Constructor para inicializar valores por defecto
        public Factura()
        {
            // Inicializar campos decimales explícitamente
            Exento = 0.00m;
            Gravado = 0.00m;  
            Itbis = 0.00m;
            APagar = 0.00m;
            Pago = 0.00m;
            Cambio = 0.00m;
            
            // Inicializar campos booleanos
            EsCredito = false;
            EsEfectivo = false;
            EsCheque = false;
            
            // Inicializar campos de texto
            NulaTexto = "NO";
            
            // Fecha por defecto
            Fecha = DateTime.Now;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("idfactura")]
        public int IdFactura { get; set; } // PK Identity

        [Column("nofactura")]
        public int NoFactura { get; set; } // Mismo valor que IdFactura

        [Column("fecha")]
        public DateTime Fecha { get; set; }

        [Column("idcliente")]
        public int? IdCliente { get; set; }

        [Required]
        [Column("exento")]
        public decimal Exento { get; set; } = 0m;

        [Required]
        [Column("gravado")]
        public decimal Gravado { get; set; } = 0m;

        [Required]
        [Column("itbis")]
        public decimal Itbis { get; set; } = 0m;

        [Required]
        [Column("apagar")]
        public decimal APagar { get; set; } = 0m;

        [Column("cred")]
        public bool EsCredito { get; set; }

        [Column("efec")]
        public bool EsEfectivo { get; set; }

        [Column("cheq")]
        public bool EsCheque { get; set; }

        [Column("cheque")]
        public string? NumeroCheque { get; set; }

        [Column("banco")]
        public string? Banco { get; set; }

        [Column("fechapago")]
        public DateTime? FechaPago { get; set; }

        [Column("pago")]
        public decimal? Pago { get; set; } = 0m;

        [Column("ncf")]
        public long? NCFNumerico { get; set; }

        [Column("cambio")]
        public decimal? Cambio { get; set; } = 0m;

        [Column("tcf")]
        public int? TCFNumerico { get; set; }

        [Column("nula")]
        public string? NulaTexto { get; set; }

        [Column("fechav2")]
        public string? FechaVencimientoTexto { get; set; }

        // Relación con Cliente
        public virtual Clientes? Cliente { get; set; }
        
        // ============================================
        // PROPIEDADES COMPUTADAS PARA COMPATIBILIDAD
        // ============================================
        
        [NotMapped]
        public string? Numero 
        { 
            get => NoFactura.ToString();
            set { /* Ignorar asignación, se usa NoFactura */ }
        }
        
        [NotMapped]
        public decimal Total 
        { 
            get => APagar;
            set => APagar = value;
        }
        
        [NotMapped]
        public string? Observaciones { get; set; }
        
        [NotMapped]
        public int Estado 
        { 
            get 
            {
                // Mapear desde NulaTexto y estado de pago
                if (NulaTexto == "SI" || NulaTexto == "S") return 0; // Anulada
                if (Pago >= APagar && APagar > 0) return 2; // Pagada
                if (Pago > 0) return 1; // Parcial
                return 3; // Pendiente
            }
            set 
            { 
                // Mapear a NulaTexto
                if (value == 0) NulaTexto = "SI";
                else NulaTexto = "NO";
            }
        }
        
        [NotMapped]
        public bool? EsNula 
        { 
            get => NulaTexto == "SI" || NulaTexto == "S";
            set => NulaTexto = value == true ? "SI" : "NO";
        }
        
        [NotMapped]
        public string? NCF 
        { 
            get => NCFNumerico?.ToString();
            set => NCFNumerico = long.TryParse(value, out long ncf) ? ncf : null;
        }
        
        [NotMapped]
        public string? TCF 
        { 
            get => TCFNumerico?.ToString();
            set => TCFNumerico = int.TryParse(value, out int tcf) ? tcf : null;
        }
        
        [NotMapped]
        public DateTime? FechaVencimiento 
        { 
            get => DateTime.TryParse(FechaVencimientoTexto, out DateTime fecha) ? fecha : null;
            set => FechaVencimientoTexto = value?.ToString("dd/MM/yyyy");
        }
        
        // ============================================
        // PROPIEDADES COMPUTADAS PARA LA UI
        // ============================================
        
        [NotMapped]
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
        
        [NotMapped]
        public string FechaPagoFormateada => FechaPago?.ToString("dd/MM/yyyy") ?? "Pendiente";
        
        [NotMapped]
        public string FechaVencimientoFormateada => FechaVencimiento?.ToString("dd/MM/yyyy") ?? "N/A";
        
        [NotMapped]
        public string NombreCliente => Cliente?.nombre ?? "Sin cliente";
        
        [NotMapped]
        public string EstadoPago
        {
            get
            {
                if (EsNula == true) return "Anulada";
                
                var pagoSafe = Pago ?? 0m;
                var totalSafe = APagar;
                
                if (pagoSafe >= totalSafe && totalSafe > 0) return "Pagada";
                if (pagoSafe > 0) return "Parcial";
                return "Pendiente";
            }
        }
        
        [NotMapped]
        public SolidColorBrush EstadoPagoColor
        {
            get
            {
                return EstadoPago switch
                {
                    "Pagada" => new SolidColorBrush(Colors.Green),
                    "Parcial" => new SolidColorBrush(Colors.Orange),
                    "Pendiente" => new SolidColorBrush(Colors.Red),
                    "Anulada" => new SolidColorBrush(Colors.Gray),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }
        
        [NotMapped]
        public string EstadoPagoIcono
        {
            get
            {
                return EstadoPago switch
                {
                    "Pagada" => "✅",
                    "Parcial" => "⏳",
                    "Pendiente" => "⏰",
                    "Anulada" => "❌",
                    _ => "❓"
                };
            }
        }
        
        [NotMapped]
        public string TipoPago
        {
            get
            {
                if (EsEfectivo) return "Efectivo";
                if (EsCheque) return "Cheque";
                if (EsCredito) return "Crédito";
                return "No especificado";
            }
        }
        
        [NotMapped]
        public SolidColorBrush TipoPagoColor
        {
            get
            {
                if (EsEfectivo) return new SolidColorBrush(Colors.Green);
                if (EsCheque) return new SolidColorBrush(Colors.Orange);
                if (EsCredito) return new SolidColorBrush(Colors.Blue);
                return new SolidColorBrush(Colors.Gray);
            }
        }
        
        [NotMapped]
        public decimal Pendiente => APagar - (Pago ?? 0m);
        
        [NotMapped]
        public decimal PorcentajePagado => APagar > 0 ? ((Pago ?? 0m) / APagar) * 100 : 0;
        
        [NotMapped]
        public double PorcentajePagadoDouble => APagar > 0 ? (double)(((Pago ?? 0m) / APagar) * 100) : 0;
        
        [NotMapped]
        public string DetallesPago
        {
            get
            {
                if (EsCheque && !string.IsNullOrEmpty(NumeroCheque))
                    return $"Cheque: {NumeroCheque} - {Banco ?? ""}";
                if (NCFNumerico.HasValue)
                    return $"NCF: {NCF}";
                if (!string.IsNullOrEmpty(Observaciones))
                    return Observaciones;
                return string.Empty;
            }
        }
        
        [NotMapped]
        public string MontoFormateado => $"RD$ {APagar:N2}";
        
        [NotMapped]
        public string PagoFormateado => $"RD$ {(Pago ?? 0m):N2}";
        
        [NotMapped]
        public string PendienteFormateado => $"RD$ {Pendiente:N2}";
        
        [NotMapped]
        public string NumeroFacturaFormateado => NoFactura.ToString();
        
        [NotMapped]
        public string EstadoTexto => EstadoPago;
    }
}
