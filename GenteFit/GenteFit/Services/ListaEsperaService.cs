
using System.Data;
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public class ListaEsperaService
{
    private readonly GenteFitContext _context;

    public ListaEsperaService(GenteFitContext context)
    {
        _context = context;
    }

    public async Task<ListaEspera> CrearAsync(int clienteId, int sesionId)
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
                "Solo se puede entrar en la lista de espera de sesiones programadas.");

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
            throw new ArgumentException(
                "El cliente ya está en la lista de espera para esta sesión.");

        int reservasConfirmadas = await _context.Reservas.CountAsync(r =>
            r.Sesion.Id == sesionId &&
            r.Status == EstadoReserva.CONFIRMADA);

        if (reservasConfirmadas < sesion.MaxParticipants)
            throw new InvalidOperationException(
                "Todavía hay plazas disponibles. Debe realizarse una reserva normal.");

        int ultimaPosicion = await _context.ListasEspera
            .Where(l =>
                l.Sesion.Id == sesionId &&
                l.Status == EstadoListaEspera.EN_ESPERA)
            .Select(l => (int?)l.Position)
            .MaxAsync() ?? 0;

        var entrada = new ListaEspera
        {
            Cliente = cliente,
            Sesion = sesion,
            Position = ultimaPosicion + 1,
            JoinedAt = DateTime.UtcNow,
            Status = EstadoListaEspera.EN_ESPERA
        };

        _context.ListasEspera.Add(entrada);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return entrada;
    }

    public async Task<List<ListaEspera>> ConsultarTodasAsync()
    {
        return await _context.ListasEspera
            .AsNoTracking()
            .Include(l => l.Cliente)
            .Include(l => l.Sesion)
            .OrderBy(l => l.Sesion.Id)
            .ThenBy(l => l.Position)
            .ToListAsync();
    }

    public async Task<ListaEspera?> ConsultarPorIdAsync(int id)
    {
        return await _context.ListasEspera
            .Include(l => l.Cliente)
            .Include(l => l.Sesion)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<ListaEspera>> ConsultarPorSesionAsync(int sesionId)
    {
        return await _context.ListasEspera
            .AsNoTracking()
            .Include(l => l.Cliente)
            .Include(l => l.Sesion)
            .Where(l =>
                l.Sesion.Id == sesionId &&
                l.Status == EstadoListaEspera.EN_ESPERA)
            .OrderBy(l => l.Position)
            .ToListAsync();
    }

    public async Task<List<ListaEspera>> ConsultarPorClienteAsync(int clienteId)
    {
        return await _context.ListasEspera
            .AsNoTracking()
            .Include(l => l.Cliente)
            .Include(l => l.Sesion)
            .Where(l => l.Cliente.Id == clienteId)
            .OrderByDescending(l => l.JoinedAt)
            .ToListAsync();
    }

    public async Task<bool> ActualizarPosicionAsync(
        int id,
        int nuevaPosicion)
    {
        if (nuevaPosicion <= 0)
            throw new ArgumentException(
                "La posición debe ser mayor que cero.");

        var entrada = await _context.ListasEspera
            .Include(l => l.Sesion)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entrada is null ||
            entrada.Status != EstadoListaEspera.EN_ESPERA)
            return false;

        var lista = await _context.ListasEspera
            .Where(l =>
                l.Sesion.Id == entrada.Sesion.Id &&
                l.Status == EstadoListaEspera.EN_ESPERA)
            .OrderBy(l => l.Position)
            .ThenBy(l => l.JoinedAt)
            .ToListAsync();

        if (nuevaPosicion > lista.Count)
            throw new ArgumentException(
                "La posición indicada supera el tamaño de la lista.");

        lista.RemoveAll(l => l.Id == id);
        lista.Insert(nuevaPosicion - 1, entrada);

        for (int i = 0; i < lista.Count; i++)
        {
            lista[i].Position = i + 1;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> PromocionarAsync(int id)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var entrada = await _context.ListasEspera
            .Include(l => l.Cliente)
            .Include(l => l.Sesion)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entrada is null ||
            entrada.Status != EstadoListaEspera.EN_ESPERA)
            return false;

        var sesion = await _context.Sesiones
            .FindAsync(entrada.Sesion.Id);

        if (sesion is null ||
            sesion.Status != EstadoSesion.PROGRAMADA)
            throw new InvalidOperationException(
                "No se puede promocionar una entrada de una sesión no programada.");

        var primero = await _context.ListasEspera
            .Where(l =>
                l.Sesion.Id == sesion.Id &&
                l.Status == EstadoListaEspera.EN_ESPERA)
            .OrderBy(l => l.Position)
            .ThenBy(l => l.JoinedAt)
            .FirstOrDefaultAsync();

        if (primero is null || primero.Id != id)
            throw new InvalidOperationException(
                "Solo se puede promocionar al primer cliente de la lista de espera.");

        int reservasConfirmadas = await _context.Reservas.CountAsync(r =>
            r.Sesion.Id == sesion.Id &&
            r.Status == EstadoReserva.CONFIRMADA);

        if (reservasConfirmadas >= sesion.MaxParticipants)
            throw new InvalidOperationException(
                "No se puede promocionar al cliente porque la sesión sigue completa.");

        bool yaTieneReserva = await _context.Reservas.AnyAsync(r =>
            r.Cliente.Id == entrada.Cliente.Id &&
            r.Sesion.Id == sesion.Id &&
            r.Status == EstadoReserva.CONFIRMADA);

        if (yaTieneReserva)
        {
            entrada.Status = EstadoListaEspera.RETIRADO;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return false;
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

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var entrada = await _context.ListasEspera
            .Include(l => l.Sesion)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entrada is null)
            return false;

        int sesionId = entrada.Sesion.Id;
        bool estabaEsperando =
            entrada.Status == EstadoListaEspera.EN_ESPERA;

        _context.ListasEspera.Remove(entrada);

        await _context.SaveChangesAsync();

        if (estabaEsperando)
            await ReordenarPosicionesAsync(sesionId);

        return true;
    }

    public async Task<bool> AbandonarAsync(int id)
    {
        var entrada = await _context.ListasEspera
            .Include(l => l.Sesion)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entrada is null ||
            entrada.Status != EstadoListaEspera.EN_ESPERA)
            return false;

        int sesionId = entrada.Sesion.Id;

        entrada.Status = EstadoListaEspera.RETIRADO;

        await _context.SaveChangesAsync();

        await ReordenarPosicionesAsync(sesionId);

        return true;
    }

    private async Task ReordenarPosicionesAsync(int sesionId)
    {
        var entradas = await _context.ListasEspera
            .Where(l =>
                l.Sesion.Id == sesionId &&
                l.Status == EstadoListaEspera.EN_ESPERA)
            .OrderBy(l => l.Position)
            .ThenBy(l => l.JoinedAt)
            .ToListAsync();

        for (int i = 0; i < entradas.Count; i++)
        {
            entradas[i].Position = i + 1;
        }

        await _context.SaveChangesAsync();
    }
}
