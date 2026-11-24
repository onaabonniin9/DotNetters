using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GenteFit.Models;

namespace GenteFit.WPF.Components;

public partial class CabeceraComun : UserControl
{
    public static readonly DependencyProperty RolProperty =
        DependencyProperty.Register(nameof(Rol), typeof(string), typeof(CabeceraComun),
            new PropertyMetadata("Usuario", AlCambiarRol));

    public static readonly DependencyProperty CompactaProperty =
        DependencyProperty.Register(nameof(Compacta), typeof(bool), typeof(CabeceraComun),
            new PropertyMetadata(false, AlCambiarTamano));

    public string Rol
    {
        get => (string)GetValue(RolProperty);
        set => SetValue(RolProperty, value);
    }

    public bool Compacta
    {
        get => (bool)GetValue(CompactaProperty);
        set => SetValue(CompactaProperty, value);
    }

    public CabeceraComun()
    {
        InitializeComponent();
        ActualizarRol();
        ActualizarTamano();
    }

    private static void AlCambiarRol(DependencyObject objeto, DependencyPropertyChangedEventArgs args)
        => ((CabeceraComun)objeto).ActualizarRol();

    private static void AlCambiarTamano(DependencyObject objeto, DependencyPropertyChangedEventArgs args)
        => ((CabeceraComun)objeto).ActualizarTamano();

    private void ActualizarRol()
    {
        if (TextoRol is not null)
            TextoRol.Text = Rol;
    }

    private void ActualizarTamano()
    {
        if (Logo is null)
            return;

        Logo.Width = Compacta ? 34 : 48;
        Logo.Height = Compacta ? 34 : 48;
        Marca.FontSize = Compacta ? 17 : 22;
        TextoRol.FontSize = Compacta ? 13 : 16;
        IconoRol.Width = Compacta ? 19 : 24;
        IconoRol.Height = Compacta ? 22 : 27;
        TextoMenu.FontSize = Compacta ? 16 : 21;
        TextoMenu.Margin = Compacta ? new Thickness(0, 0, 7, 0) : new Thickness(0, 0, 12, 0);
        IconoMenu.Width = Compacta ? 27 : 34;
        IconoMenu.Height = Compacta ? 26 : 30;
    }

    private void AbrirMenu_Click(object sender, RoutedEventArgs e)
    {
        if (BotonMenu.ContextMenu is { } menu)
        {
            menu.PlacementTarget = BotonMenu;
            menu.IsOpen = true;
        }
    }

    private void IrInicio_Click(object sender, RoutedEventArgs e)
    {
        Window? ventana = Window.GetWindow(this);
        if (ventana is null)
            return;

        if (Rol == "Administrador")
        {
            var panel = Application.Current.Windows.OfType<PanelPrincipalWindow>().FirstOrDefault();
            if (panel is null)
                return;

            // Cerrar los diálogos propietarios hasta regresar al panel principal.
            while (ventana != panel)
            {
                Window? propietaria = ventana.Owner;
                ventana.Close();
                if (propietaria is null)
                    return;
                ventana = propietaria;
            }

            panel.MostrarInicioDesdeCabecera();
            panel.Activate();
        }
        else if (Rol == "Cliente")
        {
            if (ventana is PanelClienteWindow)
                return;

            UsuarioAutenticado? usuario = SesionVisual.UsuarioActual;
            if (usuario is null || usuario.Tipo != TipoCuentaUsuario.Cliente)
                return;

            var panel = new PanelClienteWindow(usuario);
            NavegacionCliente.AbrirDesde(ventana, panel);
        }
        else if (Rol == "Recepción" || Rol == "Monitor")
        {
            if (ventana is PanelMonitorWindow or PanelRecepcionWindow)
                return;
            UsuarioAutenticado? usuario = SesionVisual.UsuarioActual;
            if (usuario is null) return;
            if (Rol == "Monitor" && usuario.Tipo == TipoCuentaUsuario.Monitor)
                NavegacionCliente.AbrirDesde(ventana, new PanelMonitorWindow(usuario));
            else if (Rol == "Recepción" && usuario.Tipo == TipoCuentaUsuario.Recepcionista)
                NavegacionCliente.AbrirDesde(ventana, new PanelRecepcionWindow(usuario));
        }
    }

    private void CerrarSesion_Click(object sender, RoutedEventArgs e)
    {
        Window? ventana = Window.GetWindow(this);
        if (ventana is null)
            return;

        if (Rol != "Administrador")
        {
            NavegacionCliente.CerrarSesion(ventana);
            return;
        }

        var login = new MainWindow();
        login.Show();
        SesionVisual.UsuarioActual = null;

        if (Rol == "Administrador")
        {
            var panel = Application.Current.Windows.OfType<PanelPrincipalWindow>().FirstOrDefault();
            if (panel is not null)
            {
                panel.Close();
                return;
            }
        }

        ventana.Close();
    }
}
