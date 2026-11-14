
using GenteFit.Data;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.WPF.Services;

public class AutenticacionService
{
    public async Task<bool> AutenticarAdministradorAsync(
        string username,
        string password)
    {
        var factory = new GenteFitContextFactory();

        await using var contexto =
            factory.CreateDbContext(Array.Empty<string>());

        var administrador = await contexto.Administradores
            .AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.Username == username && a.Active);

        return administrador is not null &&
            SeguridadContrasenas.Verificar(
                password, administrador.Password);
    }
}
