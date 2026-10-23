
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class IntegracionOdooService
{
    private readonly GenteFitContext _context;

    public IntegracionOdooService(GenteFitContext context)
    {
        _context = context;
    }

    public async Task<IntegracionOdoo> CrearAsync(IntegracionOdoo integracion)
    {
        _context.Set<IntegracionOdoo>().Add(integracion);

        await _context.SaveChangesAsync();

        return integracion;
    }

    public async Task<List<IntegracionOdoo>> ConsultarTodasAsync()
    {
        return await _context.Set<IntegracionOdoo>()
            .ToListAsync();
    }

    public async Task<IntegracionOdoo?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<IntegracionOdoo>()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        bool activa,
        DateTime ultimaSincronizacion,
        EstadoIntegracion estado,
        string ultimoError)
    {
        var integracion = await ConsultarPorIdAsync(id);

        if (integracion is null)
            return false;

        integracion.Activa = activa;
        integracion.UltimaSincronizacion = ultimaSincronizacion;
        integracion.Estado = estado;
        integracion.UltimoError = ultimoError;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var integracion = await ConsultarPorIdAsync(id);

        if (integracion is null)
            return false;

        _context.Set<IntegracionOdoo>().Remove(integracion);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var integracion = await ConsultarPorIdAsync(id);

        if (integracion is null)
            return false;

        integracion.Activa = false;

        await _context.SaveChangesAsync();

        return true;
    }

}
