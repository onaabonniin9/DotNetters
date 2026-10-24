
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
        var cliente = await _context.Clientes.FindAsync(clienteId);

        if (cliente is null)
            throw new ArgumentException("El cliente  no existe.");

        var sesion = await _context.Sesiones.FindAsync(sesionId);

        if (sesion is null)
            throw new ArgumentException("La sesión no existe.");


        bool yaEstaEnLista = await _context.Set<ListaEspera>().AnyAsync(l =>
    l.Cliente.Id == clienteId &&
    l.Sesion.Id == sesionId &&
    l.Status == EstadoListaEspera.EN_ESPERA);

        if (yaEstaEnLista)
            throw new ArgumentException("Error: El cliente ya está en la lista de espera para esta sesión.");

        int ultimaPosicion = await _context.ListasEspera // calculamos la posición
            .Where(l => l.Sesion.Id == sesionId && l.Status == EstadoListaEspera.EN_ESPERA)
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

        return entrada;
    }


    public async Task<List<ListaEspera>> ConsultarTodasAsync()
    {
        return await _context.Set<ListaEspera>()
            .Include(l => l.Cliente)
            .Include(l => l.Sesion)
            .OrderBy(l => l.Sesion.Id)
            .ThenBy(l => l.Position)
            .ToListAsync();
    }

    public async Task<ListaEspera?> ConsultarPorIdAsync(int id)
    {
        return await _context.Set<ListaEspera>()
            .Include(l => l.Cliente)
            .Include(l => l.Sesion)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<ListaEspera>> ConsultarPorSesionAsync(int sesionId)
    {
        return await _context.Set<ListaEspera>()
            .Include(l => l.Cliente)
            .Include(l => l.Sesion)
            .Where(l => l.Sesion.Id == sesionId &&
                        l.Status == EstadoListaEspera.EN_ESPERA)
            .OrderBy(l => l.Position)
            .ToListAsync();
    }

    public async Task<bool> ActualizarPosicionAsync(
        int id,
        int nuevaPosicion)
    {
        if (nuevaPosicion <= 0)
            throw new ArgumentException("Error: La posición no pude ser 0");

        var entrada = await _context.Set<ListaEspera>()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entrada is null ||
            entrada.Status != EstadoListaEspera.EN_ESPERA)
            return false;

        entrada.Position = nuevaPosicion;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> PromocionarAsync(int id)
    {
        var entrada = await _context.Set<ListaEspera>()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entrada is null ||
            entrada.Status != EstadoListaEspera.EN_ESPERA)
            return false;

        entrada.Status = EstadoListaEspera.PROMOCIONADO;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var entrada = await _context.Set<ListaEspera>()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entrada is null)
            return false;

        _context.Set<ListaEspera>().Remove(entrada);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> AbandonarAsync(int id)
    {
        var entrada = await _context.Set<ListaEspera>()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entrada is null ||
            entrada.Status != EstadoListaEspera.EN_ESPERA)
            return false;

        entrada.Status = EstadoListaEspera.RETIRADO;

        await _context.SaveChangesAsync();

        return true;
    }
}
