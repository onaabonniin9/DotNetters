
using System.Net.Mail;
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using GenteFit.Services;

namespace GenteFit.WPF.Services;

public class EdicionUsuariosService
{
    public async Task<bool> ActualizarAsync(
        int usuarioId,
        string nombre,
        string apellidos,
        string correo,
        UsuarioAutenticado usuarioActual)
    {
        var factory = new GenteFitContextFactory();

        await using var contexto =
            factory.CreateDbContext(Array.Empty<string>());

        await AutorizacionService.ExigirAdministradorActivoAsync(
            contexto, usuarioActual);

        nombre = nombre.Trim();
        apellidos = apellidos.Trim();
        correo = correo.Trim();

        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(apellidos))
        {
            throw new ArgumentException(
                "El nombre y los apellidos son obligatorios.");
        }

        if (!MailAddress.TryCreate(correo, out var direccion) ||
            !string.Equals(
                direccion.Address,
                correo,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Introduce un correo electrónico válido.");
        }

        var usuario = await contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario is null)
            return false;

        usuario.Name = nombre;
        usuario.Surname = apellidos;
        usuario.Email = correo;

        await contexto.SaveChangesAsync();

        return true;
    }
}
