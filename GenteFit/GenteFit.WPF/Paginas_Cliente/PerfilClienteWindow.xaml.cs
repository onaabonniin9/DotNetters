using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;

namespace GenteFit.WPF
{
    public partial class PerfilClienteWindow : Window
    {
        private readonly UsuarioAutenticado _usuarioAutenticado;
        private readonly GenteFitContext _context;
        private readonly ClienteService _clienteService;
        private readonly PagoService _pagoService;

        private Cliente? _clienteActual;
        private bool _modoEdicion;

        public PerfilClienteWindow(UsuarioAutenticado usuarioAutenticado)
        {
            InitializeComponent();

            _usuarioAutenticado = usuarioAutenticado
                ?? throw new ArgumentNullException(nameof(usuarioAutenticado));

            _usuarioAutenticado.ExigirCliente();

            var factory = new GenteFitContextFactory();
            _context = factory.CreateDbContext(Array.Empty<string>());

            _clienteService = new ClienteService(_context);
            _pagoService = new PagoService(_context);

            Loaded += PerfilClienteWindow_Loaded;
            Closed += PerfilClienteWindow_Closed;
        }

        private async void PerfilClienteWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await CargarPerfilAsync();
        }

        private void PerfilClienteWindow_Closed(
            object? sender,
            EventArgs e)
        {
            _context.Dispose();
        }

        private async Task CargarPerfilAsync()
        {
            try
            {
                _clienteActual =
                    await _clienteService.ConsultarMiPerfilAsync(
                        _usuarioAutenticado);

                if (_clienteActual is null)
                {
                    MessageBox.Show(
                        "No se han encontrado los datos del cliente.",
                        "Perfil",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    CerrarSesion();
                    return;
                }

                MostrarDatosCliente(_clienteActual);

                await CargarPagosAsync();
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Tu cuenta no está activa o no tienes permisos para acceder a este perfil.",
                    "Acceso denegado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                CerrarSesion();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudieron cargar los datos del perfil:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task CargarPagosAsync()
        {
            try
            {
                List<Pago> pagos =
                    await _pagoService.ConsultarPagosDeMiPerfilAsync(
                        _usuarioAutenticado);

                var pagosVisuales = pagos
                    .Select(CrearElementoPago)
                    .ToList();

                ItemsPagos.ItemsSource = pagosVisuales;

                bool hayPagos = pagosVisuales.Count > 0;

                ItemsPagos.Visibility = hayPagos
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                TxtSinPagos.Visibility = hayPagos
                    ? Visibility.Collapsed
                    : Visibility.Visible;

                TxtErrorPagos.Visibility = Visibility.Collapsed;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Tu cuenta no está activa o no tienes permisos para consultar los pagos.",
                    "Acceso denegado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                CerrarSesion();
            }
            catch (Exception)
            {
                ItemsPagos.ItemsSource = null;
                ItemsPagos.Visibility = Visibility.Collapsed;
                TxtSinPagos.Visibility = Visibility.Collapsed;

                TxtErrorPagos.Text =
                    "No se han podido cargar los pagos. Inténtalo de nuevo más tarde.";

                TxtErrorPagos.Visibility = Visibility.Visible;
            }
        }

        private static PagoPerfilItem CrearElementoPago(Pago pago)
        {
            CultureInfo cultura = CultureInfo.GetCultureInfo("es-ES");

            string fechaPagado = pago.Status == EstadoPago.PAGADO
                ? pago.PaymentDate.ToString("dd/MM/yyyy", cultura)
                : "—";

            return new PagoPerfilItem
            {
                ImporteTexto = pago.Amount.ToString("C2", cultura),

                Concepto = pago.Suscripcion?.PlanMensual?.Name
                    ?? "Suscripción",

                Estado = pago.Status.ToString(),

                EstadoColor = ObtenerColorEstado(pago.Status),

                FechaPagadoTexto = fechaPagado
            };
        }

        private static Brush ObtenerColorEstado(EstadoPago estado)
        {
            return estado switch
            {
                EstadoPago.PAGADO =>
                    new SolidColorBrush(Color.FromRgb(23, 72, 61)),

                EstadoPago.PENDIENTE =>
                    new SolidColorBrush(Color.FromRgb(166, 90, 0)),

                EstadoPago.PROCESANDO =>
                    new SolidColorBrush(Color.FromRgb(39, 102, 120)),

                EstadoPago.FALLIDO =>
                    new SolidColorBrush(Color.FromRgb(180, 35, 24)),

                EstadoPago.CANCELADO =>
                    new SolidColorBrush(Color.FromRgb(101, 117, 110)),

                _ => Brushes.Gray
            };
        }

        private void MostrarDatosCliente(Cliente cliente)
        {
            TxtNombre.Text = cliente.Name;
            TxtApellidos.Text = cliente.Surname;
            TxtUsuario.Text = cliente.Username;
            TxtEmail.Text = cliente.Email;
            TxtTelefono.Text = cliente.Phone;
            TxtDNI.Text = cliente.DNI;

            DpFechaNacimiento.SelectedDate = cliente.BirthDate;

            TxtDireccion.Text = cliente.Address;
            TxtCiudad.Text = cliente.City;
            TxtCodigoPostal.Text = cliente.PostalCode;
            TxtIban.Text = cliente.IBAN;
        }

        private void EditarPerfil_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_clienteActual is null || _modoEdicion)
                return;

            _modoEdicion = true;

            CambiarEstadoCampos(true);

            BtnEditarPerfil.Visibility = Visibility.Collapsed;
            BtnGuardarPerfil.Visibility = Visibility.Visible;
            BtnCancelarEdicion.Visibility = Visibility.Visible;

            TxtNombre.Focus();
        }

        private async void GuardarPerfil_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!_modoEdicion || _clienteActual is null)
                return;

