using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;

namespace GenteFit.WPF;

public sealed record MonitorResumen(
    int Id, string Nombre, string Apellidos, string Usuario,
    string Dni, string Especialidad, string Correo, string Estado);

public partial class MonitoresAdminWindow : UserControl
{
    public event EventHandler? SolicitarInicio;


    private readonly UsuarioAutenticado _usuarioActual;
    private bool _cargando;
    private System.Collections.Generic.List<MonitorResumen> _monitores = new();

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
            _monitores = monitores.Select(m => new MonitorResumen(
                m.Id, m.Name, m.Surname, m.Username, m.DNI,
                m.Speciality, m.Email, m.Active ? "Activo" : "Inactivo")).ToList();
            AplicarFiltro();
        }
        catch (UnauthorizedAccessException ex)
        {
            EstadoTextBlock.Text = "Acceso denegado";
            MessageBox.Show(Window.GetWindow(this)!, ex.Message, "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);
            SolicitarInicio?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            EstadoTextBlock.Text = "No se han podido cargar los monitores";
            MessageBox.Show(Window.GetWindow(this)!, "Comprueba la conexión con SQL Server.",
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
        if (BuscarTextBox is null || MonitoresDataGrid is null)
            return;

        string texto = BuscarTextBox.Text.Trim();
        var filtrados = _monitores.Where(m => texto.Length == 0 ||
            new[] { m.Id.ToString(), m.Nombre, m.Apellidos, m.Usuario,
                    m.Dni, m.Especialidad, m.Correo, m.Estado }
                .Any(c => c.Contains(texto, StringComparison.OrdinalIgnoreCase))).ToList();

        MonitoresDataGrid.ItemsSource = filtrados;
        EstadoTextBlock.Text = $"{filtrados.Count} monitores registrados";
    }

    private async void Actualizar_Click(object sender, RoutedEventArgs e) => await CargarMonitoresAsync();

    private async void NuevoMonitor_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new EditarMonitorAdminWindow(_usuarioActual) { Owner = Window.GetWindow(this) };
        if (ventana.ShowDialog() == true)
            await CargarMonitoresAsync();
    }

    private async void EditarMonitor_Click(object sender, RoutedEventArgs e)
    {
        if (MonitoresDataGrid.SelectedItem is not MonitorResumen seleccionado)
        {
            MessageBox.Show(Window.GetWindow(this)!, "Selecciona primero un monitor.", "GenteFit",
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
                MessageBox.Show(Window.GetWindow(this)!, "El monitor ya no existe.", "GenteFit",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                await CargarMonitoresAsync();
                return;
            }
            var ventana = new EditarMonitorAdminWindow(_usuarioActual, monitor) { Owner = Window.GetWindow(this) };
            if (ventana.ShowDialog() == true)
                await CargarMonitoresAsync();
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(Window.GetWindow(this)!, ex.Message, "Acceso denegado",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception)
        {
            MessageBox.Show(Window.GetWindow(this)!, "No se ha podido abrir el monitor.", "GenteFit",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CambiarEstado_Click(object sender, RoutedEventArgs e)
    {
        if (MonitoresDataGrid.SelectedItem is not MonitorResumen seleccionado)
        {
            MessageBox.Show(Window.GetWindow(this)!, "Selecciona primero un monitor.", "GenteFit",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        bool activar = seleccionado.Estado == "Inactivo";
        string accion = activar ? "activar" : "desactivar";
        if (MessageBox.Show(Window.GetWindow(this)!,
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
                MessageBox.Show(Window.GetWindow(this)!, "El monitor ya no existe.", "GenteFit",
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
            MessageBox.Show(Window.GetWindow(this)!, ex.Message, "Acceso denegado",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception)
        {
            MessageBox.Show(Window.GetWindow(this)!, "No se ha podido cambiar el estado del monitor.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e) => SolicitarInicio?.Invoke(this, EventArgs.Empty);
}
