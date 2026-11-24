using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;

namespace GenteFit.WPF;

public sealed record SalaResumen(int Id, string Nombre, int Capacidad, string Estado);

public partial class SalasAdminWindow : Window
{
    private readonly UsuarioAutenticado _usuarioActual;
    private bool _cargando;

    public SalasAdminWindow(UsuarioAutenticado usuarioActual)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);
        usuarioActual.ExigirAdministrador();
        _usuarioActual = usuarioActual;
        InitializeComponent();
        Loaded += async (_, _) => await CargarSalasAsync();
    }

    private async Task CargarSalasAsync()
    {
        if (_cargando) return;
        _cargando = true;
        EstadoTextBlock.Text = "Cargando salas...";
        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            await AutorizacionService.ExigirAdministradorActivoAsync(contexto, _usuarioActual);

            var servicio = new SalaService(contexto);
            var salas = await servicio.ConsultarTodasAsync();
            SalasDataGrid.ItemsSource = salas.Select(s => new SalaResumen(
                s.Id, s.Name, s.Capacity, s.Active ? "Activa" : "Inactiva")).ToList();
            EstadoTextBlock.Text = $"{salas.Count} salas registradas";
        }
        catch (UnauthorizedAccessException ex)
        {
            EstadoTextBlock.Text = "Acceso denegado";
            MessageBox.Show(this, ex.Message, "Acceso denegado",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
        }
        catch (Exception)
        {
            EstadoTextBlock.Text = "No se han podido cargar las salas";
            MessageBox.Show(this, "No se han podido cargar las salas. Comprueba SQL Server.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _cargando = false;
        }
    }

    private async void Actualizar_Click(object sender, RoutedEventArgs e) => await CargarSalasAsync();

    private async void NuevaSala_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new EditarSalaAdminWindow(_usuarioActual) { Owner = this };
        if (ventana.ShowDialog() == true)
            await CargarSalasAsync();
    }

    private async void EditarSala_Click(object sender, RoutedEventArgs e)
    {
        if (SalasDataGrid.SelectedItem is not SalaResumen seleccionada)
        {
            MessageBox.Show(this, "Selecciona primero una sala.", "GenteFit",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            await AutorizacionService.ExigirAdministradorActivoAsync(contexto, _usuarioActual);
            var servicio = new SalaService(contexto);
            var sala = await servicio.ConsultarPorIdAsync(seleccionada.Id);
            if (sala is null)
            {
                MessageBox.Show(this, "La sala ya no existe.", "GenteFit",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                await CargarSalasAsync();
                return;
            }
            var ventana = new EditarSalaAdminWindow(_usuarioActual, sala) { Owner = this };
            if (ventana.ShowDialog() == true)
                await CargarSalasAsync();
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(this, ex.Message, "Acceso denegado",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception)
        {
            MessageBox.Show(this, "No se ha podido abrir la sala.", "GenteFit",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CambiarEstado_Click(object sender, RoutedEventArgs e)
    {
        if (SalasDataGrid.SelectedItem is not SalaResumen seleccionada)
        {
            MessageBox.Show(this, "Selecciona primero una sala.", "GenteFit",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        bool activar = seleccionada.Estado == "Inactiva";
        string accion = activar ? "activar" : "desactivar";
        if (MessageBox.Show(this,
                $"¿Quieres {accion} la sala «{seleccionada.Nombre}»?",
                "Confirmar cambio de estado", MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            await AutorizacionService.ExigirAdministradorActivoAsync(contexto, _usuarioActual);
            var servicio = new SalaService(contexto);
            bool realizado = activar
                ? await servicio.ActivarAsync(seleccionada.Id)
                : await servicio.DesactivarAsync(seleccionada.Id);
            if (!realizado)
                MessageBox.Show(this, "La sala ya no existe.", "GenteFit",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            await CargarSalasAsync();
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(this, ex.Message, "Acceso denegado",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception)
        {
            MessageBox.Show(this, "No se ha podido cambiar el estado de la sala.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
}
