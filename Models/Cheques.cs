using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RamaFemenina.Models
{
    [Table("cheques")]
    public class Cheques
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("idcheque")]
        public int idCheque { get; set; }

        [Column("monto")]
        public decimal monto { get; set; }
        
        [Required]
        [Column("fecha")]
        public DateTime Fecha { get; set; }

        [Required]
        [Column("nombre")]
        [MaxLength(200)]
        public string nombre { get; set; } = string.Empty;

        [Column("concepto")]
        [MaxLength(200)]
        public string? concepto { get; set; } // Permitir NULL y usar nullable reference type
        
        [Required]
        [Column("numero")]
        [MaxLength(50)]
        public string numero { get; set; } = string.Empty;

        // Propiedad para mostrar la fecha formateada
        [NotMapped]
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
        
        // Propiedad para manejar concepto seguro (nunca NULL)
        [NotMapped]
        public string ConceptoSeguro => concepto ?? string.Empty;
    }
}
