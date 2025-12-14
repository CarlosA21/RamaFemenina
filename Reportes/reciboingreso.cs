using DevExpress.XtraReports.UI;
using System;

namespace RamaFemenina.Reportes
{
    public partial class reciboingreso : DevExpress.XtraReports.UI.XtraReport
    {
        public reciboingreso()
        {
            InitializeComponent();
        }

        public void CargarDatosRecibo(
            string numeroRecibo,
            string fecha,
            string nombre,
            string monto,
            string enLetras,
            string concepto,
            string numeroCheque,
            string banco,
            bool esEfectivo,
            bool esTransferencia,
            bool esCheque)
        {
            // Asignar valores a los labels
            lbl_recibo.Text = numeroRecibo;
            lbl_fecha.Text = fecha;
            lbl_nombre.Text = nombre;
            lbl_monto.Text = monto;
            lbl_enletra.Text = enLetras;
            lbl_concepto.Text = concepto;
            lbl_numcheque.Text = numeroCheque;
            lbl_tbanco.Text = banco;

            // Mostrar checks de tipo de pago (✓ o vacío)
            lbl_checkefectivo.Text = esEfectivo ? "✓" : "";
            lbl_checktransf.Text = esTransferencia ? "✓" : "";
            lbl_checkcheque.Text = esCheque ? "✓" : "";
        }
    }
}


