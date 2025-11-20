using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RamaFemenina.Models
{
    [Table("Clientes")]
    public class Clientes
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("idCliente")]
        public int idCliente { get; set; }
        
        [Required]
        [Column("nombre")]
        public string nombre { get; set; }

        [Column("telefono")]
        public string telefono { get; set; }
        
        [Column("direccion")]
        public string direccion { get; set; }
        
        [Column("rnc")]
        public string rnc { get; set; }
    }
}
