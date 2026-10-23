
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenteFit.Data.Configurations;

public class SesionConfiguration : IEntityTypeConfiguration<Sesion>
{
    public void Configure(EntityTypeBuilder<Sesion> builder)
    {
        builder.ToTable("sesiones");

        // Clave primaria
        builder.HasKey(s => s.Id);

        // Propiedades
        builder.Property(s => s.Date)
            .IsRequired();

        builder.Property(s => s.HourStart)
            .IsRequired();

        builder.Property(s => s.HourEnd)
            .IsRequired();

        builder.Property(s => s.MaxParticipants)
            .HasDefaultValue(16)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .IsRequired();

        // Relación con Monitor
        builder.HasOne(s => s.Monitor)
            .WithMany()
            .HasForeignKey("MonitorId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Relación con Sala
        builder.HasOne(s => s.Sala)
            .WithMany()
            .HasForeignKey("SalaId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Relación con Actividad
        builder.HasOne(s => s.Actividad)
            .WithMany()
            .HasForeignKey("ActividadId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
