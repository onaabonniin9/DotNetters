
using System.Data;
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

    private static void ValidarDatos(
        TimeOnly horaInicio,
        TimeOnly horaFin,
        int maxParticipants)
    {
        if (horaFin <= horaInicio)
            throw new ArgumentException(
                "La hora de finalización debe ser posterior a la hora de inicio.");

        if (maxParticipants <= 0)
            throw new ArgumentException(
                "El número máximo de participantes debe ser mayor que cero.");
    }

    public async Task<Sesion> CrearAsync(Sesion sesion)
    {
        if (sesion is null)
            throw new ArgumentNullException(nameof(sesion));

        ValidarDatos(
            sesion.HourStart,
            sesion.HourEnd,
            sesion.MaxParticipants);

        if (sesion.Status != EstadoSesion.PROGRAMADA)
            throw new InvalidOperationException(
                "Una nueva sesión debe estar programada.");

        if (sesion.Monitor is null ||
            sesion.Sala is null ||
            sesion.Actividad is null)
            throw new ArgumentException(
                "La sesión debe tener un monitor, una sala y una actividad.");

        var monitor = await _context.Monitores
            .FindAsync(sesion.Monitor.Id);

        if (monitor is null)
            throw new ArgumentException(
                "El monitor indicado no existe.");

        var sala = await _context.Salas
            .FindAsync(sesion.Sala.Id);

        if (sala is null)
            throw new ArgumentException(
                "La sala indicada no existe.");

        var actividad = await _context.Actividades
            .FindAsync(sesion.Actividad.Id);

        if (actividad is null)
            throw new ArgumentException(
                "La actividad indicada no existe.");

        if (!sala.Active)
            throw new InvalidOperationException(
                "No se puede programar una sesión en una sala inactiva.");

        if (!actividad.Active)
            throw new InvalidOperationException(
                "No se puede programar una actividad inactiva.");

        if (sesion.MaxParticipants > sala.Capacity)
            throw new InvalidOperationException(
                "El número máximo de participantes no puede superar la capacidad de la sala.");

        sesion.Monitor = monitor;
        sesion.Sala = sala;
        sesion.Actividad = actividad;

        _context.Sesiones.Add(sesion);

        await _context.SaveChangesAsync();

        return sesion;
    }

    public async Task<List<Sesion>> ConsultarTodasAsync()
    {
        return await _context.Sesiones
            .AsNoTracking()
            .Include(s => s.Monitor)
            .Include(s => s.Sala)
            .Include(s => s.Actividad)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.HourStart)
            .ToListAsync();
    }

    public async Task<Sesion?> ConsultarPorIdAsync(int id)
    {
        return await _context.Sesiones
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
        ValidarDatos(horaInicio, horaFin, maxParticipants);

        var sesion = await ConsultarPorIdAsync(id);

        if (sesion is null)
            return false;

        if (sesion.Status != EstadoSesion.PROGRAMADA)
            throw new InvalidOperationException(
                "Solo se pueden modificar sesiones programadas.");

        if (maxParticipants > sesion.Sala.Capacity)
            throw new InvalidOperationException(
                "El número máximo de participantes no puede superar la capacidad de la sala.");

        int reservasConfirmadas = await _context.Reservas.CountAsync(r =>
            r.Sesion.Id == id &&
            r.Status == EstadoReserva.CONFIRMADA);

        if (maxParticipants < reservasConfirmadas)
            throw new InvalidOperationException(
                "No se puede reducir la capacidad por debajo del número de reservas confirmadas.");

        sesion.Date = fecha;
        sesion.HourStart = horaInicio;
        sesion.HourEnd = horaFin;
        sesion.MaxParticipants = maxParticipants;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var sesion = await _context.Sesiones
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sesion is null)
            return false;

        bool tieneReservas = await _context.Reservas
            .AnyAsync(r => r.Sesion.Id == id);

        bool tieneListaEspera = await _context.ListasEspera
            .AnyAsync(l => l.Sesion.Id == id);

        if (tieneReservas || tieneListaEspera)
            throw new InvalidOperationException(
                "No se puede eliminar una sesión que tiene reservas o entradas de lista de espera. Debe cancelarse para conservar su historial.");

        _context.Sesiones.Remove(sesion);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CancelarAsync(int id)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var sesion = await _context.Sesiones
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sesion is null ||
            sesion.Status != EstadoSesion.PROGRAMADA)
            return false;

        sesion.Status = EstadoSesion.CANCELADA;

        var reservas = await _context.Reservas
            .Where(r =>
                r.Sesion.Id == id &&
                r.Status == EstadoReserva.CONFIRMADA)
            .ToListAsync();

        foreach (var reserva in reservas)
        {
            reserva.Status = EstadoReserva.CANCELADA_POR_CENTRO;
            reserva.CancelledAt = DateTime.UtcNow;
        }

        var entradas = await _context.ListasEspera
            .Where(l =>
                l.Sesion.Id == id &&
                l.Status == EstadoListaEspera.EN_ESPERA)
            .ToListAsync();

        foreach (var entrada in entradas)
        {
            entrada.Status = EstadoListaEspera.RETIRADO;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }
}
