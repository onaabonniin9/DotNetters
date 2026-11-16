
using System.Data;
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using GenteFit.Services;

namespace GenteFit.WPF.Services;

public class GestionEstadoUsuariosService
{
    public async Task<bool> CambiarEstadoAsync(
        int usuarioId,
        bool activar,
        UsuarioAutenticado usuarioActual)
    {
        var factory = new GenteFitContextFactory();

        await using var contexto =
            factory.CreateDbContext(Array.Empty<string>());

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        await AutorizacionService.ExigirAdministradorActivoAsync(
            contexto, usuarioActual);

        var usuario = await contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario is null)
            return false;

        if (usuario.Active == activar)
        {
            await transaccion.CommitAsync();
            return true;
        }

        if (!activar)
        {
            bool esAdministrador =
                await contexto.Administradores
                    .AnyAsync(a => a.Id == usuarioId);

            if (esAdministrador)
            {
                int administradoresActivos =
                    await contexto.Administradores
                        .CountAsync(a => a.Active);

                if (administradoresActivos <= 1)
                {
                    throw new InvalidOperationException(
                        "No se puede desactivar el último administrador activo.");
                }
            }
        }

        usuario.Active = activar;

        await contexto.SaveChangesAsync();
        await transaccion.CommitAsync();

        return true;
    }
}
