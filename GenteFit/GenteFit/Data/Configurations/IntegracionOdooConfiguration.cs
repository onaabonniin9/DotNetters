
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenteFit.Data.Configurations;

public class IntegracionOdooConfiguration : IEntityTypeConfiguration<IntegracionOdoo>
{
    public void Configure(EntityTypeBuilder<IntegracionOdoo> builder)
    {
        builder.ToTable("integracion_odoo");

        // Clave primaria
        builder.HasKey(i => i.Id);

        // Propiedades
        builder.Property(i => i.Activa)
            .HasDefaultValue(true);

        builder.Property(i => i.UltimaSincronizacion);

        builder.Property(i => i.Estado)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(i => i.UltimoError)
            .IsRequired();
    }
}
