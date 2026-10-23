
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class SalaService
{
    private readonly GenteFitContext _context;

    public SalaService(GenteFitContext context)
    {
        _context = context;
    }

    public async Task<Sala> CrearAsync(Sala sala)
    {
        if (string.IsNullOrWhiteSpace(sala.Name))
            throw new ArgumentException("Error: falta añadir un nobmre");

        _context.Set<Sala>().Add(sala);

        await _context.SaveChangesAsync();

        return sala;
    }

    public async Task<List<Sala>> ConsultarTodasAsync()
    {
        return await _context.Set<Sala>()
            .OrderBy(s => s.Name) // Ordenamos por alfabeto pero lo podemos canviar de menor a mayor capacidad
            .ToListAsync();
    }

    public async Task<Sala?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<Sala>()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        string name,
        int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Error: falta añadir un nobmre");

        var sala = await ConsultarPorIdAsync(id);

        if (sala is null)
            return false;

        sala.Name = name;
        sala.Capacity = capacity;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var sala = await ConsultarPorIdAsync(id);

        if (sala is null)
            return false;

        _context.Set<Sala>().Remove(sala);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var sala = await ConsultarPorIdAsync(id);

        if (sala is null)
            return false;

        sala.Active = false;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ActivarAsync(int id)
    {
        var sala = await ConsultarPorIdAsync(id);

        if (sala is null)
            return false;

        sala.Active = true;

        await _context.SaveChangesAsync();

        return true;
    }
}
