
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenteFit.Data.Configurations;

public class ActividadConfiguration : IEntityTypeConfiguration<Actividad>
{
    public void Configure(EntityTypeBuilder<Actividad> builder)
    {
        builder.ToTable("actividades");

        // Clave primaria
        builder.HasKey(a => a.Id);

        // Propiedades
        builder.Property(a => a.Name)
            .IsRequired();

        builder.Property(a => a.Description)
            .IsRequired();

        builder.Property(a => a.Level)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(a => a.Duration)
            .IsRequired();

        builder.Property(a => a.Active)
            .HasDefaultValue(true);
    }
}
