using System;
using System.Windows;
using GenteFit.Models;

namespace GenteFit.WPF
{
    public partial class PanelMonitorWindow : Window
    {
        private readonly UsuarioAutenticado _usuarioAutenticado;

        public PanelMonitorWindow(UsuarioAutenticado usuarioAutenticado)
        {
            ArgumentNullException.ThrowIfNull(usuarioAutenticado);
            usuarioAutenticado.ExigirMonitor();

            InitializeComponent();
            _usuarioAutenticado = usuarioAutenticado;

            string nombre = $"{usuarioAutenticado.Name} {usuarioAutenticado.Surname}".Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                nombre = usuarioAutenticado.Username;

            TxtBienvenida.Text = $"¡Bienvenido/a, {nombre}!";
        }

        private void BtnHorario_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new HorarioPersonalWindow(_usuarioAutenticado);
            NavegacionCliente.AbrirDesde(this, ventana);
        }
    }
}