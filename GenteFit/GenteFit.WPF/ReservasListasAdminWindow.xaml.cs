using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GenteFit.Models;
using GenteFit.WPF.Services;

namespace GenteFit.WPF;

public partial class ReservasListasAdminWindow : Window
{
    private readonly UsuarioAutenticado _usuarioActual;
    private List<ReservaAdminResumen> _reservas = new();
    private List<ListaEsperaAdminResumen> _listas = new();
    private bool _cargando;

    public ReservasListasAdminWindow(UsuarioAutenticado usuarioActual)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);
        usuarioActual.ExigirAdministrador();
        _usuarioActual = usuarioActual;
        InitializeComponent();
        Loaded += async (_, _) => await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        if (_cargando) return;
        _cargando = true;
        ActualizarButton.IsEnabled = false;
        EstadoTextBlock.Text = "Cargando reservas y listas de espera...";

        try
        {
            var servicio = new ConsultaReservasAdminService();
            var datos = await servicio.ConsultarAsync(_usuarioActual);
            _reservas = datos.Reservas;
            _listas = datos.Listas;
            AplicarFiltros();
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
            EstadoTextBlock.Text = "No se han podido cargar los datos";
            MessageBox.Show(this,
                "No se han podido consultar las reservas y listas de espera. Comprueba la conexión con SQL Server.",
                "GenteFit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _cargando = false;
            ActualizarButton.IsEnabled = true;
        }
    }

    private void AplicarFiltros()
    {
        if (BuscarTextBox is null || ReservasDataGrid is null || ListasDataGrid is null)
            return;

        string filtro = BuscarTextBox.Text.Trim();
        var reservasFiltradas = _reservas.Where(r => Coincide(filtro,
            r.Id.ToString(), r.SesionId.ToString(), r.Actividad, r.Cliente,
            r.Usuario, r.Estado, r.Fecha)).ToList();
        var listasFiltradas = _listas.Where(l => Coincide(filtro,
            l.Id.ToString(), l.SesionId.ToString(), l.Actividad, l.Cliente,
            l.Usuario, l.Estado, l.Fecha)).ToList();

        ReservasDataGrid.ItemsSource = reservasFiltradas;
        ListasDataGrid.ItemsSource = listasFiltradas;
        EstadoTextBlock.Text = $"{reservasFiltradas.Count} reservas · {listasFiltradas.Count} entradas en lista de espera";
        SinReservasTextBlock.Visibility = reservasFiltradas.Count == 0
            ? Visibility.Visible : Visibility.Collapsed;
        SinListasTextBlock.Visibility = listasFiltradas.Count == 0
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private static bool Coincide(string filtro, params string[] valores) =>
        filtro.Length == 0 || valores.Any(v =>
            v.Contains(filtro, StringComparison.OrdinalIgnoreCase));

    private void BuscarTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => AplicarFiltros();

    private async void Actualizar_Click(object sender, RoutedEventArgs e)
        => await CargarDatosAsync();

    private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
}
