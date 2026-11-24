
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;

namespace GenteFit.WPF
{
    public partial class MisReservasWindow : Window
    {
        private readonly UsuarioAutenticado _usuarioAutenticado;

        private readonly GenteFitContext _context;
        private readonly ReservaService _reservaService;
        private readonly ListaEsperaService _listaEsperaService;
        private bool _operacionEnCurso;

        public MisReservasWindow(
            UsuarioAutenticado usuarioAutenticado)
        {
            ArgumentNullException.ThrowIfNull(usuarioAutenticado);

            usuarioAutenticado.ExigirCliente();

            InitializeComponent();

            _usuarioAutenticado = usuarioAutenticado;

            var factory = new GenteFitContextFactory();

            _context = factory.CreateDbContext(
                Array.Empty<string>());

            _reservaService = new ReservaService(_context);
            _listaEsperaService = new ListaEsperaService(_context);

            Loaded += MisReservasWindow_Loaded;
        }

        private async void MisReservasWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await CargarDatosClienteAsync();
        }

        private async Task CargarDatosClienteAsync()
        {
            try
            {
                TxtSinReservas.Text = "Cargando reservas...";
                TxtSinListaEspera.Text = "Cargando lista de espera...";

                TxtSinReservas.Visibility = Visibility.Visible;
                TxtSinListaEspera.Visibility = Visibility.Visible;

                var reservasMias =
                    await _reservaService.ConsultarMiasAsync(
                        _usuarioAutenticado);

                var listasMias =
                    await _listaEsperaService.ConsultarMiasAsync(
                        _usuarioAutenticado);

                var reservasPresentacion = reservasMias
                    .Where(r => r.Estado == EstadoReserva.CONFIRMADA)
                    .Select(r => new
                    {
                        ReservaId = r.Id,

                        HoraFormateada =
                            r.HoraInicio.ToString("HH:mm"),

                        ActividadNombre = r.ActividadNombre,

                        FechaTexto =
                            $"Fecha: {r.FechaSesion:dd/MM/yyyy}",

                        EstadoTexto =
                            $"Estado: {ObtenerTextoEstadoReserva(r.Estado)}" +
                            $" · Sesión: {ObtenerTextoEstadoSesion(r.EstadoSesion)}",

                        PuedeCancelar =
                            r.Estado == EstadoReserva.CONFIRMADA,

                        TextoBotonCancelar =
                            r.Estado == EstadoReserva.CONFIRMADA
                                ? "Cancelar reserva"
                                : "No cancelable"
                    })
                    .ToList();

                var listasPresentacion = listasMias
                    .Select(l => new
                    {
                        ListaId = l.Id,

                        HoraFormateada =
                            l.HoraInicio.ToString("HH:mm"),

                        ActividadNombre = l.ActividadNombre,

                        FechaTexto =
                            $"Fecha: {l.FechaSesion:dd/MM/yyyy}",

                        PosicionTexto =
                            l.Estado == EstadoListaEspera.EN_ESPERA
                                ? $"Posición en la lista: {l.Posicion}"
                                : "La entrada ya no está pendiente.",

                        EstadoTexto =
                            $"Estado: {ObtenerTextoEstadoLista(l.Estado)}" +
                            $" · Sesión: {ObtenerTextoEstadoSesion(l.EstadoSesion)}",

                        PuedeAbandonar =
                            l.Estado == EstadoListaEspera.EN_ESPERA,

                        TextoBotonAbandonar =
                            l.Estado == EstadoListaEspera.EN_ESPERA
                                ? "Salir de la lista"
                                : "No disponible"
                    })
                    .ToList();

                IcReservas.ItemsSource = reservasPresentacion;
                IcListaEspera.ItemsSource = listasPresentacion;

                if (reservasPresentacion.Count == 0)
                {
                    TxtSinReservas.Text =
                        "Todavía no tienes reservas.";

                    TxtSinReservas.Visibility =
                        Visibility.Visible;
                }
                else
                {
                    TxtSinReservas.Visibility =
                        Visibility.Collapsed;
                }

                if (listasPresentacion.Count == 0)
                {
                    TxtSinListaEspera.Text =
                        "No tienes entradas en lista de espera.";

                    TxtSinListaEspera.Visibility =
                        Visibility.Visible;
                }
                else
                {
                    TxtSinListaEspera.Visibility =
                        Visibility.Collapsed;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                TxtSinReservas.Text =
                    "No se pueden consultar tus reservas.";

                TxtSinListaEspera.Text =
                    "No se puede consultar tu lista de espera.";

                TxtSinReservas.Visibility = Visibility.Visible;
                TxtSinListaEspera.Visibility = Visibility.Visible;

                MessageBox.Show(
                    ex.Message,
                    "Acceso no permitido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                TxtSinReservas.Text =
                    "No se han podido cargar tus reservas.";

                TxtSinListaEspera.Text =
                    "No se ha podido cargar tu lista de espera.";

                TxtSinReservas.Visibility = Visibility.Visible;
                TxtSinListaEspera.Visibility = Visibility.Visible;

                MessageBox.Show(
                    $"Error al cargar tus datos: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string ObtenerTextoEstadoReserva(
            EstadoReserva estado)
        {
            return estado switch
            {
                EstadoReserva.CONFIRMADA =>
                    "Confirmada",

                EstadoReserva.CANCELADA =>
                    "Cancelada por el cliente",

                EstadoReserva.CANCELADA_POR_CENTRO =>
                    "Cancelada por el centro",

                _ => estado.ToString()
            };
        }

        private static string ObtenerTextoEstadoLista(
            EstadoListaEspera estado)
        {
            return estado switch
            {
                EstadoListaEspera.EN_ESPERA =>
                    "En espera",

                EstadoListaEspera.PROMOCIONADO =>
                    "Promocionada a reserva",

                EstadoListaEspera.RETIRADO =>
                    "Retirada",

                _ => estado.ToString()
            };
        }

        private static string ObtenerTextoEstadoSesion(
            EstadoSesion estado)
        {
            return estado switch
            {
                EstadoSesion.PROGRAMADA =>
                    "Programada",

                EstadoSesion.CANCELADA =>
                    "Cancelada",

                EstadoSesion.FINALIZADA =>
                    "Finalizada",

                _ => estado.ToString()
            };
        }

        private void Logo_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            var panel =
                new PanelClienteWindow(_usuarioAutenticado);

            NavegacionCliente.AbrirDesde(this, panel);
        }

        private void VerHorario_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            var horario =
                new HorarioClienteWindow(_usuarioAutenticado);

            NavegacionCliente.AbrirDesde(this, horario);
        }

        private async void BtnCancelarReserva_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not int reservaId ||
                !boton.IsEnabled ||
                _operacionEnCurso)
            {
                return;
            }

            _operacionEnCurso = true;
            boton.IsEnabled = false;

            try
            {
                MessageBoxResult respuesta = MessageBox.Show(
                    "¿Seguro que quieres cancelar esta reserva?",
                    "Confirmar cancelación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (respuesta != MessageBoxResult.Yes)
                    return;

                bool cancelada = await _reservaService.CancelarAsync(
                    reservaId, _usuarioAutenticado);

                MessageBox.Show(
                    cancelada
                        ? "Reserva cancelada correctamente."
                        : "No se ha podido cancelar la reserva. Puede que ya no esté confirmada.",
                    cancelada ? "GenteFit" : "Reserva no modificada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await CargarDatosClienteAsync();
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Tu cuenta no tiene permisos para cancelar esta reserva.",
                    "Acceso no permitido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                await CargarDatosClienteAsync();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "No se ha podido cancelar la reserva. Inténtalo de nuevo.",
                    "Error al cancelar la reserva",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                await CargarDatosClienteAsync();
            }
            finally
            {
                _operacionEnCurso = false;
                boton.IsEnabled = true;
            }
        }

        private async void BtnSalirListaEspera_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not int listaId ||
                !boton.IsEnabled ||
                _operacionEnCurso)
            {
                return;
            }

            _operacionEnCurso = true;
            boton.IsEnabled = false;

            try
            {
                MessageBoxResult respuesta = MessageBox.Show(
                    "¿Seguro que quieres salir de esta lista de espera?",
                    "Confirmar salida",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (respuesta != MessageBoxResult.Yes)
                    return;

                bool abandonada = await _listaEsperaService.AbandonarAsync(
                    listaId, _usuarioAutenticado);

                MessageBox.Show(
                    abandonada
                        ? "Has salido de la lista de espera."
                        : "No se ha podido abandonar la lista. Puede que la entrada ya no esté pendiente.",
                    abandonada ? "GenteFit" : "Lista de espera",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await CargarDatosClienteAsync();
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Tu cuenta no tiene permisos para modificar esta lista de espera.",
                    "Acceso no permitido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                await CargarDatosClienteAsync();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "No se ha podido abandonar la lista de espera. Inténtalo de nuevo.",
                    "Error al salir de la lista de espera",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                await CargarDatosClienteAsync();
            }
            finally
            {
                _operacionEnCurso = false;
                boton.IsEnabled = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _context.Dispose();
            base.OnClosed(e);
        }
        private void Volver_Click(object sender, RoutedEventArgs e)
        {
            Cabecera_InicioSolicitado(sender, EventArgs.Empty);
        }

        private void Cabecera_InicioSolicitado(object sender, EventArgs e)
        {
            var panel = new PanelClienteWindow(_usuarioAutenticado);
            NavegacionCliente.AbrirDesde(this, panel);
        }

        private void Cabecera_CerrarSesionSolicitado(object sender, EventArgs e)
        {
            NavegacionCliente.CerrarSesion(this);
        }

    }
}
