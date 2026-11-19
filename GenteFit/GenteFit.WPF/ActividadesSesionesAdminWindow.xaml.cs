using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

public partial class ActividadesSesionesAdminWindow : Window
{
    private readonly UsuarioAutenticado _usuarioActual;
    private bool _cargando;

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

            ActividadesDataGrid.ItemsSource = actividades.Select(a => new ActividadResumen(
                a.Id, a.Name, a.Description, a.Level.ToString(), a.Duration,
                a.Active ? "Activa" : "Inactiva")).ToList();

            SesionesDataGrid.ItemsSource = sesiones.Select(s => new SesionResumen(
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

            EstadoTextBlock.Text = $"{actividades.Count} actividades · {sesiones.Count} sesiones";
        }
        catch (Exception)
        {
            EstadoTextBlock.Text = "No se han podido cargar los datos.";
            MessageBox.Show(this,
                "No se han podido consultar las actividades y sesiones. Comprueba SQL Server.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _cargando = false;
        }
    }

    private async void NuevaActividad_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new EditarActividadAdminWindow { Owner = this };
        if (ventana.ShowDialog() == true)
            await CargarDatosAsync();
    }

    private async void EditarActividad_Click(object sender, RoutedEventArgs e)
    {
        if (ActividadesDataGrid.SelectedItem is not ActividadResumen seleccionada)
        {
            MessageBox.Show(this, "Selecciona primero una actividad.",
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
                MessageBox.Show(this, "La actividad ya no existe.",
                    "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);
                await CargarDatosAsync();
                return;
            }

            var ventana = new EditarActividadAdminWindow(actividad) { Owner = this };
            if (ventana.ShowDialog() == true)
                await CargarDatosAsync();
        }
        catch (Exception)
        {
            MessageBox.Show(this, "No se ha podido abrir la actividad.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CambiarEstadoActividad_Click(object sender, RoutedEventArgs e)
    {
        if (ActividadesDataGrid.SelectedItem is not ActividadResumen seleccionada)
        {
            MessageBox.Show(this, "Selecciona primero una actividad.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        bool activar = seleccionada.Estado == "Inactiva";
        string accion = activar ? "activar" : "desactivar";
        if (MessageBox.Show(this,
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
                MessageBox.Show(this, "La actividad ya no existe.",
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
            MessageBox.Show(this, "No se ha podido cambiar el estado de la actividad.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void NuevaSesion_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new EditarSesionAdminWindow(_usuarioActual) { Owner = this };
        if (ventana.ShowDialog() == true)
            await CargarDatosAsync();
    }

    private async void GestionarSalas_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new SalasAdminWindow(_usuarioActual) { Owner = this };
        ventana.ShowDialog();
        await CargarDatosAsync();
    }

    private async void EditarSesion_Click(object sender, RoutedEventArgs e)
    {
        if (SesionesDataGrid.SelectedItem is not SesionResumen seleccionada)
        {
            MessageBox.Show(this, "Selecciona primero una sesión.",
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
                MessageBox.Show(this, "La sesión ya no existe.",
                    "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);
                await CargarDatosAsync();
                return;
            }

            var ventana = new EditarSesionAdminWindow(_usuarioActual, sesion) { Owner = this };
            if (ventana.ShowDialog() == true)
                await CargarDatosAsync();
        }
        catch (Exception)
        {
            MessageBox.Show(this, "No se ha podido abrir la sesión.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CancelarSesion_Click(object sender, RoutedEventArgs e)
    {
        if (SesionesDataGrid.SelectedItem is not SesionResumen seleccionada)
        {
            MessageBox.Show(this, "Selecciona primero una sesión.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (seleccionada.Estado != EstadoSesion.PROGRAMADA.ToString())
        {
            MessageBox.Show(this, "Solo se pueden cancelar sesiones programadas.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show(this,
                $"¿Quieres cancelar la sesión {seleccionada.Id} de «{seleccionada.Actividad}»?",
                "Confirmar cancelación", MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            var servicio = new SesionService(contexto);
            bool cancelada = await servicio.CancelarAsync(seleccionada.Id, _usuarioActual);
            if (!cancelada)
                MessageBox.Show(this, "La sesión ya no existe.",
                    "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);
            await CargarDatosAsync();
        }
        catch (Exception)
        {
            MessageBox.Show(this, "No se ha podido cancelar la sesión.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Actualizar_Click(object sender, RoutedEventArgs e)
    {
        await CargarDatosAsync();
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
