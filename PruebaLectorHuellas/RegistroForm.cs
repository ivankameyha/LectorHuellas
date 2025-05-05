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
    public partial class RegistroForm : Form
    {
        private readonly LectorHuellas _lector;
        private CancellationTokenSource _cts;
        private int _idMiembro;
        public RegistroForm(LectorHuellas lector)
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
        private async void btnCapturar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtIdMiembro.Text) || !int.TryParse(txtIdMiembro.Text, out _idMiembro))
            {
                MessageBox.Show("Ingrese un ID de miembro válido");
                return;
            }

            btnCapturar.Enabled = false;
            btnCancelar.Enabled = true;
            _cts = new CancellationTokenSource();

            try
            {
                var huella = await _lector.CapturarHuellaAsync(_cts.Token);
                if (huella != null)
                {
                    _lector.RegistrarHuella(_idMiembro, huella);
                    MessageBox.Show($"Huella registrada para ID: {_idMiembro}");
                }
            }
            finally
            {
                btnCapturar.Enabled = true;
                btnCancelar.Enabled = false;
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
