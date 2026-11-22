using System;
using System.Threading.Tasks;
using System.Windows;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;

namespace GenteFit.WPF
{
    public partial class PanelClienteWindow : Window
    {
        private readonly UsuarioAutenticado _usuarioAutenticado;
        private readonly GenteFitContext _context;
        private readonly ClienteService _clienteService;

        public PanelClienteWindow(UsuarioAutenticado usuarioAutenticado)
        {
            ArgumentNullException.ThrowIfNull(usuarioAutenticado);
            usuarioAutenticado.ExigirCliente();

            InitializeComponent();
            _usuarioAutenticado = usuarioAutenticado;

            var factory = new GenteFitContextFactory();
            _context = factory.CreateDbContext(Array.Empty<string>());
            _clienteService = new ClienteService(_context);

            CargarBienvenida(_usuarioAutenticado.Name,
                _usuarioAutenticado.Surname,
                _usuarioAutenticado.Username);

            Loaded += PanelClienteWindow_Loaded;
        }

        private async void PanelClienteWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var cliente = await _clienteService.ConsultarMiPerfilAsync(
                    _usuarioAutenticado);

                if (cliente is not null)
                    CargarBienvenida(cliente.Name, cliente.Surname, cliente.Username);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Tu cuenta no está activa o no tiene permisos para acceder al área de cliente.",
                    "Acceso denegado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                var inicioSesion = new MainWindow();
                inicioSesion.Show();
                Close();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "No se han podido actualizar los datos de bienvenida. Se mostrarán los últimos datos disponibles.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void CargarBienvenida(string nombre, string apellidos, string username)
        {
            string nombreCompleto = $"{nombre} {apellidos}".Trim();
            if (string.IsNullOrWhiteSpace(nombreCompleto))
                nombreCompleto = username;

            TxtBienvenida.Text = $"¡Bienvenido/a, {nombreCompleto}!";
        }

        private void BtnVerHorario_Click(object sender, RoutedEventArgs e)
        {
            var horarioWindow = new HorarioClienteWindow(_usuarioAutenticado);
            horarioWindow.Show();
            Close();
        }

        private void BtnMisReservas_Click(object sender, RoutedEventArgs e)
        {
            var misReservasWindow = new MisReservasWindow(_usuarioAutenticado);
            misReservasWindow.Show();
            Close();
        }

        private void BtnMiPerfil_Click(object sender, RoutedEventArgs e)
        {
            var perfilClienteWindow = new PerfilClienteWindow(_usuarioAutenticado);
            perfilClienteWindow.Show();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _context.Dispose();
            base.OnClosed(e);
        }
        private void Cabecera_InicioSolicitado(object sender, EventArgs e)
        {
            // Ya estamos en Inicio.

        }

        private void Cabecera_CerrarSesionSolicitado(object sender, EventArgs e)
        {
            var inicio = new MainWindow();
            inicio.Show();
            Close();
        }

    }
}
