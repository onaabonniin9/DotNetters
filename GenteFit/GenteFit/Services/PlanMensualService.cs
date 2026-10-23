
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class PlanMensualService
{
    private readonly GenteFitContext _context;

    public PlanMensualService(GenteFitContext context)
    {
        _context = context;
    }

    public async Task<PlanMensual> CrearAsync(PlanMensual planMensual)
    {
        _context.Set<PlanMensual>().Add(planMensual);

        await _context.SaveChangesAsync();

        return planMensual;
    }

    public async Task<List<PlanMensual>> ConsultarTodasAsync()
    {
        return await _context.Set<PlanMensual>().ToListAsync();
    }

    public async Task<PlanMensual?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<PlanMensual>()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        string name,
        string description,
        int classesPerWeek,
        decimal monthlyFee)
    {
        var planMensual = await ConsultarPorIdAsync(id);

        if (planMensual is null)
            return false;

        planMensual.Name = name;
        planMensual.Description = description;
        planMensual.ClassesPerWeek = classesPerWeek;
        planMensual.MonthlyFee = monthlyFee;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var planMensual = await ConsultarPorIdAsync(id);

        if (planMensual is null)
            return false;

        _context.Set<PlanMensual>().Remove(planMensual);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var planMensual = await ConsultarPorIdAsync(id);

        if (planMensual is null)
            return false;

        planMensual.Active = false;

        await _context.SaveChangesAsync();

        return true;
    }

}
