
using System;
using System.Net.Mail;
using System.Threading.Tasks;
using GenteFit.Data;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.WPF.Services;

public class EdicionUsuariosService
{
    public async Task<bool> ActualizarAsync(
        int usuarioId,
        string nombre,
        string apellidos,
        string correo)
    {
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

        var factory = new GenteFitContextFactory();

        await using var contexto =
            factory.CreateDbContext(Array.Empty<string>());

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
