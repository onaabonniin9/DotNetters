
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;

namespace GenteFit.WPF
{
    public partial class DetalleSesionWindow : Window
    {
        private readonly int _sesionId;
        private readonly UsuarioAutenticado _usuarioAutenticado;

        private readonly GenteFitContext _context;
        private readonly SesionService _sesionService;
        private readonly ReservaService _reservaService;
        private readonly ListaEsperaService _listaEsperaService;

        private Sesion? _sesionActual;

        public DetalleSesionWindow(
            int sesionId,
            UsuarioAutenticado usuarioAutenticado)
        {
            ArgumentNullException.ThrowIfNull(usuarioAutenticado);

            if (sesionId <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(sesionId),
                    "El identificador de sesión no es válido.");

            usuarioAutenticado.ExigirCliente();

            InitializeComponent();

            _sesionId = sesionId;
            _usuarioAutenticado = usuarioAutenticado;

            var factory = new GenteFitContextFactory();

            _context = factory.CreateDbContext(
                Array.Empty<string>());

            _sesionService = new SesionService(_context);
            _reservaService = new ReservaService(_context);
            _listaEsperaService = new ListaEsperaService(_context);

            Loaded += DetalleSesionWindow_Loaded;
        }

        private async void DetalleSesionWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await CargarDetallesSesionAsync();
        }

