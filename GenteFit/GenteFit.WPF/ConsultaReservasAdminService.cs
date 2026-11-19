using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.WPF.Services;

public sealed record ReservaAdminResumen(
    int Id,
    int SesionId,
    string Actividad,
    string Fecha,
    string Hora,
    string Cliente,
    string Usuario,
    string Estado,
    string FechaReserva);

public sealed record ListaEsperaAdminResumen(
    int Id,
    int SesionId,
    string Actividad,
    string Fecha,
    string Hora,
    string Cliente,
    string Usuario,
    int Posicion,
    string Estado);

public sealed class ConsultaReservasAdminService
{
    public async Task<(List<ReservaAdminResumen> Reservas,
        List<ListaEsperaAdminResumen> Listas)> ConsultarAsync(
        UsuarioAutenticado usuarioActual)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);
        usuarioActual.ExigirAdministrador();

        var factory = new GenteFitContextFactory();
        await using var contexto = factory.CreateDbContext(Array.Empty<string>());
        await AutorizacionService.ExigirAdministradorActivoAsync(contexto, usuarioActual);

        // Solo lectura. No modifica el flujo de reservas implementado por Ona.
        var reservas = await contexto.Reservas
            .AsNoTracking()
            .Include(r => r.Cliente)
            .Include(r => r.Sesion)
                .ThenInclude(s => s.Actividad)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var listas = await contexto.ListasEspera
            .AsNoTracking()
            .Include(l => l.Cliente)
            .Include(l => l.Sesion)
                .ThenInclude(s => s.Actividad)
            .OrderBy(l => l.Sesion.Id)
            .ThenBy(l => l.Position)
            .ToListAsync();

        var reservasVista = reservas.Select(r => new ReservaAdminResumen(
            r.Id,
            r.Sesion.Id,
            r.Sesion.Actividad?.Name ?? "Actividad no disponible",
            r.Sesion.Date.ToString("dd/MM/yyyy"),
            r.Sesion.HourStart.ToString("HH:mm"),
            $"{r.Cliente.Name} {r.Cliente.Surname}".Trim(),
            r.Cliente.Username,
            r.Status.ToString().Replace('_', ' '),
            r.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")))
            .ToList();

        var listasVista = listas.Select(l => new ListaEsperaAdminResumen(
            l.Id,
            l.Sesion.Id,
            l.Sesion.Actividad?.Name ?? "Actividad no disponible",
            l.Sesion.Date.ToString("dd/MM/yyyy"),
            l.Sesion.HourStart.ToString("HH:mm"),
            $"{l.Cliente.Name} {l.Cliente.Surname}".Trim(),
            l.Cliente.Username,
            l.Position,
            l.Status.ToString().Replace('_', ' ')))
            .ToList();

        return (reservasVista, listasVista);
    }
}
