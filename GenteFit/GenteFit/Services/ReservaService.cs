
using System.Data;
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public sealed record ReservaResumen(
    int Id,
    int ClienteId,
    string ClienteUsername,
    string ClienteNombre,
    string ClienteApellidos,
    int SesionId,
    DateOnly FechaSesion,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    EstadoSesion EstadoSesion,
    DateTime CreatedAt,
    DateTime? CancelledAt,
    EstadoReserva Estado
)
{
    public string ActividadNombre { get; init; } = "Actividad";
}

public sealed class SesionSinPlazasException : InvalidOperationException
{
    public SesionSinPlazasException()
        : base("No hay plazas disponibles. Debes apuntarte a la lista de espera.")
    {
    }
}

public class ReservaService
{
    private readonly GenteFitContext _context;

    public ReservaService(GenteFitContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    private IQueryable<ReservaResumen> ConsultarResumenes(IQueryable<Reserva> consulta)
    {
        return consulta
            .Select(r => new ReservaResumen(
                r.Id,
                r.Cliente.Id,
                r.Cliente.Username,
                r.Cliente.Name,
                r.Cliente.Surname,
                r.Sesion.Id,
                r.Sesion.Date,
                r.Sesion.HourStart,
                r.Sesion.HourEnd,
                r.Sesion.Status,
                r.CreatedAt,
                r.CancelledAt,
                r.Status
            )
            {
                ActividadNombre =
                    r.Sesion.Actividad != null
                        ? r.Sesion.Actividad.Name
                        : "Actividad"
            });
    }

    public async Task<ReservaResumen> CrearAsync(
        int sesionId,
        UsuarioAutenticado usuarioActual)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        int clienteId =
            await AutorizacionService.ExigirClienteActivoAsync(
                _context, usuarioActual);

        var cliente = await _context.Clientes.FindAsync(clienteId);

        if (cliente is null)
            throw new UnauthorizedAccessException(
                "No se ha encontrado la cuenta del cliente.");

        var sesion = await _context.Sesiones.FindAsync(sesionId);

        if (sesion is null)
            throw new ArgumentException(
                "La sesión no existe.");

        if (sesion.Status != EstadoSesion.PROGRAMADA)
            throw new InvalidOperationException(
                "Solo se pueden reservar sesiones programadas.");

        DateTime ahora = DateTime.Now;
        DateOnly hoy = DateOnly.FromDateTime(ahora);
        TimeOnly horaActual = TimeOnly.FromDateTime(ahora);

        if (sesion.Date < hoy ||
            (sesion.Date == hoy && sesion.HourStart <= horaActual))
        {
            throw new InvalidOperationException(
                "No se pueden reservar sesiones que ya han comenzado.");
        }

        if (sesion.MaxParticipants <= 0)
            throw new InvalidOperationException(
                "La capacidad de la sesión no es válida.");

        bool yaTieneReserva = await _context.Reservas.AnyAsync(r =>
            r.Cliente.Id == clienteId &&
            r.Sesion.Id == sesionId &&
            r.Status == EstadoReserva.CONFIRMADA);

        if (yaTieneReserva)
            throw new InvalidOperationException(
                "Ya tienes una reserva confirmada para esta sesión.");

        bool yaEstaEnLista = await _context.ListasEspera.AnyAsync(l =>
            l.Cliente.Id == clienteId &&
            l.Sesion.Id == sesionId &&
            l.Status == EstadoListaEspera.EN_ESPERA);

        if (yaEstaEnLista)
            throw new InvalidOperationException(
                "Ya estás en la lista de espera de esta sesión.");

        int reservasConfirmadas = await _context.Reservas.CountAsync(r =>
            r.Sesion.Id == sesionId &&
            r.Status == EstadoReserva.CONFIRMADA);

        if (reservasConfirmadas >= sesion.MaxParticipants)
            throw new SesionSinPlazasException();

        bool hayListaPendiente = await _context.ListasEspera.AnyAsync(l =>
            l.Sesion.Id == sesionId &&
            l.Status == EstadoListaEspera.EN_ESPERA);

        if (hayListaPendiente)
            throw new InvalidOperationException(
                "Hay clientes esperando. Las nuevas plazas deben asignarse respetando el orden de la lista.");

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

        var resultado = await ConsultarResumenes(_context.Reservas
            .AsNoTracking()
            .Where(r => r.Id == reserva.Id))
            .FirstAsync();

        await transaction.CommitAsync();

        return resultado;
    }

