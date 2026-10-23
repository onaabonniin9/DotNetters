
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class AdministradorService
{
    private readonly GenteFitContext _context;

    public AdministradorService(GenteFitContext context)
    {
        _context = context;
    }

    public async Task<Administrador> CrearAsync(Administrador administrador)
    {
        _context.Set<Administrador>().Add(administrador);

        await _context.SaveChangesAsync();

        return administrador;
    }

    public async Task<List<Administrador>> ConsultarTodasAsync()
    {
        return await _context.Set<Administrador>().ToListAsync();
    }

    public async Task<Administrador?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<Administrador>()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        string name,
        string surname,
        string username,
        string email,
        string phone)
    {
        var administrador = await ConsultarPorIdAsync(id);

        if (administrador is null)
            return false;

        administrador.Name = name;
        administrador.Surname = surname;
        administrador.Username = username;
        administrador.Email = email;
        administrador.Phone = phone;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var administrador = await ConsultarPorIdAsync(id);

        if (administrador is null)
            return false;

        _context.Set<Administrador>().Remove(administrador);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var administrador = await ConsultarPorIdAsync(id);

        if (administrador is null)
            return false;

        administrador.Active = false;

        await _context.SaveChangesAsync();

        return true;
    }

}
