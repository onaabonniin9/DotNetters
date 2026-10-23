
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class SuscripcionService
{
    private readonly GenteFitContext _context;

    public SuscripcionService(GenteFitContext context)
    {
        _context = context;
    }

    public async Task<Suscripcion> CrearAsync(Suscripcion suscripcion)
    {
        _context.Set<Suscripcion>().Add(suscripcion);

        await _context.SaveChangesAsync();

        return suscripcion;
    }

    public async Task<List<Suscripcion>> ConsultarTodasAsync()
    {
        return await _context.Set<Suscripcion>()
            .Include(s => s.Cliente)
            .Include(s => s.PlanMensual)
            .ToListAsync();
    }

    public async Task<Suscripcion?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<Suscripcion>()
            .Include(s => s.Cliente)
            .Include(s => s.PlanMensual)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        DateOnly startDate,
        DateOnly? endDate,
        EstadoSuscripcion status,
        int billingDay,
        DateTime? cancellationRequestedAt,
        int clienteId,
        int planMensualId)
    {
        var suscripcion = await ConsultarPorIdAsync(id);

        if (suscripcion is null)
            return false;

        var cliente = await _context.Set<Cliente>()
            .FindAsync(clienteId);

        var planMensual = await _context.Set<PlanMensual>()
            .FindAsync(planMensualId);

        if (cliente is null || planMensual is null)
            return false;

        suscripcion.StartDate = startDate;
        suscripcion.EndDate = endDate;
        suscripcion.Status = status;
        suscripcion.BillingDay = billingDay;
        suscripcion.CancellationRequestedAt = cancellationRequestedAt;
        suscripcion.Cliente = cliente;
        suscripcion.PlanMensual = planMensual;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var suscripcion = await ConsultarPorIdAsync(id);

        if (suscripcion is null)
            return false;

        _context.Set<Suscripcion>().Remove(suscripcion);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var suscripcion = await ConsultarPorIdAsync(id);

        if (suscripcion is null)
            return false;

        suscripcion.Status = EstadoSuscripcion.FINALIZADA;

        await _context.SaveChangesAsync();

        return true;
    }

}
