using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RamaFemenina.Models
{
    [Table("Pacientes")]
    public class Paciente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("idpaciente")]
        public int idpaciente { get; set; }

        [Required]
        [Column("cedula")]
        [MaxLength(50)]
        public string cedula { get; set; }

        [Required]
        [Column("nombre")]
        [MaxLength(50)]
        public string nombre { get; set; }

        [Column("telefono")]
        [MaxLength(50)]
        public string telefono { get; set; }

        [Column("celular")]
        [MaxLength(50)]
        public string celular { get; set; }

        [Required]
        [Column("estado")]
        [MaxLength(50)]
        public string estado { get; set; }

        [Required]
        [Column("nrecord")]
        [MaxLength(50)]
        public string nrecord { get; set; }

        [Column("observaciones")]
        [MaxLength(300)]
        public string observaciones { get; set; }

        [Column("sexo")]
        [MaxLength(50)]
        public string sexo { get; set; }

        [Column("area")]
        [MaxLength(50)]
        public string area { get; set; }
    }
}
