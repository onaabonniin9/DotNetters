
using System.Data;
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
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var cliente = await _context.Clientes.FindAsync(clienteId);

        if (cliente is null)
            throw new ArgumentException("El cliente no existe.");

        var sesion = await _context.Sesiones.FindAsync(sesionId);

        if (sesion is null)
            throw new ArgumentException("La sesión no existe.");

        if (sesion.Status != EstadoSesion.PROGRAMADA)
            throw new InvalidOperationException(
                "Solo se pueden reservar sesiones programadas.");

        if (sesion.MaxParticipants <= 0)
            throw new InvalidOperationException(
                "La sesión tiene una capacidad no válida.");

        bool yaTieneReserva = await _context.Reservas.AnyAsync(r =>
            r.Cliente.Id == clienteId &&
            r.Sesion.Id == sesionId &&
            r.Status == EstadoReserva.CONFIRMADA);

        if (yaTieneReserva)
            throw new ArgumentException(
                "El cliente ya tiene una reserva confirmada para esta sesión.");

        bool yaEstaEnLista = await _context.ListasEspera.AnyAsync(l =>
            l.Cliente.Id == clienteId &&
            l.Sesion.Id == sesionId &&
            l.Status == EstadoListaEspera.EN_ESPERA);

        if (yaEstaEnLista)
            throw new InvalidOperationException(
                "El cliente ya está en la lista de espera. Debe respetarse el orden de la lista.");

        int reservasConfirmadas = await _context.Reservas.CountAsync(r =>
            r.Sesion.Id == sesionId &&
            r.Status == EstadoReserva.CONFIRMADA);

        if (reservasConfirmadas >= sesion.MaxParticipants)
            throw new InvalidOperationException(
                "No hay plazas disponibles. El cliente debe apuntarse a la lista de espera.");

        bool hayListaPendiente = await _context.ListasEspera.AnyAsync(l =>
            l.Sesion.Id == sesionId &&
            l.Status == EstadoListaEspera.EN_ESPERA);

        if (hayListaPendiente)
            throw new InvalidOperationException(
                "Hay clientes esperando. La plaza debe asignarse siguiendo el orden de la lista de espera.");

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
        await transaction.CommitAsync();

        return reserva;
    }

    public async Task<List<Reserva>> ConsultarTodasAsync()
    {
        return await _context.Reservas
            .AsNoTracking()
            .Include(r => r.Cliente)
            .Include(r => r.Sesion)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Reserva?> ConsultarPorIdAsync(int id)
    {
        return await _context.Reservas
            .AsNoTracking()
            .Include(r => r.Cliente)
            .Include(r => r.Sesion)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<Reserva>> ConsultarPorClienteAsync(int clienteId)
    {
        return await _context.Reservas
            .AsNoTracking()
            .Include(r => r.Cliente)
            .Include(r => r.Sesion)
            .Where(r => r.Cliente.Id == clienteId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Reserva>> ConsultarPorSesionAsync(int sesionId)
    {
        return await _context.Reservas
            .AsNoTracking()
            .Include(r => r.Cliente)
            .Include(r => r.Sesion)
            .Where(r => r.Sesion.Id == sesionId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ActualizarEstadoAsync(
        int id,
        EstadoReserva nuevoEstado)
    {
        if (nuevoEstado == EstadoReserva.CONFIRMADA)
            throw new InvalidOperationException(
                "Para confirmar una reserva se debe utilizar CrearAsync o promocionar una entrada de la lista de espera.");

        if (nuevoEstado == EstadoReserva.CANCELADA)
            return await CancelarAsync(id);

        var reserva = await _context.Reservas
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null)
            return false;

        if (nuevoEstado == EstadoReserva.CANCELADA_POR_CENTRO)
        {
            reserva.Status = nuevoEstado;
            reserva.CancelledAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        throw new ArgumentException("El estado indicado no es válido.");
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var reserva = await _context.Reservas
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null)
            return false;

        if (reserva.Status == EstadoReserva.CONFIRMADA)
            throw new InvalidOperationException(
                "No se puede eliminar una reserva confirmada. Debe utilizarse CancelarAsync para liberar la plaza y promocionar la lista de espera.");

        _context.Reservas.Remove(reserva);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CancelarAsync(int id)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var reserva = await _context.Reservas
            .Include(r => r.Sesion)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null ||
            reserva.Status != EstadoReserva.CONFIRMADA)
            return false;

        int sesionId = reserva.Sesion.Id;

        reserva.Status = EstadoReserva.CANCELADA;
        reserva.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await PromocionarSiguientesDisponiblesAsync(sesionId);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }

    private async Task PromocionarSiguientesDisponiblesAsync(int sesionId)
    {
        var sesion = await _context.Sesiones.FindAsync(sesionId);

        if (sesion is null ||
            sesion.Status != EstadoSesion.PROGRAMADA)
            return;

        int reservasConfirmadas = await _context.Reservas.CountAsync(r =>
            r.Sesion.Id == sesionId &&
            r.Status == EstadoReserva.CONFIRMADA);

        int plazasDisponibles =
            sesion.MaxParticipants - reservasConfirmadas;

        if (plazasDisponibles <= 0)
            return;

        var pendientes = await _context.ListasEspera
            .Include(l => l.Cliente)
            .Include(l => l.Sesion)
            .Where(l =>
                l.Sesion.Id == sesionId &&
                l.Status == EstadoListaEspera.EN_ESPERA)
            .OrderBy(l => l.Position)
            .ThenBy(l => l.JoinedAt)
            .ToListAsync();

        foreach (var entrada in pendientes)
        {
            if (plazasDisponibles <= 0)
                break;

            bool yaTieneReserva = await _context.Reservas.AnyAsync(r =>
                r.Cliente.Id == entrada.Cliente.Id &&
                r.Sesion.Id == sesionId &&
                r.Status == EstadoReserva.CONFIRMADA);

            if (yaTieneReserva)
            {
                entrada.Status = EstadoListaEspera.RETIRADO;
                continue;
            }

            var nuevaReserva = new Reserva
            {
                Cliente = entrada.Cliente,
                Sesion = sesion,
                CreatedAt = DateTime.UtcNow,
                CancelledAt = null,
                Status = EstadoReserva.CONFIRMADA
            };

            _context.Reservas.Add(nuevaReserva);

            entrada.Status = EstadoListaEspera.PROMOCIONADO;

            plazasDisponibles--;
        }
    }
}
