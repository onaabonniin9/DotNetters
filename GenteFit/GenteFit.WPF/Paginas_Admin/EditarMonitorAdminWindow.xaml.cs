using System;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;
using Microsoft.EntityFrameworkCore;
using Monitor = GenteFit.Models.Monitor;

namespace GenteFit.WPF;

public partial class EditarMonitorAdminWindow : Window
{
    private readonly UsuarioAutenticado _usuarioActual;
    private readonly int? _monitorId;
    private bool _guardando;

    public EditarMonitorAdminWindow(UsuarioAutenticado usuarioActual, Monitor? monitor = null)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);
        usuarioActual.ExigirAdministrador();
        _usuarioActual = usuarioActual;
        _monitorId = monitor?.Id;
        InitializeComponent();

        if (monitor is not null)
        {
            Title = "GenteFit - Editar monitor";
            TituloTextBlock.Text = "Editar monitor";
            GuardarButton.Content = "Guardar cambios";
            NombreTextBox.Text = monitor.Name;
            ApellidosTextBox.Text = monitor.Surname;
            UsuarioTextBox.Text = monitor.Username;
            DniTextBox.Text = monitor.DNI;
            EspecialidadTextBox.Text = monitor.Speciality;
            CorreoTextBox.Text = monitor.Email;
            TelefonoTextBox.Text = monitor.Phone;
            ContrasenaPanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (_guardando) return;

        string nombre = NombreTextBox.Text.Trim();
        string apellidos = ApellidosTextBox.Text.Trim();
        string usuario = UsuarioTextBox.Text.Trim();
        string dni = DniTextBox.Text.Trim();
        string especialidad = EspecialidadTextBox.Text.Trim();
        string correo = CorreoTextBox.Text.Trim();
        string telefono = TelefonoTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellidos) ||
            string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(dni) ||
            string.IsNullOrWhiteSpace(especialidad) || string.IsNullOrWhiteSpace(telefono))
        {
            MostrarAviso("Completa todos los campos obligatorios.");
            return;
        }
        if (!MailAddress.TryCreate(correo, out var direccion) ||
            !string.Equals(direccion.Address, correo, StringComparison.OrdinalIgnoreCase))
        {
            MostrarAviso("Introduce un correo electrónico válido.");
            return;
        }
        if (_monitorId is null && string.IsNullOrWhiteSpace(ContrasenaPasswordBox.Password))
        {
            MostrarAviso("Introduce la contraseña inicial del monitor.");
            return;
        }

        _guardando = true;
        GuardarButton.IsEnabled = false;
        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            await AutorizacionService.ExigirAdministradorActivoAsync(contexto, _usuarioActual);

            bool usuarioRepetido = await contexto.Usuarios.AnyAsync(u =>
                u.Username == usuario && u.Id != (_monitorId ?? 0));
            if (usuarioRepetido)
            {
                MostrarAviso("Ya existe un usuario con ese nombre de acceso.");
                return;
            }
            bool dniRepetido = await contexto.Monitores.AnyAsync(m =>
                m.DNI == dni && m.Id != (_monitorId ?? 0));
            if (dniRepetido)
            {
                MostrarAviso("Ya existe un monitor con ese DNI.");
                return;
            }

            var servicio = new MonitorService(contexto);
            if (_monitorId is int id)
            {
                bool actualizado = await servicio.ActualizarAsync(id, nombre, apellidos,
                    usuario, correo, telefono, dni, especialidad);
                if (!actualizado)
                {
                    MostrarAviso("El monitor ya no existe.");
                    return;
                }
            }
            else
            {
                var monitor = new Monitor
                {
                    Name = nombre,
                    Surname = apellidos,
                    Username = usuario,
                    Email = correo,
                    Phone = telefono,
                    DNI = dni,
                    Speciality = especialidad,
                    Password = SeguridadContrasenas.CrearHash(ContrasenaPasswordBox.Password),
                    Active = true
                };
                await servicio.CrearAsync(monitor);
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
            MostrarAviso("No se han podido guardar los datos. Revisa si hay valores duplicados.");
        }
        catch (Exception)
        {
            MessageBox.Show(this, "No se ha podido guardar el monitor. Comprueba SQL Server.",
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
