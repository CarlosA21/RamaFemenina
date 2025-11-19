using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RamaFemenina.Models
{
    public class Cheques
    {
        public int idCheque { get; set; }

        public decimal monto { get; set; }
        public DateTime Fecha { get; set; }

        public string nombre { get; set; }

        public string concepto { get; set; }
        public string numero { get; set; }

        // Propiedad para mostrar la fecha formateada
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
    }
}
