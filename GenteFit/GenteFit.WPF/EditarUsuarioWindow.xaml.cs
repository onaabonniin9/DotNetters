using System;
using System.Windows;
using GenteFit.WPF.Services;

namespace GenteFit.WPF;

public partial class EditarUsuarioWindow : Window
{
    private readonly int _usuarioId;

    public EditarUsuarioWindow(UsuarioResumen usuario)
    {
        InitializeComponent();
        _usuarioId = usuario.Id;
        UsuarioTexto.Text = $"Usuario: {usuario.Username}";
        NombreTextBox.Text = usuario.Name;
        ApellidosTextBox.Text = usuario.Surname;
        CorreoTextBox.Text = usuario.Email;
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        GuardarButton.IsEnabled = false;
        try
        {
            var servicio = new EdicionUsuariosService();
            bool actualizado = await servicio.ActualizarAsync(
                _usuarioId,
                NombreTextBox.Text,
                ApellidosTextBox.Text,
                CorreoTextBox.Text);

            if (!actualizado)
            {
                MessageBox.Show(this, "El usuario ya no existe.", "GenteFit",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "Datos no válidos",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception)
        {
            MessageBox.Show(this,
                "No se han podido guardar los cambios. Revisa los datos y la conexión con SQL Server.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            GuardarButton.IsEnabled = true;
        }
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
