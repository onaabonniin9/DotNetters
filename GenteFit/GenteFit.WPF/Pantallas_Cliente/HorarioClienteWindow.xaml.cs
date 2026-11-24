
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
    public partial class HorarioClienteWindow : Window
    {
      
        private readonly UsuarioAutenticado _usuarioAutenticado;
        private readonly GenteFitContext _context;

        private readonly SesionService _sesionService;
        private readonly ReservaService _reservaService;
        private readonly ListaEsperaService _listaEsperaService;
        private bool _operacionEnCurso;

        public HorarioClienteWindow(
            UsuarioAutenticado usuarioAutenticado)
        {
            ArgumentNullException.ThrowIfNull(usuarioAutenticado);

            usuarioAutenticado.ExigirCliente();

            InitializeComponent();

            _usuarioAutenticado = usuarioAutenticado;

            var factory = new GenteFitContextFactory();

            _context = factory.CreateDbContext(
                Array.Empty<string>());

            _sesionService = new SesionService(_context);
            _reservaService = new ReservaService(_context);
            _listaEsperaService = new ListaEsperaService(_context);

            Loaded += HorarioClienteWindow_Loaded;
        }

        private async void HorarioClienteWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await CargarSesionesRealesAsync();
        }

        private async Task CargarSesionesRealesAsync()
        {
            try
            {
                TxtEstadoHorario.Text = "Cargando sesiones...";
                TxtEstadoHorario.Visibility = Visibility.Visible;

                var sesionesDb =
                    await _sesionService.ConsultarTodasAsync(
                        _usuarioAutenticado);

                var reservasMias =
                    await _reservaService.ConsultarMiasAsync(
                        _usuarioAutenticado);

                var listasMias =
                    await _listaEsperaService.ConsultarMiasAsync(
                        _usuarioAutenticado);

                var disponibilidadPorSesion =
                    await _sesionService.ConsultarDisponibilidadAsync(
                        sesionesDb.Select(s => s.Id),
                        _usuarioAutenticado);

                var sesionesReservadas = reservasMias
                    .Where(r =>
                        r.Estado == EstadoReserva.CONFIRMADA)
                    .Select(r => r.SesionId)
                    .ToHashSet();

                var sesionesEnEspera = listasMias
                    .Where(l =>
                        l.Estado == EstadoListaEspera.EN_ESPERA)
                    .Select(l => l.SesionId)
                    .ToHashSet();

                var listaPresentacion = sesionesDb.Select(s =>
                {
                    bool tieneReserva =
                        sesionesReservadas.Contains(s.Id);

                    bool estaEnEspera =
                        sesionesEnEspera.Contains(s.Id);

                    string textoAccion;

                    if (tieneReserva)
                    {
                        textoAccion = "Ya reservada";
                    }
                    else if (estaEnEspera)
                    {
                        textoAccion = "En lista de espera";
                    }
                    else
                    {
                        textoAccion = "Reservar / Espera";
                    }

                    string disponibilidadTitulo;
                    string disponibilidadTexto;

                    if (disponibilidadPorSesion.TryGetValue(s.Id, out var disponibilidad))
                    {
                        if (disponibilidad.PlazasLibres > 0)
                        {
                            disponibilidadTitulo = "Plazas libres";
                            disponibilidadTexto =
                                $"{disponibilidad.PlazasLibres} libres";
                        }
                        else
                        {
                            disponibilidadTitulo = "Lista de espera";
                            disponibilidadTexto =
                                $"{disponibilidad.PersonasEnEspera} personas";
                        }
                    }
                    else
                    {
                        disponibilidadTitulo = "Disponibilidad";
                        disponibilidadTexto = "No disponible";
                    }

                    return new
                    {
                        SesionId = s.Id,

                        FechaFormateada =
                            s.Date.ToString("dd/MM/yyyy"),

                        HoraFormateada =
                            s.HourStart.ToString("HH:mm"),

                        NombreActividad =
                            s.Actividad?.Name ?? "Actividad",

                        DescripcionActividad =
                            s.Actividad?.Description ??
                            "Sin descripción disponible.",

                        DisponibilidadTitulo = disponibilidadTitulo,
                        DisponibilidadTexto = disponibilidadTexto,

                        TextoBotonAccion = textoAccion,

                        PuedeSolicitar =
                            !tieneReserva && !estaEnEspera
                    };
                }).ToList();

                IcSesiones.ItemsSource = listaPresentacion;

                TxtSubtituloHorario.Text =
                    $"Clases disponibles · {DateTime.Now:dd/MM/yyyy}";

                if (listaPresentacion.Count == 0)
                {
                    TxtEstadoHorario.Text =
                        "Actualmente no hay sesiones disponibles.";

                    TxtEstadoHorario.Visibility =
                        Visibility.Visible;
                }
                else
                {
                    TxtEstadoHorario.Visibility =
                        Visibility.Collapsed;
                }
            }
            catch (UnauthorizedAccessException)
            {
                TxtEstadoHorario.Text =
                    "Tu cuenta no tiene permisos para consultar el horario.";

                TxtEstadoHorario.Visibility =
                    Visibility.Visible;

                MessageBox.Show(
                    "No tienes permisos para consultar el horario. " +
                    "Comprueba que tu cuenta de cliente sigue activa.",
                    "Acceso no permitido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                TxtEstadoHorario.Text =
                    "No se han podido cargar las sesiones.";

                TxtEstadoHorario.Visibility =
                    Visibility.Visible;

                MessageBox.Show(
                    $"Error al cargar las sesiones: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Logo_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            var panelCliente =
                new PanelClienteWindow(_usuarioAutenticado);

            NavegacionCliente.AbrirDesde(this, panelCliente);
        }

        private void MenuMisReservas_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            var misReservas =
                new MisReservasWindow(_usuarioAutenticado);

            NavegacionCliente.AbrirDesde(this, misReservas);
        }

        private void BtnVerDetalles_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.Tag is not int sesionId)
            {
                return;
            }

            var detalleWindow =
                new DetalleSesionWindow(
                    sesionId,
                    _usuarioAutenticado);

            NavegacionCliente.AbrirDesde(this, detalleWindow);
        }

        private async void BtnAccionSesion_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not int sesionId)
            {
                return;
            }

            if (!boton.IsEnabled || _operacionEnCurso)
                return;

            _operacionEnCurso = true;
            boton.IsEnabled = false;

            try
            {
                await _reservaService.CrearAsync(
                    sesionId,
                    _usuarioAutenticado);

                MessageBox.Show(
                    "¡Reserva realizada con éxito!",
                    "GenteFit",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await CargarSesionesRealesAsync();
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
                            sesionId,
                            _usuarioAutenticado);

                        MessageBox.Show(
                            "Te has añadido correctamente a la lista de espera.",
                            "GenteFit",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarSesionesRealesAsync();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        MessageBox.Show(
                            "Tu cuenta ya no tiene permisos para entrar en la lista de espera.",
                            "Acceso no permitido",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(
                            "No se ha podido completar la inscripción en la lista de espera. Inténtalo de nuevo.",
                            "Lista de espera",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Acceso no permitido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "No se pudo completar la reserva",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
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
