using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;
using GenteFit.WPF.Components;
using GenteFit.WPF.Services;

namespace GenteFit.WPF
{
    public partial class ClientesRecepcionWindow : Window
    {
        private static readonly Brush ColorActivo = new SolidColorBrush(Color.FromRgb(0x1B, 0x8A, 0x4B));
        private static readonly Brush ColorInactivo = new SolidColorBrush(Color.FromRgb(0x8B, 0x00, 0x00));
        private readonly UsuarioAutenticado _usuarioAutenticado;
        private int _busquedaActual;

        public ClientesRecepcionWindow(UsuarioAutenticado usuarioAutenticado)
        {
            ArgumentNullException.ThrowIfNull(usuarioAutenticado);

            if  (usuarioAutenticado.Tipo !=TipoCuentaUsuario.Recepcionista && 
                usuarioAutenticado.Tipo != TipoCuentaUsuario.Administrador)
            {
                throw new UnauthorizedAccessException(
                    "No tienes permisos para consultar los clientes.");
            }

            InitializeComponent();
            _usuarioAutenticado = usuarioAutenticado;

            Loaded += async (_, _) => await CargarAsync(string.Empty);
        }
        private async Task CargarAsync(string texto)
        {
            int numeroBusqueda = ++_busquedaActual;

            try
            {
                var factory = new GenteFitContextFactory();
                await using var contexto = factory.CreateDbContext(Array.Empty<string>());
                var clienteService = new ClienteService(contexto);

                List<Cliente> clientes = await clienteService.ConsultarParaRecepcionAsync(_usuarioAutenticado, texto);

                if (numeroBusqueda != _busquedaActual)
                    return;
                
                ListaClientes.ItemsSource = clientes.Select(c => new ClienteFila(c.Id, $"{c.Name} {c.Surname}"
                .Trim(),c.Active)).ToList();

                TxtResumen.Text = clientes.Count == 0 
                    ? "No se ha encontrado ningún cliente."
                    : $"{clientes.Count} cliente(s)";
            }
            catch(UnauthorizedAccessException ex)
            {
                MessageBox.Show("No se ha podido cargar el listado de clientes. Revisa la conexión con la base de datos.",
                "GenteFit",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
                
            }
        }

        private async void Buscador_TextChanged(object sender, TextChangedEventArgs e)
        {
            await CargarAsync(Buscador.Text);
        }

        private async void BtnVer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button boton || boton.Tag is not int clienteId)
            return;

            var ventana = new DetalleClienteRecepcionWindow(_usuarioAutenticado, clienteId)
            {
                Owner = this
            };

            ventana.ShowDialog();

            await CargarAsync(Buscador.Text);
        }

        private sealed record ClienteFila(int Id, string NombreCompleto, bool Activo)
        {
            public Brush ColorEstado => Activo ? ColorActivo : ColorInactivo;
        }

    }
        
}