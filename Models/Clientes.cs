using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RamaFemenina.Models
{
    [Table("clientes")]
    public class Clientes
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("idcliente")]
        public int idCliente { get; set; }
        
        [Required]
        [Column("nombre")]
        [MaxLength(150)]
        public string nombre { get; set; }

        [Required]
        [Column("telefono")]
        [MaxLength(50)]
        public string telefono { get; set; }
        
        [Required]
        [Column("direccion")]
        [MaxLength(200)]
        public string direccion { get; set; }
        
        [Column("rnc")]
        [MaxLength(50)]
        public string rnc { get; set; }
    }
}
