using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RamaFemenina.Models
{
    public class Paciente
    {
        public int cedula { get; set; }
        public string nombre { get; set; }
        public string telefono { get; set; }
        public string celular { get; set; }
        public long nrecord { get; set; }
        public string observaciones { get; set; }
        public string sexo { get; set; }
        public string area { get; set; }

    }
}
