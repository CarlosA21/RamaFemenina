using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace RamaFemenina.Models
{
    [Table("cajachica")]
    public class CajaChica
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("idrecibo")]
        public int IdRecibo { get; set; }
        
        [Required]
        [Column("recibo")]
        public int NumeroRecibo { get; set; }
        
        [Required]
        [Column("fecha")]
        public DateTime Fecha { get; set; }
        
        [Required]
        [Column("nombre")]
        [MaxLength(50)]
        public string PagadoA { get; set; } = string.Empty;
        
        [Required]
        [Column("monto")]
        public decimal Monto { get; set; }
        
        [Required]
        [Column("cargoa")]
        [MaxLength(50)]
        public string ConCargoA { get; set; } = string.Empty;
        
        [Required]
        [Column("concepto")]
        [MaxLength(100)]
        public string Concepto { get; set; } = string.Empty;

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
