
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenteFit.Data.Configurations;

public class SuscripcionConfiguration : IEntityTypeConfiguration<Suscripcion>
{
    public void Configure(EntityTypeBuilder<Suscripcion> builder)
    {
        builder.ToTable("suscripciones");

        // Clave primaria
        builder.HasKey(s => s.Id);

        // Propiedades
        builder.Property(s => s.StartDate)
            .IsRequired();

        builder.Property(s => s.EndDate)
            .IsRequired(false);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(s => s.BillingDay)
            .IsRequired();

        builder.Property(s => s.CancellationRequestedAt)
            .IsRequired(false);

        // Relación con Cliente
        builder.HasOne(s => s.Cliente)
            .WithMany()
            .HasForeignKey("ClienteId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Relación con PlanMensual
        builder.HasOne(s => s.PlanMensual)
            .WithMany()
            .HasForeignKey("PlanMensualId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
