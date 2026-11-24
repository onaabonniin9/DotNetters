int id,
UsuarioAutenticado usuarioActual)
    {
        await AutorizacionService.ExigirAdministradorActivoAsync(
            _context,
    usuarioActual);

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
    
    public async Task<List<Sesion>> ConsultarPorRangoAsync(
        DateOnly desde, DateOnly hasta, UsuarioAutenticado usuarioActual)
{
    await ExigirPermisoConsultaAsync(usuarioActual);

    var consulta = _context.Sesiones
        .AsNoTracking()
        .Include(s => s.Monitor)
        .Include(s => s.Sala)
        .Include(s => s.Actividad)
        .Where(s => s.Date >= desde && s.Date <= hasta);

    if (usuarioActual.Tipo == TipoCuentaUsuario.Monitor)
        consulta = consulta.Where(s => s.Monitor.Id == usuarioActual.UsuarioId);

    return await consulta
    .OrderBy(s => s.Date)
    .ThenBy(s => s.HourStart)
    .ToListAsync();
}

public async Task<List<Reserva>> ConsultarReservasConfirmadasAsync(int sesionId, UsuarioAutenticado usuarioActual)
{
    await ExigirPermisoConsultaAsync(usuarioActual);

    var consulta = _context.Reservas
        .AsNoTracking()
        .Include(r => r.Cliente)
        .Where(r => r.Sesion.Id == sesionId &&
                    r.Status == EstadoReserva.CONFIRMADA);

    if (usuarioActual.Tipo == TipoCuentaUsuario.Monitor)
        consulta = consulta.Where(r => r.Sesion.Monitor.Id == usuarioActual.UsuarioId);

    return await consulta.OrderBy(r => r.CreatedAt).ToListAsync();
}

public async Task<bool> ReprogramarAsync(
    int id,
    UsuarioAutenticado usuarioActual)
{
    await AutorizacionService.ExigirAdministradorActivoAsync(
        _context,
        usuarioActual);

    var sesion = await _context.Sesiones
        .Include(s => s.Monitor)
        .Include(s => s.Sala)
        .Include(s => s.Actividad)
        .FirstOrDefaultAsync(s => s.Id == id);

    if (sesion is null || sesion.Status != EstadoSesion.CANCELADA)
        return false;

    if (!EsFutura(sesion))
        throw new InvalidOperationException(
            "No se puede reprogramar una sesión con fecha u hora pasada.");

    if (!sesion.Monitor.Active || !sesion.Sala.Active || !sesion.Actividad.Active)
        throw new InvalidOperationException(
            "No se puede reprogramar la sesión porque su monitor, sala o actividad están inactivos.");

    if (sesion.MaxParticipants > sesion.Sala.Capacity)
        throw new InvalidOperationException(
            "La capacidad de la sesión supera la capacidad actual de la sala.");

    // Las reservas canceladas y las entradas retiradas conservan su estado histórico.
    sesion.Status = EstadoSesion.PROGRAMADA;
    await _context.SaveChangesAsync();
    return true;
}

}
