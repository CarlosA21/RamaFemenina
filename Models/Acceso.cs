using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RamaFemenina.Models
{
    [Table("acceso")]
    public class Acceso
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("idusuario")]
        public int IdUsuario { get; set; }

        [Required]
        [Column("usuario")]
        public string Usuario { get; set; } = string.Empty;

        [Required]
        [Column("contraseña")]
        public string Contraseña { get; set; } = string.Empty;

        [Column("rol")]
        [MaxLength(20)]
        public string? Rol { get; set; } = "Moderador";
    }
}
