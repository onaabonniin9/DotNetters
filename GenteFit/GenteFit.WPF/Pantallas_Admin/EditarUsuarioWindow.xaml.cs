
using System;
using System.Windows;
using GenteFit.Models;
using GenteFit.WPF.Services;

namespace GenteFit.WPF;

public partial class EditarUsuarioWindow : Window
{
    private readonly int _usuarioId;
    private readonly UsuarioAutenticado _usuarioActual;

    public EditarUsuarioWindow(
        UsuarioResumen usuario,
        UsuarioAutenticado usuarioActual)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentNullException.ThrowIfNull(usuarioActual);

        usuarioActual.ExigirAdministrador();

        InitializeComponent();

        _usuarioId = usuario.Id;
        _usuarioActual = usuarioActual;

        UsuarioTexto.Text = $"Usuario: {usuario.Username}";
        NombreTextBox.Text = usuario.Name;
        ApellidosTextBox.Text = usuario.Surname;
        CorreoTextBox.Text = usuario.Email;
    }

    private async void Guardar_Click(
        object sender, RoutedEventArgs e)
    {
        GuardarButton.IsEnabled = false;

        try
        {
            var servicio = new EdicionUsuariosService();

            bool actualizado = await servicio.ActualizarAsync(
                _usuarioId,
                NombreTextBox.Text,
                ApellidosTextBox.Text,
                CorreoTextBox.Text,
                _usuarioActual);

            if (!actualizado)
            {
                MessageBox.Show(
                    this,
                    "El usuario ya no existe.",
                    "GenteFit",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            DialogResult = true;
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Datos no válidos",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Acceso denegado",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception)
        {
            MessageBox.Show(
                this,
                "No se han podido guardar los cambios. Revisa los datos y la conexión con SQL Server.",
                "GenteFit",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            GuardarButton.IsEnabled = true;
        }
    }

    private void Cancelar_Click(
        object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
