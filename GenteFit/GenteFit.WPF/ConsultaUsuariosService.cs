
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.WPF.Services;

public record UsuarioResumen(
    int Id,
    string Username,
    string Name,
    string Surname,
    string Email,
    bool Active
);

public class ConsultaUsuariosService
{
    public async Task<List<UsuarioResumen>> ConsultarAsync(
        UsuarioAutenticado usuarioActual)
    {
        var factory = new GenteFitContextFactory();

        await using var contexto =
            factory.CreateDbContext(Array.Empty<string>());

        await AutorizacionService.ExigirAdministradorActivoAsync(
            contexto, usuarioActual);

        return await contexto.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new UsuarioResumen(
                u.Id,
                u.Username,
                u.Name,
                u.Surname,
                u.Email,
                u.Active))
            .ToListAsync();
    }
}
