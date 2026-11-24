using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;

namespace GenteFit.WPF
{
    public partial class DetalleSesionPersonalWindow : Window
    {
        private static readonly CultureInfo Es = new("es-ES");

        private readonly UsuarioAutenticado _usuarioAutenticado;
        private readonly int _sesionId;
        private readonly bool _esMonitor;

        public DetalleSesionPersonalWindow(UsuarioAutenticado usuarioAutenticado, int sesionId)
        {
            ArgumentNullException.ThrowIfNull(usuarioAutenticado);

            if (usuarioAutenticado.Tipo != TipoCuentaUsuario.Recepcionista &&
                usuarioAutenticado.Tipo != TipoCuentaUsuario.Administrador &&
                usuarioAutenticado.Tipo != TipoCuentaUsuario.Monitor)
            {
                throw new UnauthorizedAccessException("No tienes permisos para consultar sesiones.");
            }

            InitializeComponent();
            _usuarioAutenticado = usuarioAutenticado;
            _sesionId = sesionId;
            _esMonitor = usuarioAutenticado.Tipo == TipoCuentaUsuario.Monitor;

            Cabecera.Rol = _esMonitor ? "Monitor" : "Recepción";
            PanelReservas.Visibility = _esMonitor ? Visibility.Visible : Visibility.Collapsed;

            Loaded += async (_, _) => await CargarAsync();
        }

        private async Task CargarAsync()
        {
            try
            {
                var factory = new GenteFitContextFactory();
                await using var contexto = factory.CreateDbContext(Array.Empty<string>());
                var sesionService = new SesionService(contexto);

                Sesion? s = await sesionService.ConsultarPorIdAsync(_sesionId, _usuarioAutenticado);
                if (s is null)
                {
                    MessageBox.Show("La sesión ya no existe.", "GenteFit",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                TxtActividad.Text = s.Actividad.Name;
                TxtDescripcion.Text = s.Actividad.Description;
                TxtFecha.Text = $"{s.Date.ToString("dd/MM/yyyy", Es)} · {s.HourStart:HH\\:mm} - {s.HourEnd:HH\\:mm}";
                TxtMonitor.Text = $"{s.Monitor.Name} {s.Monitor.Surname}";
                TxtNivel.Text = s.Actividad.Level.ToString();
                TxtSala.Text = s.Sala.Name;
                TxtEstado.Text = s.Status.ToString();

                var disponibilidad = await sesionService.ConsultarDisponibilidadAsync(
                    new[] { s.Id }, _usuarioAutenticado);

                if (disponibilidad.TryGetValue(s.Id, out DisponibilidadSesion? d))
                    TxtPlazas.Text = $"{d.ReservasConfirmadas}/{d.CapacidadMaxima} ocupadas · {d.PersonasEnEspera} en lista de espera";
                else
                    TxtPlazas.Text = "-";

                if (_esMonitor)
                {
                    var reservas = await sesionService.ConsultarReservasConfirmadasAsync(
                        s.Id, _usuarioAutenticado);

                    ListaReservas.ItemsSource = reservas
                        .Select(r => new ReservaFila(
                            $"{r.Cliente.Name} {r.Cliente.Surname}",
                            r.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Es)))
                        .ToList();

                    TxtSinReservas.Visibility = reservas.Count == 0
                        ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
            }
            catch (Exception)
            {
                MessageBox.Show("No se ha podido cargar la sesión. Revisa la conexión con la base de datos.",
                    "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed record ReservaFila(string Cliente, string Fecha);
    }
}