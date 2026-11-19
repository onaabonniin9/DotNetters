using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.WPF;

public partial class EditarSalaAdminWindow : Window
{
    private readonly UsuarioAutenticado _usuarioActual;
    private readonly int? _salaId;
    private bool _guardando;

    public EditarSalaAdminWindow(UsuarioAutenticado usuarioActual, Sala? sala = null)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);
        usuarioActual.ExigirAdministrador();
        _usuarioActual = usuarioActual;
        _salaId = sala?.Id;
        InitializeComponent();

        if (sala is not null)
        {
            Title = "GenteFit - Editar sala";
            TituloTextBlock.Text = "Editar sala";
            GuardarButton.Content = "Guardar cambios";
            NombreTextBox.Text = sala.Name;
            CapacidadTextBox.Text = sala.Capacity.ToString(CultureInfo.InvariantCulture);
        }
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (_guardando) return;

        string nombre = NombreTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MostrarAviso("Introduce el nombre de la sala.");
            return;
        }
        if (!int.TryParse(CapacidadTextBox.Text.Trim(), NumberStyles.None,
                CultureInfo.InvariantCulture, out int capacidad) || capacidad <= 0)
        {
            MostrarAviso("La capacidad debe ser un número entero mayor que cero.");
            return;
        }

        _guardando = true;
        GuardarButton.IsEnabled = false;
        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            await AutorizacionService.ExigirAdministradorActivoAsync(contexto, _usuarioActual);
            var servicio = new SalaService(contexto);
            if (_salaId is int id)
            {
                // Mantener la capacidad coherente con las sesiones programadas existentes.
                bool capacidadInsuficiente = await contexto.Sesiones.AnyAsync(s =>
                    s.Sala.Id == id && s.Status == EstadoSesion.PROGRAMADA &&
                    s.MaxParticipants > capacidad);
                if (capacidadInsuficiente)
                {
                    MostrarAviso("Hay sesiones programadas con más plazas que la nueva capacidad.");
                    return;
                }
                bool actualizada = await servicio.ActualizarAsync(id, nombre, capacidad);
                if (!actualizada)
                {
                    MostrarAviso("La sala ya no existe.");
                    return;
                }
            }
            else
            {
                var sala = new Sala { Name = nombre, Capacity = capacidad, Active = true };
                await servicio.CrearAsync(sala);
            }
            DialogResult = true;
        }
        catch (ArgumentException ex)
        {
            MostrarAviso(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(this, ex.Message, "Acceso denegado",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (DbUpdateException)
        {
            MostrarAviso("No se ha podido guardar la sala. Revisa los datos introducidos.");
        }
        catch (Exception)
        {
            MessageBox.Show(this, "No se ha podido guardar la sala. Comprueba SQL Server.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _guardando = false;
            GuardarButton.IsEnabled = true;
        }
    }

    private void MostrarAviso(string mensaje) =>
        MessageBox.Show(this, mensaje, "Datos no válidos",
            MessageBoxButton.OK, MessageBoxImage.Warning);

    private void Cancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
