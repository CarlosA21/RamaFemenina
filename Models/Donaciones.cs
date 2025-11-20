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
        [Column("Fecha")]
        public DateTime Fecha { get; set; }
        
        [Column("valor")]
        public decimal valor { get; set; }
        
        [Column("total")]
        public decimal total { get; set; }
        
        [Required]
        [Column("idPaciente")]
        public string idPaciente { get; set; }  // Cambiado de int a string
        
        [Column("procedimiento")]
        public string procedimiento { get; set; } = string.Empty;
        
        [Column("observacion")]
        public string observacion { get; set; } = string.Empty;
        
        [Column("montoSolicitado")]
        public decimal montoSolicitado { get; set; }

        // Propiedades computadas para la UI
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");

        public string EstadoTexto
        {
            get
            {
                if (total >= montoSolicitado)
                    return "Completado";
                else if (total > 0)
                    return "Parcial";
                else
                    return "Pendiente";
            }
        }

        public SolidColorBrush EstadoColor
        {
            get
            {
                if (total >= montoSolicitado)
                    return new SolidColorBrush(Colors.Green);
                else if (total > 0)
                    return new SolidColorBrush(Colors.Orange);
                else
                    return new SolidColorBrush(Colors.Red);
            }
        }

        public decimal Diferencia => montoSolicitado - total;
        
        public decimal PorcentajeCompletado => montoSolicitado > 0 ? (total / montoSolicitado) * 100 : 0;
    }
}
