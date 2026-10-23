
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Data;

public class GenteFitContext : DbContext
{
    public GenteFitContext(DbContextOptions<GenteFitContext> options)
        : base(options)
    {
    }

    // Usuarios
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<GenteFit.Models.Monitor> Monitores { get; set; }
    public DbSet<Recepcionista> Recepcionistas { get; set; }
    public DbSet<Encargado> Encargados { get; set; }
    public DbSet<Administrador> Administradores { get; set; }

    // Actividades y reservas
    public DbSet<Actividad> Actividades { get; set; }
    public DbSet<Sala> Salas { get; set; }
    public DbSet<Sesion> Sesiones { get; set; }
    public DbSet<Reserva> Reservas { get; set; }
    public DbSet<ListaEspera> ListasEspera { get; set; }

    // Suscripciones y facturación
    public DbSet<PlanMensual> PlanesMensuales { get; set; }
    public DbSet<Suscripcion> Suscripciones { get; set; }
    public DbSet<Factura> Facturas { get; set; }
    public DbSet<Pago> Pagos { get; set; }

    // Integración con Odoo
    public DbSet<IntegracionOdoo> IntegracionesOdoo { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(GenteFitContext).Assembly);
    }
}
