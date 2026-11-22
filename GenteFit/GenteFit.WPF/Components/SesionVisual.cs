using GenteFit.Models;

namespace GenteFit.WPF.Components;

// Solo conserva el usuario autenticado para la navegación de la cabecera.
public static class SesionVisual
{
    public static UsuarioAutenticado? UsuarioActual { get; set; }
}
