using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.WPF;

public sealed record ActividadResumen(
    int Id, string Nombre, string Descripcion, string Intensidad, int Duracion, string Estado);

public sealed record SesionResumen(
    int Id, string Actividad, string Fecha, string Inicio, string Fin,
    string Sala, string Monitor, int Plazas, string Estado);

public partial class ActividadesSesionesAdminWindow : UserControl
{
    public event EventHandler? SolicitarInicio;


    private readonly UsuarioAutenticado _usuarioActual;
    private bool _cargando;
    private List<ActividadResumen> _actividades = new();
    private List<SesionResumen> _sesiones = new();

    public ActividadesSesionesAdminWindow(UsuarioAutenticado usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        usuario.ExigirAdministrador();
        _usuarioActual = usuario;

        InitializeComponent();
        Loaded += async (_, _) => await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        if (_cargando) return;
        _cargando = true;
        EstadoTextBlock.Text = "Cargando datos...";

        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());

            var actividades = await contexto.Actividades
                .AsNoTracking()
                .OrderBy(a => a.Name)
                .ToListAsync();

            var sesiones = await contexto.Sesiones
                .AsNoTracking()
                .Include(s => s.Actividad)
                .Include(s => s.Sala)
                .Include(s => s.Monitor)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.HourStart)
                .ToListAsync();

            _actividades = actividades.Select(a => new ActividadResumen(
                a.Id, a.Name, a.Description, a.Level.ToString(), a.Duration,
                a.Active ? "Activa" : "Inactiva")).ToList();

            _sesiones = sesiones.Select(s => new SesionResumen(
                s.Id,
                s.Actividad?.Name ?? "Sin actividad",
                s.Date.ToString("dd/MM/yyyy"),
                s.HourStart.ToString("HH:mm"),
                s.HourEnd.ToString("HH:mm"),
                s.Sala?.Name ?? "Sin sala",
                s.Monitor is null ? "Sin monitor" : $"{s.Monitor.Name} {s.Monitor.Surname}",
                s.MaxParticipants,
                s.Status.ToString()
            )).ToList();

            AplicarFiltro();
        }
        catch (Exception)
        {
            EstadoTextBlock.Text = "No se han podido cargar los datos.";
            MessageBox.Show(Window.GetWindow(this)!,
                "No se han podido consultar las actividades y sesiones. Comprueba SQL Server.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _cargando = false;
        }
    }

    private void BuscarTextBox_TextChanged(object sender, TextChangedEventArgs e)
        => AplicarFiltro();

    private void AplicarFiltro()
    {
        if (BuscarTextBox is null || ActividadesDataGrid is null || SesionesDataGrid is null)
            return;

        string texto = BuscarTextBox.Text.Trim();
        bool Coincide(params string[] campos) =>
            texto.Length == 0 || campos.Any(c =>
                c.Contains(texto, StringComparison.OrdinalIgnoreCase));

        var actividades = _actividades.Where(a => Coincide(
            a.Id.ToString(), a.Nombre, a.Descripcion, a.Intensidad,
            a.Duracion.ToString(), a.Estado)).ToList();
        var sesiones = _sesiones.Where(s => Coincide(
            s.Id.ToString(), s.Actividad, s.Fecha, s.Inicio, s.Fin,
            s.Sala, s.Monitor, s.Plazas.ToString(), s.Estado)).ToList();

        ActividadesDataGrid.ItemsSource = actividades;
        SesionesDataGrid.ItemsSource = sesiones;
        EstadoTextBlock.Text = $"{actividades.Count} actividades · {sesiones.Count} sesiones";
    }

    private async void NuevaActividad_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new EditarActividadAdminWindow { Owner = Window.GetWindow(this) };
        if (ventana.ShowDialog() == true)
            await CargarDatosAsync();
    }

    private async void EditarActividad_Click(object sender, RoutedEventArgs e)
    {
        if (ActividadesDataGrid.SelectedItem is not ActividadResumen seleccionada)
        {
            MessageBox.Show(Window.GetWindow(this)!, "Selecciona primero una actividad.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            var servicio = new ActividadService(contexto);
            var actividad = await servicio.ConsultarPorIdAsync(seleccionada.Id);
            if (actividad is null)
            {
                MessageBox.Show(Window.GetWindow(this)!, "La actividad ya no existe.",
                    "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);
                await CargarDatosAsync();
                return;
            }

            var ventana = new EditarActividadAdminWindow(actividad) { Owner = Window.GetWindow(this) };
            if (ventana.ShowDialog() == true)
                await CargarDatosAsync();
        }
        catch (Exception)
        {
            MessageBox.Show(Window.GetWindow(this)!, "No se ha podido abrir la actividad.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CambiarEstadoActividad_Click(object sender, RoutedEventArgs e)
    {
        if (ActividadesDataGrid.SelectedItem is not ActividadResumen seleccionada)
        {
            MessageBox.Show(Window.GetWindow(this)!, "Selecciona primero una actividad.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        bool activar = seleccionada.Estado == "Inactiva";
        string accion = activar ? "activar" : "desactivar";
        if (MessageBox.Show(Window.GetWindow(this)!,
                $"¿Quieres {accion} la actividad «{seleccionada.Nombre}»?",
                "Confirmar cambio de estado", MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            var actividad = await contexto.Actividades.FindAsync(seleccionada.Id);
            if (actividad is null)
            {
                MessageBox.Show(Window.GetWindow(this)!, "La actividad ya no existe.",
                    "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                actividad.Active = activar;
                await contexto.SaveChangesAsync();
            }
            await CargarDatosAsync();
        }
        catch (Exception)
        {
            MessageBox.Show(Window.GetWindow(this)!, "No se ha podido cambiar el estado de la actividad.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void NuevaSesion_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new EditarSesionAdminWindow(_usuarioActual) { Owner = Window.GetWindow(this) };
        if (ventana.ShowDialog() == true)
            await CargarDatosAsync();
    }

    private async void GestionarSalas_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new SalasAdminWindow(_usuarioActual) { Owner = Window.GetWindow(this) };
        ventana.ShowDialog();
        await CargarDatosAsync();
    }

    private async void EditarSesion_Click(object sender, RoutedEventArgs e)
    {
        if (SesionesDataGrid.SelectedItem is not SesionResumen seleccionada)
        {
            MessageBox.Show(Window.GetWindow(this)!, "Selecciona primero una sesión.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            var servicio = new SesionService(contexto);
            var sesion = await servicio.ConsultarPorIdAsync(seleccionada.Id, _usuarioActual);
            if (sesion is null)
            {
                MessageBox.Show(Window.GetWindow(this)!, "La sesión ya no existe.",
                    "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);
                await CargarDatosAsync();
                return;
            }

            var ventana = new EditarSesionAdminWindow(_usuarioActual, sesion) { Owner = Window.GetWindow(this) };
            if (ventana.ShowDialog() == true)
                await CargarDatosAsync();
        }
        catch (Exception)
        {
            MessageBox.Show(Window.GetWindow(this)!, "No se ha podido abrir la sesión.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SesionesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ActualizarBotonEstadoSesion();
    }

    private void ActualizarBotonEstadoSesion()
    {
        if (CambiarEstadoSesionButton is null)
            return;

        var seleccionada = SesionesDataGrid?.SelectedItem as SesionResumen;
        bool programada = seleccionada?.Estado == EstadoSesion.PROGRAMADA.ToString();
        bool cancelada = seleccionada?.Estado == EstadoSesion.CANCELADA.ToString();

        CambiarEstadoSesionButton.IsEnabled = programada || cancelada;
        CambiarEstadoSesionButton.Content = cancelada ? "Reprogramar sesión" : "Cancelar sesión";
        CambiarEstadoSesionButton.Background = new SolidColorBrush(
            cancelada ? Color.FromRgb(0x17, 0x48, 0x3D) : Color.FromRgb(0xA3, 0x20, 0x20));
    }

    private async void CancelarSesion_Click(object sender, RoutedEventArgs e)
    {
        if (SesionesDataGrid.SelectedItem is not SesionResumen seleccionada)
        {
            MessageBox.Show(Window.GetWindow(this)!, "Selecciona primero una sesión.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        bool reprogramar = seleccionada.Estado == EstadoSesion.CANCELADA.ToString();
        if (!reprogramar && seleccionada.Estado != EstadoSesion.PROGRAMADA.ToString())
        {
            MessageBox.Show(Window.GetWindow(this)!, "Solo se pueden cancelar sesiones programadas o reprogramar sesiones canceladas.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string pregunta = reprogramar
            ? $"¿Quieres volver a programar la sesión {seleccionada.Id} de «{seleccionada.Actividad}»?\n\nLas reservas y listas de espera canceladas anteriormente no se restaurarán automáticamente."
            : $"¿Quieres cancelar la sesión {seleccionada.Id} de «{seleccionada.Actividad}»?";

        if (MessageBox.Show(Window.GetWindow(this)!, pregunta,
                reprogramar ? "Confirmar reprogramación" : "Confirmar cancelación",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            var servicio = new SesionService(contexto);
            bool actualizado = reprogramar
                ? await servicio.ReprogramarAsync(seleccionada.Id, _usuarioActual)
                : await servicio.CancelarAsync(seleccionada.Id, _usuarioActual);

            if (!actualizado)
                MessageBox.Show(Window.GetWindow(this)!,
                    "La sesión ya no existe o ha cambiado de estado. Actualiza la lista.",
                    "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);

            await CargarDatosAsync();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(Window.GetWindow(this)!, ex.Message,
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception)
        {
            MessageBox.Show(Window.GetWindow(this)!,
                reprogramar ? "No se ha podido reprogramar la sesión." : "No se ha podido cancelar la sesión.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Actualizar_Click(object sender, RoutedEventArgs e)
    {
        await CargarDatosAsync();
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e)
    {
        SolicitarInicio?.Invoke(this, EventArgs.Empty);
    }
}
