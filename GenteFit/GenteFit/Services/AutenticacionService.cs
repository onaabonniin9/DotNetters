
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class AutenticacionService
{
    public async Task<UsuarioAutenticado?> AutenticarAsync(
        string username,
        string password)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrEmpty(password))
        {
            return null;
        }

        var factory = new GenteFitContextFactory();

        await using var contexto =
            factory.CreateDbContext(Array.Empty<string>());

        var administrador = await contexto.Administradores
            .AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.Username == username && a.Active);

        if (administrador is not null &&
            SeguridadContrasenas.Verificar(
                password,
                administrador.Password))
        {
            return new UsuarioAutenticado(
                administrador.Id,
                administrador.Username,
                administrador.Name,
                administrador.Surname,
                TipoCuentaUsuario.Administrador,
                null);
        }

        var recepcionista = await contexto.Recepcionistas
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.Username == username && r.Active);
        
        if (recepcionista is not null &&
            SeguridadContrasenas.Verificar(
                password,
                recepcionista.Password))
        {
            return new UsuarioAutenticado(
                recepcionista.Id,
                recepcionista.Username,
                recepcionista.Name,
                recepcionista.Surname,
                TipoCuentaUsuario.Recepcionista,
                null);
            
        }


        var cliente = await contexto.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Username == username && c.Active);

        if (cliente is not null &&
            SeguridadContrasenas.Verificar(
                password,
                cliente.Password))
        {
            return new UsuarioAutenticado(
                cliente.Id,
                cliente.Username,
                cliente.Name,
                cliente.Surname,
                TipoCuentaUsuario.Cliente,
                cliente.Id);
        }

        return null;
    }
}
