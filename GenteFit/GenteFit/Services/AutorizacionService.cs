
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public static class AutorizacionService
{
    public static async Task ExigirAdministradorActivoAsync(
        GenteFitContext contexto,
        UsuarioAutenticado usuarioActual)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);

        usuarioActual.ExigirAdministrador();

        bool administradorActivo = await contexto.Administradores
            .AsNoTracking()
            .AnyAsync(a =>
                a.Id == usuarioActual.UsuarioId &&
                a.Active);

        if (!administradorActivo)
        {
            throw new UnauthorizedAccessException(
                "La cuenta no tiene permisos de administrador activos.");
        }
    }
    //Se ha puesto Personal Recepcion ya que en los casos de uso el administrador es un actor alternativo
    // es decir que no solo es el recepcionista.
    public static async Task ExigirPersonalRecepcionActivoAsync(
        GenteFitContext contexto,
        UsuarioAutenticado usuarioActual)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);
        if (usuarioActual.Tipo == TipoCuentaUsuario.Administrador)
        {
            await ExigirAdministradorActivoAsync(contexto,usuarioActual);
            return;
        }

        usuarioActual.ExigirRecepcionista();

        bool recepcionistaActivo = await contexto.Recepcionistas
        .AsNoTracking()
        .AnyAsync(r =>
            r.Id == usuarioActual.UsuarioId && r.Active);

        if (!recepcionistaActivo)
        {
            throw new UnauthorizedAccessException(
                "La cuenta no está activa.");
        }        

    }

    public static async Task ExigirMonitorActivoAsync(GenteFitContext contexto, UsuarioAutenticado usuarioActual)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);
        usuarioActual.ExigirMonitor();

        bool activo = await contexto.Monitores
        .AsNoTracking()
        .AnyAsync(m => m.Id == usuarioActual.UsuarioId && m.Active);

        if (!activo)
        {
            throw new UnauthorizedAccessException("La cuenta no está activa");
        }
    }
    

    public static async Task<int> ExigirClienteActivoAsync(
        GenteFitContext contexto,
        UsuarioAutenticado usuarioActual)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);

        int clienteId = usuarioActual.ObtenerClienteId();

        bool clienteActivo = await contexto.Clientes
            .AsNoTracking()
            .AnyAsync(c =>
                c.Id == clienteId &&
                c.Id == usuarioActual.UsuarioId &&
                c.Active);

        if (!clienteActivo)
        {
            throw new UnauthorizedAccessException(
                "La cuenta del cliente no está activa.");
        }

        return clienteId;
    }
}
