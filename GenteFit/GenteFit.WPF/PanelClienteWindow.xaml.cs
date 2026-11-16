
using System;
using System.Windows;
using GenteFit.Models;

namespace GenteFit.WPF;

public partial class PanelClienteWindow : Window
{
    public UsuarioAutenticado UsuarioActual { get; }

    public int ClienteId => UsuarioActual.ObtenerClienteId();

    public PanelClienteWindow(UsuarioAutenticado usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        // Impedir que un administrador entre en el panel de cliente.
        usuario.ExigirCliente();

        UsuarioActual = usuario;

        InitializeComponent();
    }
}
