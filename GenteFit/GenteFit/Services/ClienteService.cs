
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
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Error: falta añadir un nombre");

        if (string.IsNullOrWhiteSpace(surname))
            throw new ArgumentException("Error: faltan los apellidos");

        if (string.IsNullOrWhiteSpace(dni))
            throw new ArgumentException("Error: falta añadir el DNI");

        var cliente = await ConsultarPorIdAsync(id);

        if (cliente is null)
            return false;

        cliente.Name = name;
        cliente.Surname = surname;
        cliente.Username = username;
        cliente.Email = email;
        cliente.Phone = phone;
        cliente.DNI = dni;
        cliente.BirthDate = birthDate;
        cliente.Address = address;
        cliente.City = city;
        cliente.PostalCode = postalCode;
        cliente.IBAN = iban;

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
