using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace RamaFemenina.Models
{
    [Table("CajaChica")]
    public class CajaChica
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("idrecibo")]
        public int IdRecibo { get; set; }
        
        [Column("recibo")]
        public int NumeroRecibo { get; set; }
        
        [Required]
        [Column("fecha")]
        public DateTime Fecha { get; set; }
        
        [Required]
        [Column("nombre")]
        public string PagadoA { get; set; } = string.Empty;
        
        [Column("monto")]
        public decimal Monto { get; set; }
        
        [Column("cargoa")]
        public string? ConCargoA { get; set; }
        
        [Column("concepto")]
        public string? Concepto { get; set; }

        // Propiedades computadas para la UI
        [NotMapped]
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
        
        [NotMapped]
        public string MontoFormateado => $"${Monto:N2}";
        
        [NotMapped]
        public SolidColorBrush MontoColor
        {
            get
            {
                return Monto >= 1000 
                    ? new SolidColorBrush(Colors.OrangeRed) 
                    : new SolidColorBrush(Colors.Green);
            }
        }
    }
}
