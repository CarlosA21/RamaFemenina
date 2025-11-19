using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace RamaFemenina.Models
{
    public class Donaciones
    {
        public int idDonacion { get; set; }
        
        public DateTime Fecha { get; set; }
        public decimal valor { get; set; }
        public decimal total { get; set; }
        
        public int idPaciente { get; set; }
        public string procedimiento { get; set; } = string.Empty;
        public string observacion { get; set; } = string.Empty;
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
