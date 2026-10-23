
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class SesionService
{
    private readonly GenteFitContext _context;

    public SesionService(GenteFitContext context)
    {
        _context = context;
    }

    public async Task<Sesion> CrearAsync(Sesion sesion)
    {
        _context.Set<Sesion>().Add(sesion);

        await _context.SaveChangesAsync();

        return sesion;
    }

    public async Task<List<Sesion>> ConsultarTodasAsync()
    {
        return await _context.Set<Sesion>()
            .Include(s => s.Monitor)
            .Include(s => s.Sala)
            .Include(s => s.Actividad)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.HourStart)
            .ToListAsync();
    }

    public async Task<Sesion?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<Sesion>()
            .Include(s => s.Monitor)
            .Include(s => s.Sala)
            .Include(s => s.Actividad)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        int maxParticipants)
    {
        var sesion = await ConsultarPorIdAsync(id);

        if (sesion is null)
            return false;

        sesion.Date = fecha;
        sesion.HourStart = horaInicio;
        sesion.HourEnd = horaFin;
        sesion.MaxParticipants = maxParticipants;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var sesion = await ConsultarPorIdAsync(id);

        if (sesion is null)
            return false;

        _context.Set<Sesion>().Remove(sesion);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CancelarAsync(int id)
    {
        var sesion = await ConsultarPorIdAsync(id);

        if (sesion is null)
            return false;

        sesion.Status = EstadoSesion.CANCELADA;

        await _context.SaveChangesAsync();

        return true;
    }
}
