
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class EncargadoService
{
    private readonly GenteFitContext _context;

    public EncargadoService(GenteFitContext context)
    {
        _context = context;
    }

    public async Task<Encargado> CrearAsync(Encargado encargado)
    {
        _context.Set<Encargado>().Add(encargado);

        await _context.SaveChangesAsync();

        return encargado;
    }

    public async Task<List<Encargado>> ConsultarTodasAsync()
    {
        return await _context.Set<Encargado>().ToListAsync();
    }

    public async Task<Encargado?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<Encargado>()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        string name,
        string surname,
        string username,
        string email,
        string phone)
    {
        var encargado = await ConsultarPorIdAsync(id);

        if (encargado is null)
            return false;

        encargado.Name = name;
        encargado.Surname = surname;
        encargado.Username = username;
        encargado.Email = email;
        encargado.Phone = phone;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var encargado = await ConsultarPorIdAsync(id);

        if (encargado is null)
            return false;

        _context.Set<Encargado>().Remove(encargado);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var encargado = await ConsultarPorIdAsync(id);

        if (encargado is null)
            return false;

        encargado.Active = false;

        await _context.SaveChangesAsync();

        return true;
    }

}
