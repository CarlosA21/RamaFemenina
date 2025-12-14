using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace RamaFemenina.Models
{
    [Table("Donaciones")]
    public class Donaciones
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Iddonacion")]
        public int idDonacion { get; set; }
        
        [Required]
        [Column("fecha")]
        public DateTime Fecha { get; set; }
        
        [Column("valor")]
        public decimal valor { get; set; }
        
        [Column("total")]
        public decimal total { get; set; }
        
        [Required]
        [Column("idpaciente")]
        public int idPaciente { get; set; }
        
        [Column("procedimiento")]
        [MaxLength(50)]
        public string? procedimiento { get; set; }
        
        [Column("observacion")]
        [MaxLength(300)]
        public string? observacion { get; set; }
        
        [Column("montoSolicitado")]
        public decimal montoSolicitado { get; set; }

        // Propiedad de navegación
        public virtual Paciente? Paciente { get; set; }

        // Propiedades computadas para la UI
        [NotMapped]
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");

        [NotMapped]
        public string EstadoTexto
        {
            get
            {
                if (total >= montoSolicitado && montoSolicitado > 0)
                    return "Completado";
                else if (total > 0)
                    return "Parcial";
                else
                    return "Pendiente";
            }
        }

        [NotMapped]
        public SolidColorBrush EstadoColor
        {
            get
            {
                if (total >= montoSolicitado && montoSolicitado > 0)
                    return new SolidColorBrush(Colors.Green);
                else if (total > 0)
                    return new SolidColorBrush(Colors.Orange);
                else
                    return new SolidColorBrush(Colors.Red);
            }
        }

        [NotMapped]
        public decimal Diferencia => montoSolicitado - total;
        
        [NotMapped]
        public decimal PorcentajeCompletado => montoSolicitado > 0 ? (total / montoSolicitado) * 100 : 0;
    }
}
