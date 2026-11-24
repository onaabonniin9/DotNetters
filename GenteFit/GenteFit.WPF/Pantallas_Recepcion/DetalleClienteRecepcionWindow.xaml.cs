using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;
using GenteFit.WPF.Services;

namespace GenteFit.WPF
{
    public partial class DetalleClienteRecepcionWindow : Window
    {
        private static readonly CultureInfo Es = new ("es-ES");

        private static readonly Brush ColorOk = new SolidColorBrush(Color.FromRgb(0x1B, 0x8A, 0x4B));

        private static readonly Brush ColorMal = new SolidColorBrush(Color.FromRgb(0x8B, 0x00, 0x00));

        private static readonly Brush ColorNeutro = new SolidColorBrush(Color.FromRgb(0x52, 0x52, 0x52));

        private readonly UsuarioAutenticado _usuarioAutenticado;
        private readonly int _clienteId;
        private DetalleClienteRecepcion? _detalleActual;
        private bool _editando;
        private bool _guardando;

        public DetalleClienteRecepcionWindow(UsuarioAutenticado usuarioAutenticado, int clienteId)
        {
            ArgumentNullException.ThrowIfNull(usuarioAutenticado);

            if (usuarioAutenticado.Tipo != TipoCuentaUsuario.Recepcionista &&
                usuarioAutenticado.Tipo != TipoCuentaUsuario.Administrador)
            {
                throw new UnauthorizedAccessException(
                    "No tienes permisos para consultar los clientes.");
            }

            InitializeComponent();
            _usuarioAutenticado = usuarioAutenticado;
            _clienteId = clienteId;

            Loaded += async (_, _) => await CargarAsync();
            Closing += (_, e) => { if (_guardando) e.Cancel = true; };
        }

