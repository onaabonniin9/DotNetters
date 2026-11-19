using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;

namespace GenteFit.WPF;

public sealed record MonitorResumen(
    int Id, string Nombre, string Apellidos, string Usuario,
    string Dni, string Especialidad, string Correo, string Estado);

public partial class MonitoresAdminWindow : Window
{
    private readonly UsuarioAutenticado _usuarioActual;
    private bool _cargando;

    public MonitoresAdminWindow(UsuarioAutenticado usuarioActual)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);
        usuarioActual.ExigirAdministrador();
        _usuarioActual = usuarioActual;
        InitializeComponent();
        Loaded += async (_, _) => await CargarMonitoresAsync();
    }

    private async Task CargarMonitoresAsync()
    {
        if (_cargando) return;
        _cargando = true;
        EstadoTextBlock.Text = "Cargando monitores...";
        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            await AutorizacionService.ExigirAdministradorActivoAsync(contexto, _usuarioActual);

            var servicio = new MonitorService(contexto);
            var monitores = await servicio.ConsultarTodasAsync();
            MonitoresDataGrid.ItemsSource = monitores.Select(m => new MonitorResumen(
                m.Id, m.Name, m.Surname, m.Username, m.DNI,
                m.Speciality, m.Email, m.Active ? "Activo" : "Inactivo")).ToList();
            EstadoTextBlock.Text = $"{monitores.Count} monitores registrados";
        }
        catch (UnauthorizedAccessException ex)
        {
            EstadoTextBlock.Text = "Acceso denegado";
            MessageBox.Show(this, ex.Message, "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
        }
        catch (Exception)
        {
            EstadoTextBlock.Text = "No se han podido cargar los monitores";
            MessageBox.Show(this, "Comprueba la conexión con SQL Server.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _cargando = false;
        }
    }

    private async void Actualizar_Click(object sender, RoutedEventArgs e) => await CargarMonitoresAsync();

    private async void NuevoMonitor_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new EditarMonitorAdminWindow(_usuarioActual) { Owner = this };
        if (ventana.ShowDialog() == true)
            await CargarMonitoresAsync();
    }

    private async void EditarMonitor_Click(object sender, RoutedEventArgs e)
    {
        if (MonitoresDataGrid.SelectedItem is not MonitorResumen seleccionado)
        {
            MessageBox.Show(this, "Selecciona primero un monitor.", "GenteFit",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            await AutorizacionService.ExigirAdministradorActivoAsync(contexto, _usuarioActual);
            var servicio = new MonitorService(contexto);
            var monitor = await servicio.ConsultarPorIdAsync(seleccionado.Id);
            if (monitor is null)
            {
                MessageBox.Show(this, "El monitor ya no existe.", "GenteFit",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                await CargarMonitoresAsync();
                return;
            }
            var ventana = new EditarMonitorAdminWindow(_usuarioActual, monitor) { Owner = this };
            if (ventana.ShowDialog() == true)
                await CargarMonitoresAsync();
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(this, ex.Message, "Acceso denegado",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception)
        {
            MessageBox.Show(this, "No se ha podido abrir el monitor.", "GenteFit",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CambiarEstado_Click(object sender, RoutedEventArgs e)
    {
        if (MonitoresDataGrid.SelectedItem is not MonitorResumen seleccionado)
        {
            MessageBox.Show(this, "Selecciona primero un monitor.", "GenteFit",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        bool activar = seleccionado.Estado == "Inactivo";
        string accion = activar ? "activar" : "desactivar";
        if (MessageBox.Show(this,
                $"¿Quieres {accion} al monitor «{seleccionado.Nombre} {seleccionado.Apellidos}»?",
                "Confirmar cambio de estado", MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            await AutorizacionService.ExigirAdministradorActivoAsync(contexto, _usuarioActual);
            var servicio = new MonitorService(contexto);
            var monitor = await servicio.ConsultarPorIdAsync(seleccionado.Id);
            if (monitor is null)
            {
                MessageBox.Show(this, "El monitor ya no existe.", "GenteFit",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (activar)
            {
                // MonitorService solo ofrece DesactivarAsync: reactivar sin cambiar código compartido.
                monitor.Active = true;
                await contexto.SaveChangesAsync();
            }
            else
            {
                await servicio.DesactivarAsync(monitor.Id);
            }
            await CargarMonitoresAsync();
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(this, ex.Message, "Acceso denegado",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception)
        {
            MessageBox.Show(this, "No se ha podido cambiar el estado del monitor.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
}
