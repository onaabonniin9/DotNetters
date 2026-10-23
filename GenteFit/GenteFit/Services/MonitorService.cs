
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class MonitorService
{
    private readonly GenteFitContext _context;

    public MonitorService(GenteFitContext context)
    {
        _context = context;
    }


    public async Task<GenteFit.Models.Monitor> CrearAsync(
        GenteFit.Models.Monitor monitor)
    {
        if (string.IsNullOrWhiteSpace(monitor.Name))
            throw new ArgumentException("Error: falta añadir un nombre");

        if (string.IsNullOrWhiteSpace(monitor.Surname))
            throw new ArgumentException("Error: faltan los apellidos");

        if (string.IsNullOrWhiteSpace(monitor.DNI))
            throw new ArgumentException("Error: falta añadir el DNI");

        _context.Set<GenteFit.Models.Monitor>().Add(monitor);

        await _context.SaveChangesAsync();

        return monitor;
    }

    public async Task<List<GenteFit.Models.Monitor>> ConsultarTodasAsync()
    {
        return await _context.Set<GenteFit.Models.Monitor>()
            .OrderBy(m => m.Surname)
            .ThenBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<GenteFit.Models.Monitor?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<GenteFit.Models.Monitor>()
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        string name,
        string surname,
        string username,
        string email,
        string phone,
        string dni,
        string speciality)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Error: falta añadir un nombre");

        if (string.IsNullOrWhiteSpace(surname))
            throw new ArgumentException("Error: faltan los apellidos");

        if (string.IsNullOrWhiteSpace(dni))
            throw new ArgumentException("Error: falta añadir el DNI");

        var monitor = await ConsultarPorIdAsync(id);

        if (monitor is null)
            return false;

        monitor.Name = name;
        monitor.Surname = surname;
        monitor.Username = username;
        monitor.Email = email;
        monitor.Phone = phone;
        monitor.DNI = dni;
        monitor.Speciality = speciality;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var monitor = await ConsultarPorIdAsync(id);

        if (monitor is null)
            return false;

        _context.Set<GenteFit.Models.Monitor>().Remove(monitor);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var monitor = await ConsultarPorIdAsync(id);

        if (monitor is null)
            return false;

        monitor.Active = false;

        await _context.SaveChangesAsync();

        return true;
    }



}
