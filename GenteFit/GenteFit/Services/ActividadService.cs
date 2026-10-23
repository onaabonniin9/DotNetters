
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class ActividadService
{
    private readonly GenteFitContext _context;

    public ActividadService(GenteFitContext context)
    {
        _context = context;
    }

    public async Task<Actividad> CrearAsync(Actividad actividad)
    {
        if (string.IsNullOrWhiteSpace(actividad.Name))
            throw new ArgumentException("Error: falta añadir un nobmre");

        if (actividad.Duration <= 0)
            throw new ArgumentException("Error: la duracion no puede ser 0");

        _context.Set<Actividad>().Add(actividad);

        await _context.SaveChangesAsync();

        return actividad;
    }

    public async Task<List<Actividad>> ConsultarTodasAsync()
    {
        return await _context.Set<Actividad>()
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<Actividad?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<Actividad>()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        string name,
        string description,
        NivelIntensidad level,
        int duration)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Error: falta añadir un nobmre");

        if (duration <= 0)
            throw new ArgumentException("Error: la duracion no puede ser 0");

        var actividad = await ConsultarPorIdAsync(id);

        if (actividad is null)
            return false;

        actividad.Name = name;
        actividad.Description = description;
        actividad.Level = level;
        actividad.Duration = duration;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var actividad = await ConsultarPorIdAsync(id);

        if (actividad is null)
            return false;

        _context.Set<Actividad>().Remove(actividad);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DesactivarAsync(int id) // Por si queremos desactivar en vez de eliminar el registro
    {
        var actividad = await ConsultarPorIdAsync(id);

        if (actividad is null)
            return false;

        actividad.Active = false;

        await _context.SaveChangesAsync();

        return true;
    }
}
