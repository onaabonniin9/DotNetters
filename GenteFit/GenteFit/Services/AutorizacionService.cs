
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
