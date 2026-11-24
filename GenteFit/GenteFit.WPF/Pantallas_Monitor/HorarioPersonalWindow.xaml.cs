using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;

namespace GenteFit.WPF
{
    public partial class HorarioPersonalWindow : Window
    {
        private static readonly CultureInfo Es = new("es-ES");
        private static readonly Brush ColorOk = new SolidColorBrush(Color.FromRgb(0x1B, 0x8A, 0x4B));
        private static readonly Brush ColorMal = new SolidColorBrush(Color.FromRgb(0x8B, 0x00, 0x00));
        private static readonly Brush ColorNeutro = new SolidColorBrush(Color.FromRgb(0x52, 0x52, 0x52));

        private readonly UsuarioAutenticado _usuarioAutenticado;
        private readonly bool _esMonitor;
        private DateOnly _inicio;

        private int Dias => _esMonitor ? 5 : 1;
        private int Paso => _esMonitor ? 7 : 1;

        public HorarioPersonalWindow(UsuarioAutenticado usuarioAutenticado)
        {
            ArgumentNullException.ThrowIfNull(usuarioAutenticado);

            if (usuarioAutenticado.Tipo != TipoCuentaUsuario.Recepcionista &&
                usuarioAutenticado.Tipo != TipoCuentaUsuario.Administrador &&
                usuarioAutenticado.Tipo != TipoCuentaUsuario.Monitor)
            {
                throw new UnauthorizedAccessException("No tienes permisos para consultar el horario.");
            }

            InitializeComponent();
            _usuarioAutenticado = usuarioAutenticado;
            _esMonitor = usuarioAutenticado.Tipo == TipoCuentaUsuario.Monitor;

            Cabecera.Rol = _esMonitor ? "Monitor" : "Recepción";
            TxtTitulo.Text = _esMonitor ? "Mi horario semanal" : "Horario del día";
            _inicio = InicioPorDefecto();

            Loaded += async (_, _) => await CargarAsync();
        }

        private DateOnly InicioPorDefecto()
        {
            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);
            if (!_esMonitor)
                return hoy;

            int desfase = ((int)hoy.DayOfWeek + 6) % 7; // lunes = 0
            return hoy.AddDays(-desfase);
        }

        private async Task CargarAsync()
        {
            try
            {
                DateOnly fin = _inicio.AddDays(Dias - 1);

                var factory = new GenteFitContextFactory();
                await using var contexto = factory.CreateDbContext(Array.Empty<string>());
                var sesionService = new SesionService(contexto);

                List<Sesion> sesiones = await sesionService.ConsultarPorRangoAsync(
                    _inicio, fin, _usuarioAutenticado);

                var disponibilidad = await sesionService.ConsultarDisponibilidadAsync(
                    sesiones.Select(s => s.Id), _usuarioAutenticado);

                var dias = new List<DiaFila>();
                for (DateOnly d = _inicio; d <= fin; d = d.AddDays(1))
                {
                    List<SesionFila> filas = sesiones
                        .Where(s => s.Date == d)
                        .Select(s => CrearFila(s, disponibilidad))
                        .ToList();

                    string titulo = d.ToString("dddd d 'de' MMMM", Es);
                    titulo = char.ToUpper(titulo[0], Es) + titulo[1..];

                    dias.Add(new DiaFila(titulo, filas,
                        filas.Count == 0 ? "Sin sesiones programadas" : string.Empty));
                }

                ListaDias.ItemsSource = dias;
                TxtRango.Text = _esMonitor
                    ? $"{_inicio.ToString("dd/MM", Es)} – {fin.ToString("dd/MM/yyyy", Es)}"
                    : _inicio.ToString("dd/MM/yyyy", Es);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
            }
            catch (Exception)
            {
                MessageBox.Show("No se ha podido cargar el horario. Revisa la conexión con la base de datos.",
                    "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static SesionFila CrearFila(Sesion s, IReadOnlyDictionary<int, DisponibilidadSesion> disponibilidad)
        {
            string plazas = "-";
            string espera = string.Empty;

            if (disponibilidad.TryGetValue(s.Id, out DisponibilidadSesion? d))
            {
                plazas = $"{d.ReservasConfirmadas}/{d.CapacidadMaxima}";
                espera = $"{d.PersonasEnEspera} en espera";
            }

            (string estado, Brush color) = Describir(s.Status);

            return new SesionFila(
                s.Id,
                $"{s.HourStart:HH\\:mm} - {s.HourEnd:HH\\:mm}",
                s.Actividad.Name,
                $"{s.Sala.Name} · {s.Monitor.Name} {s.Monitor.Surname}",
                plazas, espera, estado, color);
        }

        private static (string Texto, Brush Color) Describir(EstadoSesion estado) => estado switch
        {
            EstadoSesion.PROGRAMADA => ("Programada", ColorOk),
            EstadoSesion.CANCELADA => ("Cancelada", ColorMal),
            EstadoSesion.FINALIZADA => ("Finalizada", ColorNeutro),
            _ => (estado.ToString(), ColorNeutro)
        };

        private async void BtnAnterior_Click(object sender, RoutedEventArgs e)
        {
            _inicio = _inicio.AddDays(-Paso);
            await CargarAsync();
        }

        private async void BtnSiguiente_Click(object sender, RoutedEventArgs e)
        {
            _inicio = _inicio.AddDays(Paso);
            await CargarAsync();
        }

        private async void BtnHoy_Click(object sender, RoutedEventArgs e)
        {
            _inicio = InicioPorDefecto();
            await CargarAsync();
        }

        private void BtnVer_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((Button)sender).Tag;
            var detalle = new DetalleSesionPersonalWindow(_usuarioAutenticado, id) { Owner = this };
            detalle.ShowDialog();
        }

        private sealed record SesionFila(int Id, string Hora, string Actividad, string Lugar,
            string Plazas, string Espera, string Estado, Brush ColorEstado);

        private sealed record DiaFila(string Titulo, List<SesionFila> Sesiones, string Vacio);
    }
}