    public async Task<List<ReservaResumen>> ConsultarTodasAsync(
        UsuarioAutenticado usuarioActual)
    {
        await AutorizacionService.ExigirAdministradorActivoAsync(
            _context, usuarioActual);

        return await ConsultarResumenes(_context.Reservas
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt))
            .ToListAsync();
    }

    public async Task<List<ReservaResumen>> ConsultarMiasAsync(
        UsuarioAutenticado usuarioActual)
    {
        int clienteId =
            await AutorizacionService.ExigirClienteActivoAsync(
                _context, usuarioActual);

        return await ConsultarResumenes(_context.Reservas
            .AsNoTracking()
            .Where(r => r.Cliente.Id == clienteId)
            .OrderByDescending(r => r.CreatedAt))
            .ToListAsync();
    }

    public async Task<List<ReservaResumen>> ConsultarPorSesionAsync(
        int sesionId,
        UsuarioAutenticado usuarioActual)
    {
        await AutorizacionService.ExigirAdministradorActivoAsync(
            _context, usuarioActual);

        return await ConsultarResumenes(_context.Reservas
            .AsNoTracking()
            .Where(r => r.Sesion.Id == sesionId)
            .OrderBy(r => r.CreatedAt))
            .ToListAsync();
    }

    public async Task<ReservaResumen?> ConsultarPorIdAsync(
        int id,
        UsuarioAutenticado usuarioActual)
    {
        if (usuarioActual.Tipo == TipoCuentaUsuario.Administrador)
        {
            await AutorizacionService.ExigirAdministradorActivoAsync(
                _context, usuarioActual);

            return await ConsultarResumenes(_context.Reservas
            .AsNoTracking()
            .Where(r => r.Id == id))
                .FirstOrDefaultAsync();
        }

        if (usuarioActual.Tipo == TipoCuentaUsuario.Cliente)
        {
            int clienteId =
                await AutorizacionService.ExigirClienteActivoAsync(
                    _context, usuarioActual);

            return await ConsultarResumenes(_context.Reservas
            .AsNoTracking()
            .Where(r => r.Id == id && r.Cliente.Id == clienteId))
                .FirstOrDefaultAsync();
        }

        throw new UnauthorizedAccessException(
            "No tienes permisos para consultar reservas.");
    }

    public async Task<bool> CancelarAsync(
        int id,
        UsuarioAutenticado usuarioActual)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        int? clienteId = null;
        EstadoReserva estadoCancelacion;

        if (usuarioActual.Tipo == TipoCuentaUsuario.Cliente)
        {
            clienteId =
                await AutorizacionService.ExigirClienteActivoAsync(
                    _context, usuarioActual);

            estadoCancelacion = EstadoReserva.CANCELADA;
        }
        else if (usuarioActual.Tipo == TipoCuentaUsuario.Administrador)
        {
            await AutorizacionService.ExigirAdministradorActivoAsync(
                _context, usuarioActual);

            estadoCancelacion =
                EstadoReserva.CANCELADA_POR_CENTRO;
        }
        else
        {
            throw new UnauthorizedAccessException(
                "No tienes permisos para cancelar reservas.");
        }

        IQueryable<Reserva> consulta = _context.Reservas
            .Include(r => r.Sesion)
            .Where(r =>
                r.Id == id &&
                r.Status == EstadoReserva.CONFIRMADA);

        if (clienteId.HasValue)
        {
            consulta = consulta.Where(r =>
                r.Cliente.Id == clienteId.Value);
        }

        var reserva = await consulta.FirstOrDefaultAsync();

        if (reserva is null)
        {
            await transaction.CommitAsync();
            return false;
        }

        int sesionId = reserva.Sesion.Id;

        reserva.Status = estadoCancelacion;
        reserva.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await PromocionarSiguientesDisponiblesAsync(sesionId);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }

    private async Task PromocionarSiguientesDisponiblesAsync(
    int sesionId)
    {
        var sesion = await _context.Sesiones.FindAsync(sesionId);

        if (sesion is null ||
            sesion.Status != EstadoSesion.PROGRAMADA)
            return;

        DateTime ahora = DateTime.Now;
        DateTime inicioSesion =
            sesion.Date.ToDateTime(sesion.HourStart);

        if (inicioSesion <= ahora)
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

        int posicion = 1;

        foreach (var entrada in pendientes
                     .Where(l => l.Status == EstadoListaEspera.EN_ESPERA)
                     .OrderBy(l => l.Position)
                     .ThenBy(l => l.JoinedAt))
        {
            entrada.Position = posicion++;
        }
    }
}
