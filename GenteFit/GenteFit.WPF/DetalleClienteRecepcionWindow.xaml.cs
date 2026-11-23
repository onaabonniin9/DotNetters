using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
                    Close();
                    return;
                }
                Mostrar(detalle);
                
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
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

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed record PagoFila(string Importe, string Concepto, string Estado, Brush ColorEstado, string Fecha);
    }
}