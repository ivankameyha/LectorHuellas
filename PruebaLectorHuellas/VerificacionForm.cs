using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PruebaLectorHuellas
{
    public partial class VerificacionForm : Form
    {
        private readonly LectorHuellas _lector;
        private CancellationTokenSource _cts;
        public VerificacionForm(LectorHuellas lector)
        {
            InitializeComponent();
            _lector = lector;
            ConfigurarEventosLector();
        }

        private void ConfigurarEventosLector()
        {
            _lector.OnEstadoCambiado += (mensaje) =>
            {
                if (lblEstado.InvokeRequired)
                {
                    lblEstado.Invoke(new Action(() => lblEstado.Text = mensaje));
                }
                else
                {
                    lblEstado.Text = mensaje;
                }
            };
        }

        private async void btnVerificar_Click(object sender, EventArgs e)
        {
            btnVerificar.Enabled = false;
            btnCancelar.Enabled = true;
            _cts = new CancellationTokenSource();

            try
            {
                var huella = await _lector.CapturarHuellaAsync(_cts.Token);
                if (huella != null)
                {
                    int idMiembro = _lector.VerificarHuella(huella);

                    if (idMiembro != -1)
                    {
                        RegistrarAsistencia(idMiembro);
                        MessageBox.Show($"Asistencia registrada para ID: {idMiembro}");
                    }
                    else
                    {
                        MessageBox.Show("Huella no reconocida. Registre primero al miembro.");
                    }
                }
            }
            finally
            {
                btnVerificar.Enabled = true;
                btnCancelar.Enabled = false;
            }
        }

        private void RegistrarAsistencia(int idMiembro)
        {
            string registro = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ID: {idMiembro}";

            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() =>
                {
                    txtLog.AppendText(registro + Environment.NewLine);
                    txtLog.ScrollToCaret();
                }));
            }
            else
            {
                txtLog.AppendText(registro + Environment.NewLine);
                txtLog.ScrollToCaret();
            }
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
        }
    }
}
