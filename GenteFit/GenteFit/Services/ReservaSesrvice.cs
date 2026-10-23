
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class ReservaService
{
    private readonly GenteFitContext _context;

    public ReservaService(GenteFitContext context)
    {
        _context = context;
    }

    public async Task<Reserva> CrearAsync(int clienteId, int sesionId)
    {
        var cliente = await _context.Clientes.FindAsync(clienteId);

        if (cliente is null)
            throw new ArgumentException("El cliente  no existe.");

        var sesion = await _context.Sesiones.FindAsync(sesionId);

        if (sesion is null)
            throw new ArgumentException("La sesión no existe.");

        bool yaTieneReserva = await _context.Set<Reserva>().AnyAsync(r => // Asegurarnos de que solo exista una reserva
            r.Cliente.Id == reserva.Cliente.Id &&
            r.Sesion.Id == reserva.Sesion.Id &&
            r.Status == EstadoReserva.CONFIRMADA);

        if (yaTieneReserva)
            throw new ArgumentException("Error: El cliente ya tiene una reserva confirmada para esta sesión.");

        int reservasConfirmadas = await _context.Reservas.CountAsync(r => //comprobamos plazas
            r.Sesion.Id == sesionId &&
            r.Status == EstadoReserva.CONFIRMADA);

        if (reservasConfirmadas >= sesion.MaxParticipants)
            throw new ArgumentException("No hay plazas disponibles.");

        var reserva = new Reserva
        {
            Cliente = cliente,
            Sesion = sesion,
            CreatedAt = DateTime.UtcNow,
            CancelledAt = null,
            Status = EstadoReserva.CONFIRMADA
        };

        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();

        return reserva;
    }


    public async Task<List<Reserva>> ConsultarTodasAsync()
    {
        return await _context.Set<Reserva>()
            .Include(r => r.Cliente)
            .Include(r => r.Sesion)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Reserva?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<Reserva>()
            .Include(r => r.Cliente)
            .Include(r => r.Sesion)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<Reserva>> ConsultarPorClienteAsync(int clienteId)
    {
        return await _context.Set<Reserva>()
            .Include(r => r.Cliente)
            .Include(r => r.Sesion)
            .Where(r => r.Cliente.Id == clienteId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ActualizarEstadoAsync(
        int id,
        EstadoReserva nuevoEstado)
    {
        var reserva = await _context.Set<Reserva>()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null)
            return false;

        reserva.Status = nuevoEstado;

        if (nuevoEstado == EstadoReserva.CONFIRMADA)
        {
            reserva.CancelledAt = null;
        }
        else if (nuevoEstado == EstadoReserva.CANCELADA ||
                 nuevoEstado == EstadoReserva.CANCELADA_POR_CENTRO)
        {
            reserva.CancelledAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var reserva = await _context.Set<Reserva>()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null)
            return false;

        _context.Set<Reserva>().Remove(reserva);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CancelarAsync(int id)
    {
        var reserva = await _context.Set<Reserva>()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null ||
            reserva.Status != EstadoReserva.CONFIRMADA)
            return false;

        reserva.Status = EstadoReserva.CANCELADA;
        reserva.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}
