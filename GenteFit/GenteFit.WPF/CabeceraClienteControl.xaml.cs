using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GenteFit.WPF
{
    public partial class CabeceraClienteControl : UserControl
    {
        public event EventHandler? InicioSolicitado;
        public event EventHandler? CerrarSesionSolicitado;

        public CabeceraClienteControl()
        {
            InitializeComponent();
        }

        private void AbrirMenu_Click(object sender, RoutedEventArgs e)
        {
            if (BtnMenu.ContextMenu is { } menu)
            {
                menu.PlacementTarget = BtnMenu;
                menu.Placement = PlacementMode.Bottom;
                menu.IsOpen = true;
            }
        }

        private void Inicio_Click(object sender, RoutedEventArgs e) =>
            InicioSolicitado?.Invoke(this, EventArgs.Empty);

        private void CerrarSesion_Click(object sender, RoutedEventArgs e) =>
            CerrarSesionSolicitado?.Invoke(this, EventArgs.Empty);
    }
}
