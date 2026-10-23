
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class PagoService
{
    private readonly GenteFitContext _context;

    public PagoService(GenteFitContext context)
    {
        _context = context;
    }

    public async Task<Pago> CrearAsync(Pago pago)
    {
        _context.Set<Pago>().Add(pago);

        await _context.SaveChangesAsync();

        return pago;
    }

    public async Task<List<Pago>> ConsultarTodasAsync()
    {
        return await _context.Set<Pago>()
            .Include(p => p.Suscripcion)
            .Include(p => p.Factura)
            .ToListAsync();
    }

    public async Task<Pago?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<Pago>()
            .Include(p => p.Suscripcion)
            .Include(p => p.Factura)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        decimal amount,
        DateTime paymentDate,
        EstadoPago status,
        MetodoPago paymentMethod,
        int suscripcionId,
        int facturaId)
    {
        var pago = await ConsultarPorIdAsync(id);

        if (pago is null)
            return false;

        var suscripcion = await _context.Set<Suscripcion>()
            .FindAsync(suscripcionId);

        var factura = await _context.Set<Factura>()
            .FindAsync(facturaId);

        if (suscripcion is null || factura is null)
            return false;

        pago.Amount = amount;
        pago.PaymentDate = paymentDate;
        pago.Status = status;
        pago.PaymentMethod = paymentMethod;
        pago.Suscripcion = suscripcion;
        pago.Factura = factura;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var pago = await ConsultarPorIdAsync(id);

        if (pago is null)
            return false;

        _context.Set<Pago>().Remove(pago);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var pago = await ConsultarPorIdAsync(id);

        if (pago is null)
            return false;

        pago.Status = EstadoPago.CANCELADO;

        await _context.SaveChangesAsync();

        return true;
    }

}