            string nombre = TxtNombre.Text.Trim();
            string apellidos = TxtApellidos.Text.Trim();
            string usuario = TxtUsuario.Text.Trim();
            string email = TxtEmail.Text.Trim();
            string telefono = TxtTelefono.Text.Trim();
            string dni = TxtDNI.Text.Trim();

            string direccion = TxtDireccion.Text.Trim();
            string ciudad = TxtCiudad.Text.Trim();
            string codigoPostal = TxtCodigoPostal.Text.Trim();
            string iban = TxtIban.Text.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MostrarValidacion("El nombre es obligatorio.");
                TxtNombre.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(apellidos))
            {
                MostrarValidacion("Los apellidos son obligatorios.");
                TxtApellidos.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(dni))
            {
                MostrarValidacion("El DNI es obligatorio.");
                TxtDNI.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(usuario))
            {
                MostrarValidacion("El nombre de usuario es obligatorio para iniciar sesión.");
                TxtUsuario.Focus();
                return;
            }

            if (DpFechaNacimiento.SelectedDate is not DateTime fechaNacimiento)
            {
                MostrarValidacion("Debes seleccionar una fecha de nacimiento.");
                DpFechaNacimiento.Focus();
                return;
            }

            if (fechaNacimiento.Date > DateTime.Today)
            {
                MostrarValidacion("La fecha de nacimiento no puede ser futura.");
                DpFechaNacimiento.Focus();
                return;
            }

            try
            {
                bool actualizado = await _clienteService.ActualizarMiPerfilAsync(
                    _usuarioAutenticado,
                    nombre,
                    apellidos,
                    usuario,
                    email,
                    telefono,
                    dni,
                    fechaNacimiento,
                    direccion,
                    ciudad,
                    codigoPostal,
                    iban);

                if (!actualizado)
                {
                    MessageBox.Show(
                        "No se ha podido actualizar el perfil. Comprueba que el cliente sigue existiendo.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                _clienteActual = await _clienteService.ConsultarMiPerfilAsync(
                    _usuarioAutenticado);

                if (_clienteActual is not null)
                    MostrarDatosCliente(_clienteActual);

                FinalizarEdicion();

                MessageBox.Show(
                    "Tus datos se han actualizado correctamente.",
                    "GenteFit",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Tu cuenta ya no tiene permisos para modificar estos datos.",
                    "Acceso denegado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                CerrarSesion();
            }
            catch (ArgumentException ex)
            {
                MostrarValidacion(ex.Message);
                TxtUsuario.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se han podido guardar los cambios:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelarEdicion_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_clienteActual is not null)
                MostrarDatosCliente(_clienteActual);

            FinalizarEdicion();
        }

        private void CambiarEstadoCampos(bool editable)
        {
            TxtNombre.IsReadOnly = !editable;
            TxtApellidos.IsReadOnly = !editable;
            TxtUsuario.IsReadOnly = !editable;
            TxtEmail.IsReadOnly = !editable;
            TxtTelefono.IsReadOnly = !editable;
            TxtDNI.IsReadOnly = !editable;

            TxtDireccion.IsReadOnly = !editable;
            TxtCiudad.IsReadOnly = !editable;
            TxtCodigoPostal.IsReadOnly = !editable;
            TxtIban.IsReadOnly = !editable;

            DpFechaNacimiento.IsEnabled = editable;

            Brush fondo = editable
                ? Brushes.White
                : new BrushConverter().ConvertFromString("#EDF3F0") as Brush
                    ?? Brushes.Transparent;

            TxtNombre.Background = fondo;
            TxtApellidos.Background = fondo;
            TxtUsuario.Background = fondo;
            TxtEmail.Background = fondo;
            TxtTelefono.Background = fondo;
            TxtDNI.Background = fondo;

            TxtDireccion.Background = fondo;
            TxtCiudad.Background = fondo;
            TxtCodigoPostal.Background = fondo;
            TxtIban.Background = fondo;
        }

        private void FinalizarEdicion()
        {
            _modoEdicion = false;

            CambiarEstadoCampos(false);

            BtnEditarPerfil.Visibility = Visibility.Visible;
            BtnGuardarPerfil.Visibility = Visibility.Collapsed;
            BtnCancelarEdicion.Visibility = Visibility.Collapsed;
        }

        private static void MostrarValidacion(string mensaje)
        {
            MessageBox.Show(
                mensaje,
                "Revisar datos",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void Logo_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            VolverAlPanelCliente();
        }

        private void MenuButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            VolverAlPanelCliente();
        }

        private void VolverAlPanelCliente()
        {
            var panelCliente =
                new PanelClienteWindow(_usuarioAutenticado);

            panelCliente.Show();
            Close();
        }

        private void CerrarSesion_Click(
            object sender,
            RoutedEventArgs e)
        {
            CerrarSesion();
        }

        private void CerrarSesion()
        {
            var mainWindow = new MainWindow();

            mainWindow.Show();
            Close();
        }

        private sealed class PagoPerfilItem
        {
            public string ImporteTexto { get; set; } = string.Empty;

            public string Concepto { get; set; } = string.Empty;

            public string Estado { get; set; } = string.Empty;

            public Brush EstadoColor { get; set; } = Brushes.Gray;

            public string FechaPagadoTexto { get; set; } = string.Empty;
        }
    }
}