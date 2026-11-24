using System;
using System.Windows;
using GenteFit.Models;

namespace GenteFit.WPF
{
    public partial class PanelRecepcionWindow : Window
    {
        private readonly UsuarioAutenticado _usuarioAutenticado;

        public PanelRecepcionWindow(UsuarioAutenticado usuarioAutenticado)
        {
            ArgumentNullException.ThrowIfNull(usuarioAutenticado);
            usuarioAutenticado.ExigirRecepcionista();

            InitializeComponent();
            _usuarioAutenticado = usuarioAutenticado;

            string nombre = $"{usuarioAutenticado.Name} {usuarioAutenticado.Surname}".Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                nombre = usuarioAutenticado.Username;
            
            TxtBienvenida.Text = $"¡Bienvenido/a, {nombre}!";
        }

        private void BtnClientes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new ClientesRecepcionWindow(_usuarioAutenticado)
            {
                Owner=this
            };
            ventana.ShowDialog();
        }
        private void BtnHorario_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new HorarioPersonalWindow(_usuarioAutenticado) { Owner = this };
            ventana.ShowDialog();
        }
    }
}