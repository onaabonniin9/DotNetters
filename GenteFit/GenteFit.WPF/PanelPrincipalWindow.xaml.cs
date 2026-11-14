using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GenteFit.WPF.Services;

namespace GenteFit.WPF
{
    public partial class PanelPrincipalWindow : Window
    {
        public PanelPrincipalWindow()
        {
            InitializeComponent();
            Loaded += PanelPrincipalWindow_Loaded;
        }

        private async void PanelPrincipalWindow_Loaded(
            object sender, RoutedEventArgs e)
        {
            var servicio = new ConexionDatosService();

            try
            {
                bool conectado =
                    await servicio.ComprobarConexionAsync();

                if (conectado)
                {
                    MessageBox.Show(
                        this,
                        "Conexión con SQL Server correcta.",
                        "GenteFit",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MostrarErrorConexion();
                }
            }
            catch (Exception)
            {
                MostrarErrorConexion();
            }
        }

        private void MostrarErrorConexion()
        {
            MessageBox.Show(
                this,
                "No se ha podido conectar con la base de datos.",
                "GenteFit - Error de conexión",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private async void AbrirSeccion_Click(
            object sender, RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not string seccion)
            {
                return;
            }

            InicioPanel.Visibility = Visibility.Collapsed;
            SeccionPanel.Visibility = Visibility.Collapsed;
            UsuariosPanel.Visibility = Visibility.Collapsed;
            DetalleUsuarioPanel.Visibility = Visibility.Collapsed;

            if (seccion == "Gestión de usuarios")
            {
                UsuariosPanel.Visibility = Visibility.Visible;
                UsuariosDataGrid.ItemsSource = null;

                try
                {
                    var servicio = new ConsultaUsuariosService();
                    var usuarios = await servicio.ConsultarAsync();

                    UsuariosDataGrid.ItemsSource = usuarios;
                    AplicarFiltroUsuarios();
                }
                catch (Exception)
                {
                    MessageBox.Show(
                        this,
                        "No se han podido cargar los usuarios.",
                        "GenteFit",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                return;
            }

            TituloSeccion.Text = seccion;
            SeccionPanel.Visibility = Visibility.Visible;
        }

        private void BuscarUsuarioTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            AplicarFiltroUsuarios();
        }

        private void AplicarFiltroUsuarios()
        {
            // Durante InitializeComponent, el TextBox puede disparar
            // TextChanged antes de que el DataGrid esté inicializado.
            if (BuscarUsuarioTextBox is null || UsuariosDataGrid is null ||
                UsuariosDataGrid.ItemsSource is null)
            {
                return;
            }

            var vista = CollectionViewSource.GetDefaultView(
                UsuariosDataGrid.ItemsSource);

            if (vista is null)
            {
                return;
            }

            string busqueda = BuscarUsuarioTextBox.Text.Trim();

            vista.Filter = item =>
            {
                if (item is not UsuarioResumen usuario)
                {
                    return false;
                }

                return string.IsNullOrEmpty(busqueda)
                    || (usuario.Username?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (usuario.Name?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (usuario.Surname?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (usuario.Email?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false);
            };
        }

        private void VerUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button boton && boton.DataContext is UsuarioResumen usuario)
            {
                DetalleUsuarioPanel.DataContext = usuario;
                ActualizarBotonEstado(usuario);
                DetalleUsuarioPanel.Visibility = Visibility.Visible;
            }
        }

        private void ActualizarBotonEstado(UsuarioResumen usuario)
        {
            CambiarEstadoUsuarioButton.Content = usuario.Active
                ? "Desactivar usuario"
                : "Activar usuario";

            CambiarEstadoUsuarioButton.Background = usuario.Active
                ? new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(163, 32, 32))
                : new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(23, 72, 61));
        }

        private async void CambiarEstadoUsuario_Click(
            object sender, RoutedEventArgs e)
        {
            if (DetalleUsuarioPanel.DataContext is not UsuarioResumen usuario)
            {
                return;
            }

            bool activar = !usuario.Active;
            string accion = activar ? "activar" : "desactivar";

            var confirmacion = MessageBox.Show(
                this,
                $"¿Quieres {accion} al usuario '{usuario.Username}'?",
                "Confirmar cambio de estado",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmacion != MessageBoxResult.Yes)
            {
                return;
            }

            CambiarEstadoUsuarioButton.IsEnabled = false;

            try
            {
                var servicio = new GestionEstadoUsuariosService();
                bool encontrado = await servicio.CambiarEstadoAsync(
                    usuario.Id, activar);

                if (!encontrado)
                {
                    MessageBox.Show(this, "El usuario ya no existe.",
                        "GenteFit", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var consulta = new ConsultaUsuariosService();
                var usuarios = await consulta.ConsultarAsync();
                UsuariosDataGrid.ItemsSource = usuarios;
                AplicarFiltroUsuarios();

                UsuarioResumen? actualizado = null;
                foreach (var item in usuarios)
                {
                    if (item.Id == usuario.Id)
                    {
                        actualizado = item;
                        break;
                    }
                }

                if (actualizado is not null)
                {
                    DetalleUsuarioPanel.DataContext = actualizado;
                    ActualizarBotonEstado(actualizado);
                }
                else
                {
                    DetalleUsuarioPanel.Visibility = Visibility.Collapsed;
                    DetalleUsuarioPanel.DataContext = null;
                }

                MessageBox.Show(this,
                    activar ? "Usuario activado correctamente."
                            : "Usuario desactivado correctamente.",
                    "GenteFit", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                // El servicio impide desactivar al último administrador activo.
                MessageBox.Show(this, ex.Message,
                    "Operación no permitida", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception)
            {
                MessageBox.Show(this,
                    "No se ha podido cambiar el estado del usuario. " +
                    "Comprueba la conexión con SQL Server.",
                    "GenteFit", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                CambiarEstadoUsuarioButton.IsEnabled = true;
            }
        }


        private async void EditarUsuario_Click(
            object sender, RoutedEventArgs e)
        {
            if (DetalleUsuarioPanel.DataContext is not UsuarioResumen usuario)
                return;

            var ventana = new EditarUsuarioWindow(usuario)
            {
                Owner = this
            };

            if (ventana.ShowDialog() != true)
                return;

            try
            {
                var servicio = new ConsultaUsuariosService();
                var usuarios = await servicio.ConsultarAsync();

                UsuariosDataGrid.ItemsSource = usuarios;
                AplicarFiltroUsuarios();

                foreach (var actualizado in usuarios)
                {
                    if (actualizado.Id == usuario.Id)
                    {
                        DetalleUsuarioPanel.DataContext = actualizado;
                        ActualizarBotonEstado(actualizado);
                        break;
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show(
                    this,
                    "Los cambios se han guardado, pero no se ha podido actualizar la tabla.",
                    "GenteFit",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void CerrarDetalleUsuario_Click(object sender, RoutedEventArgs e)
        {
            DetalleUsuarioPanel.Visibility = Visibility.Collapsed;
            DetalleUsuarioPanel.DataContext = null;
        }

        private void VolverInicio_Click(
            object sender, RoutedEventArgs e)
        {
            MostrarInicio();
        }

        private void MenuButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (MenuButton.ContextMenu is not null)
            {
                MenuButton.ContextMenu.PlacementTarget = MenuButton;
                MenuButton.ContextMenu.IsOpen = true;
            }
        }

        private void MenuInicio_Click(
            object sender, RoutedEventArgs e)
        {
            MostrarInicio();
        }

        private void MenuCerrarSesion_Click(
            object sender, RoutedEventArgs e)
        {
            MainWindow login = new MainWindow();
            login.Show();
            Close();
        }

        private void MostrarInicio()
        {
            DetalleUsuarioPanel.Visibility = Visibility.Collapsed;
            UsuariosPanel.Visibility = Visibility.Collapsed;
            SeccionPanel.Visibility = Visibility.Collapsed;
            InicioPanel.Visibility = Visibility.Visible;
        }
    }
}
