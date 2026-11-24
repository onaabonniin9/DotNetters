using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.WPF;

public sealed record OpcionSesion(int Id, string Texto);

public partial class EditarSesionAdminWindow : Window
{
    private readonly UsuarioAutenticado _usuarioActual;
    private readonly Sesion? _sesion;
    private bool _guardando;

    public EditarSesionAdminWindow(UsuarioAutenticado usuarioActual, Sesion? sesion = null)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);
        usuarioActual.ExigirAdministrador();
        _usuarioActual = usuarioActual;
        _sesion = sesion;
        InitializeComponent();

        if (_sesion is null)
        {
            FechaDatePicker.SelectedDate = DateTime.Today;
            InicioTextBox.Text = "09:00";
            FinTextBox.Text = "10:00";
            PlazasTextBox.Text = "16";
        }
        else
        {
            Title = "GenteFit - Editar sesión";
            TituloTextBlock.Text = "Editar sesión";
            GuardarButton.Content = "Guardar cambios";
            FechaDatePicker.SelectedDate = _sesion.Date.ToDateTime(TimeOnly.MinValue);
            InicioTextBox.Text = _sesion.HourStart.ToString("HH:mm");
            FinTextBox.Text = _sesion.HourEnd.ToString("HH:mm");
            PlazasTextBox.Text = _sesion.MaxParticipants.ToString(CultureInfo.InvariantCulture);
        }

        Loaded += async (_, _) => await CargarOpcionesAsync();
    }

    private async Task CargarOpcionesAsync()
    {
        GuardarButton.IsEnabled = false;
        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());

            int actividadActualId = _sesion?.Actividad?.Id ?? 0;
            int monitorActualId = _sesion?.Monitor?.Id ?? 0;
            int salaActualId = _sesion?.Sala?.Id ?? 0;

            var actividades = await contexto.Actividades.AsNoTracking()
                .Where(a => a.Active || a.Id == actividadActualId)
                .OrderBy(a => a.Name)
                .Select(a => new OpcionSesion(a.Id, a.Name))
                .ToListAsync();

            var monitores = await contexto.Monitores.AsNoTracking()
                .Where(m => m.Active || m.Id == monitorActualId)
                .OrderBy(m => m.Name).ThenBy(m => m.Surname)
                .Select(m => new OpcionSesion(m.Id, m.Name + " " + m.Surname))
                .ToListAsync();

            var salas = await contexto.Salas.AsNoTracking()
                .Where(s => s.Active || s.Id == salaActualId)
                .OrderBy(s => s.Name)
                .Select(s => new OpcionSesion(s.Id, s.Name + " (" + s.Capacity + " plazas)"))
                .ToListAsync();

            ActividadComboBox.ItemsSource = actividades;
            MonitorComboBox.ItemsSource = monitores;
            SalaComboBox.ItemsSource = salas;

            if (_sesion is null)
            {
                ActividadComboBox.SelectedIndex = actividades.Count > 0 ? 0 : -1;
                MonitorComboBox.SelectedIndex = monitores.Count > 0 ? 0 : -1;
                SalaComboBox.SelectedIndex = salas.Count > 0 ? 0 : -1;
            }
            else
            {
                ActividadComboBox.SelectedValue = actividadActualId;
                MonitorComboBox.SelectedValue = monitorActualId;
                SalaComboBox.SelectedValue = salaActualId;
            }

            if (actividades.Count == 0 || monitores.Count == 0 || salas.Count == 0)
            {
                MessageBox.Show(this,
                    "Para crear una sesión necesitas al menos una actividad activa, un monitor activo y una sala activa.",
                    "Datos necesarios", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                GuardarButton.IsEnabled = true;
            }
        }
        catch (Exception)
        {
            MessageBox.Show(this, "No se han podido cargar actividades, monitores y salas.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (_guardando) return;

        if (ActividadComboBox.SelectedValue is not int actividadId ||
            MonitorComboBox.SelectedValue is not int monitorId ||
            SalaComboBox.SelectedValue is not int salaId ||
            FechaDatePicker.SelectedDate is not DateTime fecha)
        {
            MostrarDatosInvalidos("Selecciona actividad, monitor, sala y fecha.");
            return;
        }

        if (!TimeOnly.TryParseExact(InicioTextBox.Text.Trim(), "HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var inicio) ||
            !TimeOnly.TryParseExact(FinTextBox.Text.Trim(), "HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var fin) ||
            fin <= inicio)
        {
            MostrarDatosInvalidos("Introduce horas válidas (HH:mm). La hora de fin debe ser posterior al inicio.");
            return;
        }

        if (!int.TryParse(PlazasTextBox.Text.Trim(), out int plazas) || plazas <= 0)
        {
            MostrarDatosInvalidos("Las plazas deben ser un número entero mayor que cero.");
            return;
        }

        _guardando = true;
        GuardarButton.IsEnabled = false;
        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());

            var actividad = await contexto.Actividades.FindAsync(actividadId);
            var monitor = await contexto.Monitores.FindAsync(monitorId);
            var sala = await contexto.Salas.FindAsync(salaId);

            if (actividad is null || monitor is null || sala is null)
            {
                MostrarDatosInvalidos("La actividad, el monitor o la sala ya no existen.");
                return;
            }

            if (plazas > sala.Capacity)
            {
                MostrarDatosInvalidos($"La sala «{sala.Name}» admite un máximo de {sala.Capacity} plazas.");
                return;
            }

            if (_sesion is null)
            {
                if (!actividad.Active || !monitor.Active || !sala.Active)
                {
                    MostrarDatosInvalidos("Solo puedes asignar actividades, monitores y salas activos.");
                    return;
                }

                var nuevaSesion = new Sesion
                {
                    Actividad = actividad,
                    Monitor = monitor,
                    Sala = sala,
                    Date = DateOnly.FromDateTime(fecha),
                    HourStart = inicio,
                    HourEnd = fin,
                    MaxParticipants = plazas,
                    Status = EstadoSesion.PROGRAMADA
                };
                var servicio = new SesionService(contexto);
                await servicio.CrearAsync(nuevaSesion, _usuarioActual);
            }
            else
            {
                var sesion = await contexto.Sesiones.FirstOrDefaultAsync(s => s.Id == _sesion.Id);
                if (sesion is null)
                {
                    MostrarDatosInvalidos("La sesión ya no existe.");
                    return;
                }

                sesion.Actividad = actividad;
                sesion.Monitor = monitor;
                sesion.Sala = sala;
                sesion.Date = DateOnly.FromDateTime(fecha);
                sesion.HourStart = inicio;
                sesion.HourEnd = fin;
                sesion.MaxParticipants = plazas;
                await contexto.SaveChangesAsync();
            }

            DialogResult = true;
        }
        catch (Exception)
        {
            MessageBox.Show(this,
                "No se ha podido guardar la sesión. Comprueba la conexión y los datos relacionados.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _guardando = false;
            GuardarButton.IsEnabled = true;
        }
    }

    private void MostrarDatosInvalidos(string mensaje)
    {
        MessageBox.Show(this, mensaje, "Datos no válidos",
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => Close();
}
