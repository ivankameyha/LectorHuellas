using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PruebaLectorHuellas
{
    public partial class Form1 : Form
    {
        private LectorHuellas _lector;

        public Form1()
        {
            InitializeComponent();
            _lector = new LectorHuellas();
            ConfigurarEventosLector();

            if (!_lector.InicializarLector())
            {
                MessageBox.Show("No se pudo inicializar el lector. La aplicación se cerrará.");
                this.Close();
            }
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

            _lector.OnImagenCapturada += (imagen) =>
            {
                if (pbHuella.InvokeRequired)
                {
                    pbHuella.Invoke(new Action(() => pbHuella.Image = imagen));
                }
                else
                {
                    pbHuella.Image = imagen;
                }
            };
        }

        private void btnRegistrar_Click(object sender, EventArgs e)
        {
            var registroForm = new RegistroForm(_lector);
            registroForm.ShowDialog();
        }

        private void btnVerificar_Click(object sender, EventArgs e)
        {
            var verificacionForm = new VerificacionForm(_lector);
            verificacionForm.ShowDialog();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _lector.CerrarLector();
        }
    }
}
