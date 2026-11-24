using System;
using System.Windows;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;

namespace GenteFit.WPF;

public partial class EditarActividadAdminWindow : Window
{
    private readonly Actividad? _actividad;

    public EditarActividadAdminWindow(Actividad? actividad = null)
    {
        InitializeComponent();
        _actividad = actividad;
        IntensidadComboBox.ItemsSource = Enum.GetValues<NivelIntensidad>();
        IntensidadComboBox.SelectedIndex = 0;

        if (_actividad is null)
            return;

        Title = "GenteFit - Editar actividad";
        TituloTextBlock.Text = "Editar actividad";
        GuardarButton.Content = "Guardar cambios";
        NombreTextBox.Text = _actividad.Name;
        DescripcionTextBox.Text = _actividad.Description;
        IntensidadComboBox.SelectedItem = _actividad.Level;
        DuracionTextBox.Text = _actividad.Duration.ToString();
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        string nombre = NombreTextBox.Text.Trim();
        string descripcion = DescripcionTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(descripcion))
        {
            MessageBox.Show(this, "El nombre y la descripción son obligatorios.",
                "Datos no válidos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(DuracionTextBox.Text.Trim(), out int duracion) || duracion <= 0)
        {
            MessageBox.Show(this, "La duración debe ser un número entero mayor que cero.",
                "Datos no válidos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (IntensidadComboBox.SelectedItem is not NivelIntensidad nivel)
            return;

        GuardarButton.IsEnabled = false;
        try
        {
            var factory = new GenteFitContextFactory();
            await using var contexto = factory.CreateDbContext(Array.Empty<string>());
            var servicio = new ActividadService(contexto);

            if (_actividad is null)
            {
                await servicio.CrearAsync(new Actividad
                {
                    Name = nombre,
                    Description = descripcion,
                    Level = nivel,
                    Duration = duracion,
                    Active = true
                });
            }
            else
            {
                bool actualizada = await servicio.ActualizarAsync(
                    _actividad.Id, nombre, descripcion, nivel, duracion);
                if (!actualizada)
                {
                    MessageBox.Show(this, "La actividad ya no existe.",
                        "GenteFit", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            DialogResult = true;
        }
        catch (Exception)
        {
            MessageBox.Show(this, "No se ha podido guardar la actividad. Comprueba los datos y la conexión SQL.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            GuardarButton.IsEnabled = true;
        }
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => Close();
}
