
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class RecepcionistaService
{
    private readonly GenteFitContext _context;

    public RecepcionistaService(GenteFitContext context)
    {
        _context = context;
    }

    public async Task<Recepcionista> CrearAsync(Recepcionista recepcionista)
    {
        if (string.IsNullOrWhiteSpace(recepcionista.Name))
            throw new ArgumentException("Error: falta añadir un nombre");

        if (string.IsNullOrWhiteSpace(recepcionista.Surname))
            throw new ArgumentException("Error: faltan los apellidos");

        if (string.IsNullOrWhiteSpace(recepcionista.DNI))
            throw new ArgumentException("Error: falta añadir el DNI");

        _context.Set<Recepcionista>().Add(recepcionista);

        await _context.SaveChangesAsync();

        return recepcionista;
    }

    public async Task<List<Recepcionista>> ConsultarTodasAsync()
    {
        return await _context.Set<Recepcionista>()
            .OrderBy(r => r.Surname)
            .ThenBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Recepcionista?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<Recepcionista>()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        string name,
        string surname,
        string username,
        string email,
        string phone,
        string dni)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Error: falta añadir un nombre");

        if (string.IsNullOrWhiteSpace(surname))
            throw new ArgumentException("Error: faltan los apellidos");

        if (string.IsNullOrWhiteSpace(dni))
            throw new ArgumentException("Error: falta añadir el DNI");

        var recepcionista = await ConsultarPorIdAsync(id);

        if (recepcionista is null)
            return false;

        recepcionista.Name = name;
        recepcionista.Surname = surname;
        recepcionista.Username = username;
        recepcionista.Email = email;
        recepcionista.Phone = phone;
        recepcionista.DNI = dni;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var recepcionista = await ConsultarPorIdAsync(id);

        if (recepcionista is null)
            return false;

        _context.Set<Recepcionista>().Remove(recepcionista);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var recepcionista = await ConsultarPorIdAsync(id);

        if (recepcionista is null)
            return false;

        recepcionista.Active = false;

        await _context.SaveChangesAsync();

        return true;
    }


}
