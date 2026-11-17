
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class ClienteService
{
    private readonly GenteFitContext _context;

    public ClienteService(GenteFitContext context)
    {
        _context = context;
    }


    public async Task<Cliente> CrearAsync(Cliente cliente)
    {
        if (string.IsNullOrWhiteSpace(cliente.Name))
            throw new ArgumentException("Error: falta añadir un nombre");

        if (string.IsNullOrWhiteSpace(cliente.Surname))
            throw new ArgumentException("Error: faltan los apellidos");

        if (string.IsNullOrWhiteSpace(cliente.DNI))
            throw new ArgumentException("Error: falta añadir el DNI");

        _context.Set<Cliente>().Add(cliente);

        await _context.SaveChangesAsync();

        return cliente;
    }


    public async Task<List<Cliente>> ConsultarTodasAsync()
    {
        return await _context.Set<Cliente>()
            .OrderBy(c => c.Surname)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Cliente?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<Cliente>()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cliente?> ConsultarMiPerfilAsync(
        UsuarioAutenticado usuarioActual)
    {
        int clienteId = await AutorizacionService.ExigirClienteActivoAsync(
            _context, usuarioActual);

        return await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clienteId);
    }

    public async Task<bool> ActualizarMiPerfilAsync(
        UsuarioAutenticado usuarioActual,
        string name,
        string surname,
        string username,
        string email,
        string phone,
        string dni,
        DateTime birthDate,
        string address,
        string city,
        string postalCode,
        string iban)
    {
        int clienteId = await AutorizacionService.ExigirClienteActivoAsync(
            _context, usuarioActual);

        return await ActualizarInternoAsync(
            clienteId, name, surname, username, email, phone, dni,
            birthDate, address, city, postalCode, iban,
            usuarioActual);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        string name,
        string surname,
        string username,
        string email,
        string phone,
        string dni,
        DateTime birthDate,
        string address,
        string city,
        string postalCode,
        string iban)
    {
        return await ActualizarInternoAsync(
            id, name, surname, username, email, phone, dni,
            birthDate, address, city, postalCode, iban, null);
    }

    private async Task<bool> ActualizarInternoAsync(
        int id,
        string name,
        string surname,
        string username,
        string email,
        string phone,
        string dni,
        DateTime birthDate,
        string address,
        string city,
        string postalCode,
        string iban,
        UsuarioAutenticado? usuarioActual)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es obligatorio.", nameof(name));

        if (string.IsNullOrWhiteSpace(surname))
            throw new ArgumentException("Los apellidos son obligatorios.", nameof(surname));

        if (string.IsNullOrWhiteSpace(dni))
            throw new ArgumentException("El DNI es obligatorio.", nameof(dni));

        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("El nombre de usuario es obligatorio.", nameof(username));

        string usuarioNormalizado = username.Trim();

        bool usernameEnUso = await _context.Usuarios
            .AsNoTracking()
            .AnyAsync(u => u.Id != id && u.Username == usuarioNormalizado);

        if (usernameEnUso)
            throw new ArgumentException(
                "Ese nombre de usuario ya está siendo utilizado. Elige otro.",
                nameof(username));

        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cliente is null)
            return false;

        if (usuarioActual is not null)
        {
            int clienteAutorizado =
                await AutorizacionService.ExigirClienteActivoAsync(
                    _context, usuarioActual);

            if (clienteAutorizado != id)
                throw new UnauthorizedAccessException(
                    "No puedes modificar los datos de otro cliente.");
        }

        cliente.Name = name.Trim();
        cliente.Surname = surname.Trim();
        cliente.Username = usuarioNormalizado;
        cliente.Email = email?.Trim() ?? string.Empty;
        cliente.Phone = phone?.Trim() ?? string.Empty;
        cliente.DNI = dni.Trim();
        cliente.BirthDate = birthDate;
        cliente.Address = address?.Trim() ?? string.Empty;
        cliente.City = city?.Trim() ?? string.Empty;
        cliente.PostalCode = postalCode?.Trim() ?? string.Empty;
        cliente.IBAN = iban?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var cliente = await ConsultarPorIdAsync(id);

        if (cliente is null)
            return false;

        _context.Set<Cliente>().Remove(cliente);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var cliente = await ConsultarPorIdAsync(id);

        if (cliente is null)
            return false;

        cliente.Active = false;

        await _context.SaveChangesAsync();

        return true;
    }




}
