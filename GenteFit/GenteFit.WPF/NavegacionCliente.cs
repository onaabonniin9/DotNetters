using System;
using System.Windows;

namespace GenteFit.WPF;

internal static class NavegacionCliente
{
    public static void AbrirDesde(Window actual, Window destino)
    {
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(destino);

        // Una ventana nueva no debe volver al tamaño ni a la posición predeterminados.
        Rect limites = actual.WindowState == WindowState.Normal
            ? new Rect(actual.Left, actual.Top, actual.ActualWidth, actual.ActualHeight)
            : actual.RestoreBounds;

        destino.WindowStartupLocation = WindowStartupLocation.Manual;
        destino.Width = Math.Max(destino.MinWidth, limites.Width);
        destino.Height = Math.Max(destino.MinHeight, limites.Height);
        destino.Left = limites.Left;
        destino.Top = limites.Top;
        destino.WindowState = actual.WindowState == WindowState.Maximized
            ? WindowState.Maximized
            : WindowState.Normal;

        destino.Show();
        actual.Close();
    }
}
