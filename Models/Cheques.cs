using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RamaFemenina.Models
{
    [Table("Cheques")]
    public class Cheques
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("idCheque")]
        public int idCheque { get; set; }

        [Column("monto")]
        public decimal monto { get; set; }
        
        [Required]
        [Column("Fecha")]
        public DateTime Fecha { get; set; }

        [Column("nombre")]
        public string nombre { get; set; }

        [Column("concepto")]
        public string concepto { get; set; }
        
        [Column("numero")]
        public string numero { get; set; }

        // Propiedad para mostrar la fecha formateada
        [NotMapped]
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
    }
}