        private async Task CargarAsync()
        {
            try
            {
                var factory = new GenteFitContextFactory();
                await using var contexto = factory.CreateDbContext(Array.Empty<string>());
                var clienteService = new ClienteService(contexto);

                DetalleClienteRecepcion? detalle = await clienteService.ConsultarDetalleParaRecepcionAsync(
                    _usuarioAutenticado, _clienteId);

                if (detalle is null)
                {
                    MessageBox.Show("El cliente ya no existe", "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);
                    VolverAlListado();
                    return;
                }
                _detalleActual = detalle;
                Mostrar(detalle);
                BtnEditar.IsEnabled = true;
                BtnBaja.IsEnabled = true;
                
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                NavegacionCliente.CerrarSesion(this);
            }
            catch (Exception)
            {
                MessageBox.Show("No se ha podido cargar el cliente. Revisa la conexión con la base de datos.", "GenteFit",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Mostrar(DetalleClienteRecepcion detalle)
        {
            Cliente c = detalle.Cliente;

            TxtNombre.Text = c.Name;
            TxtApellidos.Text = c.Surname;
            TxtEmail.Text = c.Email;
            TxtUsuario.Text = c.Username;
            TxtTelefono.Text = c.Phone;
            TxtDireccion.Text = c.Address;
            TxtCiudad.Text = c.City;
            TxtCodigoPostal.Text = c.PostalCode;
            TxtNacimiento.Text = c.BirthDate.ToString("dd/MM/yyyy", Es);
            TxtDni.Text = c.DNI;
            TxtPlan.Text = detalle.PlanMensual ?? "Sin plan activo";
            TxtIban.Text = EnmascararIban(c.IBAN);

            ListaPagos.ItemsSource = detalle.Pagos.Select(p =>
            {
                (string texto, Brush color) = Describir(p.Status);

                return new PagoFila(p.Amount.ToString("C", Es), $"Mensualidad {p.PaymentDate.ToString("MMMM", Es)}",
                texto, color, p.PaymentDate.ToString("dd/MM/yyyy", Es));
            })
            .ToList();
        TxtSinPagos.Visibility = detalle.Pagos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        }

        private static (string Texto, Brush Color) Describir(EstadoPago estado) => estado switch
        {
            EstadoPago.PAGADO => ("Pagado", ColorOk),
            EstadoPago.PENDIENTE => ("Pendiente", ColorMal),
            EstadoPago.FALLIDO => ("Fallido", ColorMal),
            EstadoPago.PROCESANDO => ("Procesando", ColorNeutro),
            _ => (estado.ToString(), ColorNeutro)
        };

        private static string EnmascararIban(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban) || iban.Length <= 4)
            return iban;

            return "•••• •••• •••• " + iban[^4..];
        }

        private void CambiarEdicion(bool editar)
        {
            _editando = editar;
            foreach (TextBox campo in new[] { TxtNombre, TxtApellidos, TxtEmail, TxtUsuario, TxtTelefono, TxtDireccion, TxtCiudad, TxtCodigoPostal, TxtNacimiento, TxtDni })
                campo.IsReadOnly = !editar;

            BtnEditar.Visibility = editar ? Visibility.Collapsed : Visibility.Visible;
            BtnGuardar.Visibility = editar ? Visibility.Visible : Visibility.Collapsed;
            BtnCancelar.Visibility = editar ? Visibility.Visible : Visibility.Collapsed;
            BtnBaja.IsEnabled = !editar && _detalleActual is not null;
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (_detalleActual is null || _guardando) return;
            CambiarEdicion(true);
            TxtNombre.Focus();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (_guardando) return;
            if (_detalleActual is not null) Mostrar(_detalleActual);
            CambiarEdicion(false);
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!_editando || _guardando || _detalleActual is null) return;

            if (!DateTime.TryParseExact(TxtNacimiento.Text.Trim(), "dd/MM/yyyy", Es,
                    DateTimeStyles.None, out DateTime nacimiento) || nacimiento.Date > DateTime.Today)
            {
                MessageBox.Show("Introduce una fecha de nacimiento válida (dd/mm/aaaa), no futura.",
                    "Revisar datos", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNacimiento.Focus();
                return;
            }

            _guardando = true;
            BtnGuardar.IsEnabled = false;
            BtnCancelar.IsEnabled = false;
            foreach (TextBox campo in new[] { TxtNombre, TxtApellidos, TxtEmail, TxtUsuario, TxtTelefono, TxtDireccion, TxtCiudad, TxtCodigoPostal, TxtNacimiento, TxtDni })
                campo.IsReadOnly = true;
            try
            {
                var factory = new GenteFitContextFactory();
                await using var contexto = factory.CreateDbContext(Array.Empty<string>());
                var clienteService = new ClienteService(contexto);

                // Revalidar los permisos de recepción antes de actualizar.
                var detalle = await clienteService.ConsultarDetalleParaRecepcionAsync(
                    _usuarioAutenticado, _clienteId);
                if (detalle is null)
                {
                    MessageBox.Show("El cliente ya no existe.", "GenteFit",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool actualizado = await clienteService.ActualizarAsync(
                    _clienteId, TxtNombre.Text.Trim(), TxtApellidos.Text.Trim(),
                    TxtUsuario.Text.Trim(), TxtEmail.Text.Trim(), TxtTelefono.Text.Trim(),
                    TxtDni.Text.Trim(), nacimiento, TxtDireccion.Text.Trim(),
                    TxtCiudad.Text.Trim(), TxtCodigoPostal.Text.Trim(), detalle.Cliente.IBAN);

                if (!actualizado)
                {
                    MessageBox.Show("No se ha podido actualizar el cliente.", "GenteFit",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CambiarEdicion(false);
                _detalleActual = null;
                BtnEditar.IsEnabled = false;
                BtnBaja.IsEnabled = false;
                await CargarAsync();
                MessageBox.Show("Cliente actualizado correctamente.", "GenteFit",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Revisar datos", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception)
            {
                MessageBox.Show("No se han podido guardar los cambios. Revisa los datos y la conexión.",
                    "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _guardando = false;
                BtnGuardar.IsEnabled = true;
                BtnCancelar.IsEnabled = true;
                if (_editando) CambiarEdicion(true);
            }
        }

        private void VolverAlListado()
        {
            if (!_guardando)
                NavegacionCliente.AbrirDesde(this, new ClientesRecepcionWindow(_usuarioAutenticado));
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            VolverAlListado();
        }

        private async void BtnBaja_Click(object sender, RoutedEventArgs e)
        {

            MessageBoxResult respuesta = MessageBox.Show(
            "¿Seguro que quieres dar de baja a este cliente? No podrá iniciar sesión, pero se conservará su historial.",
            "Baja de cliente", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (respuesta != MessageBoxResult.Yes)
            return;

            try
            {
                var factory = new GenteFitContextFactory();
                await using var contexto = factory.CreateDbContext(Array.Empty<string>());
                var clienteService = new ClienteService(contexto);

                bool baja = await clienteService.DarDeBajaParaRecepcionAsync(_usuarioAutenticado, _clienteId);

                MessageBox.Show(
                    baja
                        ? "El cliente se ha dado de baja correctamente."
                        : "El cliente no existe o ya estaba de baja.",
                    "GenteFit",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                VolverAlListado();
            }
            catch(UnauthorizedAccessException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Acceso denegado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "No se ha podido dar de baja al cliente. Revisa la conexión con la base de datos.",
                    "GenteFit",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private sealed record PagoFila(string Importe, string Concepto, string Estado, Brush ColorEstado, string Fecha);
    }
}