        private async Task CargarDetallesSesionAsync()
        {
            try
            {
                BtnReservarDetalle.IsEnabled = false;
                BtnReservarDetalle.Content = "Cargando...";

                _sesionActual =
                    await _sesionService.ConsultarPorIdAsync(
                        _sesionId,
                        _usuarioAutenticado);

    
                if (_sesionActual is null)
                {
                    MostrarSesionNoDisponible();
                    return;
                }

                CargarDatosVisuales();

                await ActualizarEstadoClienteAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                MostrarErrorCarga(
                    "No tienes permisos para consultar esta sesión.");

                MessageBox.Show(
                    ex.Message,
                    "Acceso no permitido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MostrarErrorCarga(
                    "No se han podido cargar los detalles de la sesión.");

                MessageBox.Show(
                    "No se han podido cargar los detalles de la sesión. Inténtalo de nuevo.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CargarDatosVisuales()
        {
            if (_sesionActual is null)
                return;

            string nombreActividad =
                _sesionActual.Actividad?.Name ?? "Actividad";

            TxtTituloCabecera.Text =
                $"{nombreActividad} · " +
                $"{_sesionActual.Date:dd/MM/yyyy} · " +
                $"{_sesionActual.HourStart:HH:mm}";

            TxtNombreActividad.Text = nombreActividad;

            TxtHoraInicio.Text =
                $"Horario: {_sesionActual.HourStart:HH:mm} - " +
                $"{_sesionActual.HourEnd:HH:mm}";

            TxtDescripcionActividad.Text =
                _sesionActual.Actividad?.Description ??
                "Sin descripción disponible.";

            TxtMonitorNombre.Text =
                _sesionActual.Monitor is not null
                    ? $"{_sesionActual.Monitor.Name} {_sesionActual.Monitor.Surname}"
                    : "Por asignar";

            TxtNivelActividad.Text =
                _sesionActual.Actividad?.Level.ToString() ??
                "No especificado";

            TxtSalaNombre.Text =
                _sesionActual.Sala?.Name ??
                "No especificada";

        }

        private async Task ActualizarEstadoClienteAsync()
        {
            if (_sesionActual is null)
                return;

            var disponibilidadPorSesion =
                await _sesionService.ConsultarDisponibilidadAsync(
                    new[] { _sesionId },
                    _usuarioAutenticado);

            if (disponibilidadPorSesion.TryGetValue(
                    _sesionId, out var disponibilidad))
            {
                TxtPlazasLibres.Text = disponibilidad.PlazasLibres > 0
                    ? $"Plazas libres: {disponibilidad.PlazasLibres} de {disponibilidad.CapacidadMaxima}."
                    : $"Sin plazas disponibles. Lista de espera: {disponibilidad.PersonasEnEspera} personas.";
            }
            else
            {
                TxtPlazasLibres.Text = "Disponibilidad no disponible.";
            }

            var reservasMias =
                await _reservaService.ConsultarMiasAsync(
                    _usuarioAutenticado);

            var listasMias =
                await _listaEsperaService.ConsultarMiasAsync(
                    _usuarioAutenticado);

            bool tieneReserva = reservasMias.Any(r =>
                r.SesionId == _sesionId &&
                r.Estado == EstadoReserva.CONFIRMADA);

            bool estaEnEspera = listasMias.Any(l =>
                l.SesionId == _sesionId &&
                l.Estado == EstadoListaEspera.EN_ESPERA);

            if (tieneReserva)
            {
                BtnReservarDetalle.Content = "Ya tienes una reserva";
                BtnReservarDetalle.IsEnabled = false;

                TxtEstadoReserva.Text =
                    "Tu plaza está reservada para esta sesión.";

                return;
            }

            if (estaEnEspera)
            {
                BtnReservarDetalle.Content = "En lista de espera";
                BtnReservarDetalle.IsEnabled = false;

                TxtEstadoReserva.Text =
                    "Ya estás inscrito/a en la lista de espera de esta sesión.";

                return;
            }

            BtnReservarDetalle.Content = "Reservar plaza";
            BtnReservarDetalle.IsEnabled = true;

            TxtEstadoReserva.Text =
                "Puedes solicitar una plaza para esta sesión.";
        }

        private void MostrarSesionNoDisponible()
        {
            TxtTituloCabecera.Text = "Sesión no disponible";
            TxtNombreActividad.Text = "Sesión no disponible";
            TxtHoraInicio.Text = string.Empty;

            TxtDescripcionActividad.Text =
                "Esta sesión ya no existe, no está programada " +
                "o no se encuentra disponible para tu cuenta.";

            TxtMonitorNombre.Text = "No disponible";
            TxtNivelActividad.Text = "No disponible";
            TxtSalaNombre.Text = "No disponible";
            TxtPlazasLibres.Text = string.Empty;

            TxtEstadoReserva.Text =
                "No es posible realizar una reserva para esta sesión.";

            BtnReservarDetalle.Content = "Sesión no disponible";
            BtnReservarDetalle.IsEnabled = false;

            MessageBox.Show(
                "La sesión no existe o ya no está disponible para reservar.",
                "Sesión no disponible",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void MostrarErrorCarga(string mensaje)
        {
            TxtEstadoReserva.Text = mensaje;
            BtnReservarDetalle.Content = "No disponible";
            BtnReservarDetalle.IsEnabled = false;
        }

        private void Logo_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            var panel =
                new PanelClienteWindow(_usuarioAutenticado);

            NavegacionCliente.AbrirDesde(this, panel);
        }

        private void VolverHorario_Click(
            object sender,
            RoutedEventArgs e)
        {
            var horario =
                new HorarioClienteWindow(_usuarioAutenticado);

            NavegacionCliente.AbrirDesde(this, horario);
        }


        private async void BtnReservarDetalle_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_sesionActual is null ||
                !BtnReservarDetalle.IsEnabled)
            {
                return;
            }

            BtnReservarDetalle.IsEnabled = false;
            BtnReservarDetalle.Content = "Procesando...";

            try
            {
                await _reservaService.CrearAsync(
                    _sesionId,
                    _usuarioAutenticado);

                MessageBox.Show(
                    "¡Reserva realizada con éxito!",
                    "GenteFit",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await ActualizarEstadoTrasOperacionAsync();
            }
            catch (SesionSinPlazasException)
            {
                MessageBoxResult respuesta = MessageBox.Show(
                    "La sesión está completa. ¿Quieres apuntarte a la lista de espera?",
                    "Sesión completa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (respuesta == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _listaEsperaService.CrearAsync(
                            _sesionId,
                            _usuarioAutenticado);

                        MessageBox.Show(
                            "Te has añadido correctamente a la lista de espera.",
                            "GenteFit",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await ActualizarEstadoTrasOperacionAsync();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        MessageBox.Show(
                            "Tu cuenta ya no tiene permisos para entrar en la lista de espera.",
                            "Acceso no permitido",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        await ActualizarEstadoTrasOperacionAsync();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(
                            "No se ha podido completar la inscripción en la lista de espera. Inténtalo de nuevo.",
                            "Lista de espera",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        await ActualizarEstadoTrasOperacionAsync();
                    }
                }
                else
                {
                    BtnReservarDetalle.Content = "Sesión completa";
                    BtnReservarDetalle.IsEnabled = false;

                    TxtEstadoReserva.Text =
                        "La sesión está completa. Puedes consultar otras sesiones en el horario.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "No se pudo completar la reserva",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                await ActualizarEstadoTrasOperacionAsync();
            }
        }

   
        private async Task ActualizarEstadoTrasOperacionAsync()
        {
            try
            {
                await ActualizarEstadoClienteAsync();
            }
            catch (Exception ex)
            {
                BtnReservarDetalle.Content = "Estado no disponible";
                BtnReservarDetalle.IsEnabled = false;

                TxtEstadoReserva.Text =
                    $"No se ha podido actualizar el estado de la reserva: {ex.Message}";
            }
        }

        
        protected override void OnClosed(EventArgs e)
        {
            _context.Dispose();
            base.OnClosed(e);
        }
        private void Cabecera_InicioSolicitado(object sender, EventArgs e)
        {
            var panel = new PanelClienteWindow(_usuarioAutenticado);
            NavegacionCliente.AbrirDesde(this, panel);
        }

        private void Cabecera_CerrarSesionSolicitado(object sender, EventArgs e)
        {
            var inicio = new MainWindow();
            inicio.Show();
            Close();
        }

    }
}

