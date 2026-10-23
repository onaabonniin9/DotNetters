
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenteFit.Data.Configurations;

public class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.ToTable("reservas");

        // Clave primaria
        builder.HasKey(r => r.Id);

        // Propiedades
        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.CancelledAt)
            .IsRequired(false);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .IsRequired();

        // Relación con Cliente
        builder.HasOne(r => r.Cliente)
            .WithMany()
            .HasForeignKey("ClienteId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Relación con Sesion
        builder.HasOne(r => r.Sesion)
            .WithMany()
            .HasForeignKey("SesionId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
