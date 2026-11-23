
using System.Windows;
using System.Windows.Controls;

namespace GenteFit.WPF
{
    public partial class MainWindow : Window
    {
        private bool _contrasenaVisible;

        public MainWindow()
        {
            InitializeComponent();
            ActualizarPlaceholder();
        }

        private void MostrarContrasena_Click(
            object sender, RoutedEventArgs e)
        {
            _contrasenaVisible = !_contrasenaVisible;

            OjoTachado.Visibility = _contrasenaVisible
                ? Visibility.Collapsed
                : Visibility.Visible;

            if (_contrasenaVisible)
            {
                ContrasenaVisibleTextBox.Text =
                    ContrasenaPasswordBox.Password;

                ContrasenaPasswordBox.Visibility =
                    Visibility.Collapsed;

                ContrasenaVisibleTextBox.Visibility =
                    Visibility.Visible;

                ContrasenaVisibleTextBox.Focus();
            }
            else
            {
                ContrasenaPasswordBox.Password =
                    ContrasenaVisibleTextBox.Text;

                ContrasenaVisibleTextBox.Visibility =
                    Visibility.Collapsed;

                ContrasenaPasswordBox.Visibility =
                    Visibility.Visible;

                ContrasenaPasswordBox.Focus();
            }

            ActualizarPlaceholder();
        }

        private void ContrasenaPasswordBox_PasswordChanged(
            object sender, RoutedEventArgs e)
        {
            ActualizarPlaceholder();
        }

        private void ContrasenaVisibleTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            ActualizarPlaceholder();
        }

        private void ActualizarPlaceholder()
        {
            if (ContrasenaPlaceholder is null)
                return;

            bool estaVacia = _contrasenaVisible
                ? string.IsNullOrEmpty(ContrasenaVisibleTextBox.Text)
                : string.IsNullOrEmpty(ContrasenaPasswordBox.Password);

            ContrasenaPlaceholder.Visibility = estaVacia
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        
        private async void IniciarSesion_Click(
            object sender, RoutedEventArgs e)
        {
            string usuario = UsuarioTextBox.Text.Trim();

            string contrasena = _contrasenaVisible
                ? ContrasenaVisibleTextBox.Text
                : ContrasenaPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(usuario) ||
                string.IsNullOrEmpty(contrasena))
            {
                MessageBox.Show(
                    "Introduce el usuario y la contraseña.");

                return;
            }

            IniciarSesionButton.IsEnabled = false;

            try
            {
                var servicio =
                    new GenteFit.Services.AutenticacionService();

                var usuarioAutenticado =
                    await servicio.AutenticarAsync(usuario, contrasena);

                if (usuarioAutenticado is null)
                {
                    MessageBox.Show(
                        "Usuario o contraseña incorrectos, o cuenta desactivada.",
                        "GenteFit",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                Window panel = usuarioAutenticado.Tipo switch
                {
                    GenteFit.Models.TipoCuentaUsuario.Administrador =>
                        new PanelPrincipalWindow(usuarioAutenticado),

                    GenteFit.Models.TipoCuentaUsuario.Recepcionista =>
                        new PanelPrincipalWindow(usuarioAutenticado),
                    
                    GenteFit.Models.TipoCuentaUsuario.Monitor =>
                        new PanelMonitorWindow(usuarioAutenticado),

                    GenteFit.Models.TipoCuentaUsuario.Cliente =>
                        new PanelClienteWindow(usuarioAutenticado),

                    _ => throw new UnauthorizedAccessException(
                        "Este tipo de usuario no tiene acceso a la aplicación.")
                };

                GenteFit.WPF.Components.SesionVisual.UsuarioActual = usuarioAutenticado;
                panel.Show();

                Close();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "No se ha podido comprobar el inicio de sesión. Revisa la conexión con la base de datos.",
                    "GenteFit",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IniciarSesionButton.IsEnabled = true;
            }
        }

    }

}
