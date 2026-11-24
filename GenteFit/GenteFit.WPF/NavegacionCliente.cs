using System;
using System.Windows;
using GenteFit.WPF.Components;

namespace GenteFit.WPF;

// Navegación común: una sola ventana visible, sin diálogos de navegación.
internal static class NavegacionCliente
{
    public static bool AbrirDesde(Window actual, Window destino)
    {
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(destino);
        if (!actual.IsLoaded) return false;

        Rect limites = actual.WindowState == WindowState.Normal
            ? new Rect(actual.Left, actual.Top, actual.ActualWidth, actual.ActualHeight)
            : actual.RestoreBounds;
        destino.WindowStartupLocation = WindowStartupLocation.Manual;
        destino.SizeToContent = SizeToContent.Manual;
        destino.MinWidth = 900;
        destino.MinHeight = 650;
        destino.Width = Math.Max(destino.MinWidth, limites.Width);
        destino.Height = Math.Max(destino.MinHeight, limites.Height);
        destino.Left = limites.Left;
        destino.Top = limites.Top;
        destino.WindowState = actual.WindowState == WindowState.Maximized
            ? WindowState.Maximized : WindowState.Normal;

        // Mantener la aplicación viva mientras se sustituye la ventana.
        var app = Application.Current;
        var modo = app.ShutdownMode;
        bool cerrada = false;
        EventHandler alCerrar = (_, _) => cerrada = true;
        actual.Closed += alCerrar;
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            actual.Close();
            if (!cerrada)
            {
                destino.Close();
                return false;
            }
            app.MainWindow = destino;
            destino.Show();
            return true;
        }
        finally
        {
            actual.Closed -= alCerrar;
            app.ShutdownMode = modo;
        }
    }

    public static void CerrarSesion(Window actual)
    {
        if (AbrirDesde(actual, new MainWindow()))
            SesionVisual.UsuarioActual = null;
    }
}
