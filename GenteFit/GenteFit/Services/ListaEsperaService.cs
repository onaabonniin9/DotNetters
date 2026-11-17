
using System.Data;
using GenteFit.Data;
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Services;

public sealed record ListaEsperaResumen(
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
    int Posicion,
    DateTime JoinedAt,
    EstadoListaEspera Estado
);

public class ListaEsperaService
{
    private readonly GenteFitContext _context;

    public ListaEsperaService(GenteFitContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    private IQueryable<ListaEsperaResumen> ConsultarResumenes()
    {
        return _context.ListasEspera
            .AsNoTracking()
            .Select(l => new ListaEsperaResumen(
                l.Id,
                l.Cliente.Id,
                l.Cliente.Username,
                l.Cliente.Name,
                l.Cliente.Surname,
                l.Sesion.Id,
                l.Sesion.Date,
                l.Sesion.HourStart,
                l.Sesion.HourEnd,
                l.Sesion.Status,
                l.Position,
                l.JoinedAt,
                l.Status
            ));
    }

    public async Task<ListaEsperaResumen> CrearAsync(
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
                "Solo se puede entrar en la lista de espera de sesiones programadas.");

        DateTime ahora = DateTime.Now;
        DateOnly hoy = DateOnly.FromDateTime(ahora);
        TimeOnly horaActual = TimeOnly.FromDateTime(ahora);

        if (sesion.Date < hoy ||
            (sesion.Date == hoy && sesion.HourStart <= horaActual))
        {
            throw new InvalidOperationException(
                "No puedes apuntarte a una sesión que ya ha comenzado.");
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

        if (reservasConfirmadas < sesion.MaxParticipants)
            throw new InvalidOperationException(
                "Todavía hay plazas disponibles. Debes realizar una reserva normal.");

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

        var resultado = await ConsultarResumenes()
            .FirstAsync(l => l.Id == entrada.Id);

        await transaction.CommitAsync();

        return resultado;
    }

    public async Task<List<ListaEsperaResumen>> ConsultarTodasAsync(
        UsuarioAutenticado usuarioActual)
    {
        await AutorizacionService.ExigirAdministradorActivoAsync(
            _context, usuarioActual);

        return await ConsultarResumenes()
            .OrderBy(l => l.SesionId)
            .ThenBy(l => l.Posicion)
            .ToListAsync();
    }

    public async Task<List<ListaEsperaResumen>> ConsultarMiasAsync(
        UsuarioAutenticado usuarioActual)
    {
        int clienteId =
            await AutorizacionService.ExigirClienteActivoAsync(
                _context, usuarioActual);

        return await ConsultarResumenes()
            .Where(l => l.ClienteId == clienteId)
            .OrderByDescending(l => l.JoinedAt)
            .ToListAsync();
    }

    public async Task<List<ListaEsperaResumen>> ConsultarPorSesionAsync(
        int sesionId,
        UsuarioAutenticado usuarioActual)
    {
        await AutorizacionService.ExigirAdministradorActivoAsync(
            _context, usuarioActual);

        return await ConsultarResumenes()
            .Where(l =>
                l.SesionId == sesionId &&
                l.Estado == EstadoListaEspera.EN_ESPERA)
            .OrderBy(l => l.Posicion)
            .ThenBy(l => l.JoinedAt)
            .ToListAsync();
    }

    public async Task<ListaEsperaResumen?> ConsultarPorIdAsync(
        int id,
        UsuarioAutenticado usuarioActual)
    {
        if (usuarioActual.Tipo == TipoCuentaUsuario.Administrador)
        {
            await AutorizacionService.ExigirAdministradorActivoAsync(
                _context, usuarioActual);

            return await ConsultarResumenes()
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        if (usuarioActual.Tipo == TipoCuentaUsuario.Cliente)
        {
            int clienteId =
                await AutorizacionService.ExigirClienteActivoAsync(
                    _context, usuarioActual);

            return await ConsultarResumenes()
                .FirstOrDefaultAsync(l =>
                    l.Id == id &&
                    l.ClienteId == clienteId);
        }

        throw new UnauthorizedAccessException(
            "No tienes permisos para consultar esta entrada.");
    }

    public async Task<bool> ActualizarPosicionAsync(
        int id,
        int nuevaPosicion,
        UsuarioAutenticado usuarioActual)
    {
        await AutorizacionService.ExigirAdministradorActivoAsync(
            _context, usuarioActual);

        if (nuevaPosicion <= 0)
            throw new ArgumentException(
                "La posición debe ser mayor que cero.");

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var entrada = await _context.ListasEspera
            .Include(l => l.Sesion)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entrada is null ||
            entrada.Status != EstadoListaEspera.EN_ESPERA)
        {
            return false;
        }

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
        await transaction.CommitAsync();

        return true;
    }

    public async Task<bool> PromocionarAsync(
        int id,
        UsuarioAutenticado usuarioActual)
    {
        await AutorizacionService.ExigirAdministradorActivoAsync(
            _context, usuarioActual);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var entrada = await _context.ListasEspera
            .Include(l => l.Cliente)
            .Include(l => l.Sesion)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entrada is null ||
            entrada.Status != EstadoListaEspera.EN_ESPERA)
        {
            return false;
        }

        var sesion = entrada.Sesion;

        if (sesion.Status != EstadoSesion.PROGRAMADA)
            throw new InvalidOperationException(
                "No se puede promocionar una entrada de una sesión no programada.");

        DateTime ahora = DateTime.Now;
        DateOnly hoy = DateOnly.FromDateTime(ahora);
        TimeOnly horaActual = TimeOnly.FromDateTime(ahora);

        if (sesion.Date < hoy ||
            (sesion.Date == hoy && sesion.HourStart <= horaActual))
        {
            throw new InvalidOperationException(
                "No se puede promocionar a un cliente a una sesión que ya ha comenzado.");
        }

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

            await ReordenarPosicionesAsync(sesion.Id);
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

        await ReordenarPosicionesAsync(sesion.Id);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }

    public async Task<bool> AbandonarAsync(
        int id,
        UsuarioAutenticado usuarioActual)
    {
        int clienteId =
            await AutorizacionService.ExigirClienteActivoAsync(
                _context, usuarioActual);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var entrada = await _context.ListasEspera
            .Include(l => l.Sesion)
            .FirstOrDefaultAsync(l =>
                l.Id == id &&
                l.Cliente.Id == clienteId);

        if (entrada is null ||
            entrada.Status != EstadoListaEspera.EN_ESPERA)
        {
            return false;
        }

        int sesionId = entrada.Sesion.Id;

        entrada.Status = EstadoListaEspera.RETIRADO;

        await ReordenarPosicionesAsync(sesionId);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }

    public async Task<bool> RetirarAsync(
        int id,
        UsuarioAutenticado usuarioActual)
    {
        await AutorizacionService.ExigirAdministradorActivoAsync(
            _context, usuarioActual);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var entrada = await _context.ListasEspera
            .Include(l => l.Sesion)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entrada is null ||
            entrada.Status != EstadoListaEspera.EN_ESPERA)
        {
            return false;
        }

        int sesionId = entrada.Sesion.Id;

        entrada.Status = EstadoListaEspera.RETIRADO;

        await ReordenarPosicionesAsync(sesionId);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

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
    }
}
