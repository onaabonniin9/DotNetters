
using GenteFit.Data;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.WPF.Services;

public class GestionEstadoUsuariosService
{
    public async Task<bool> CambiarEstadoAsync(
        int usuarioId,
        bool activar)
    {
        var factory = new GenteFitContextFactory();

        await using var contexto =
            factory.CreateDbContext(Array.Empty<string>());

        var usuario = await contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario is null)
            return false;

        if (usuario.Active == activar)
            return true;

        if (!activar)
        {
            bool esAdministrador = await contexto.Administradores
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

        return true;
    }
}
