
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class FacturaService
{
    private readonly GenteFitContext _context;

    public FacturaService(GenteFitContext context)
    {
        _context = context;
    }

    public async Task<Factura> CrearAsync(Factura factura)
    {
        _context.Set<Factura>().Add(factura);

        await _context.SaveChangesAsync();

        return factura;
    }

    public async Task<List<Factura>> ConsultarTodasAsync()
    {
        return await _context.Set<Factura>()
            .Include(f => f.Suscripcion)
            .Include(f => f.Pagos)
            .ToListAsync();
    }

    public async Task<Factura?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<Factura>()
            .Include(f => f.Suscripcion)
            .Include(f => f.Pagos)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        string invoiceNumber,
        DateOnly issueDate,
        DateOnly dueDate,
        decimal totalAmount,
        EstadoFactura status,
        int suscripcionId)
    {
        var factura = await ConsultarPorIdAsync(id);

        if (factura is null)
            return false;

        var suscripcion = await _context.Set<Suscripcion>()
            .FindAsync(suscripcionId);

        if (suscripcion is null)
            return false;

        factura.InvoiceNumber = invoiceNumber;
        factura.IssueDate = issueDate;
        factura.DueDate = dueDate;
        factura.TotalAmount = totalAmount;
        factura.Status = status;
        factura.Suscripcion = suscripcion;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var factura = await ConsultarPorIdAsync(id);

        if (factura is null)
            return false;

        _context.Set<Factura>().Remove(factura);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var factura = await ConsultarPorIdAsync(id);

        if (factura is null)
            return false;

        factura.Status = EstadoFactura.ANULADA;

        await _context.SaveChangesAsync();

        return true;
    }

}
