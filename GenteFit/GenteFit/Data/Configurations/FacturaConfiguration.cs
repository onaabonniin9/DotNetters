
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenteFit.Data.Configurations;

public class FacturaConfiguration : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> builder)
    {
        builder.ToTable("facturas");

        // Clave primaria
        builder.HasKey(f => f.Id);

        // Propiedades
        builder.Property(f => f.InvoiceNumber)
            .IsRequired();

        builder.Property(f => f.IssueDate)
            .IsRequired();

        builder.Property(f => f.DueDate)
            .IsRequired();

        builder.Property(f => f.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(f => f.Status)
            .HasConversion<string>()
            .IsRequired();

        // Relación con Suscripcion
        builder.HasOne(f => f.Suscripcion)
            .WithMany()
            .HasForeignKey("SuscripcionId")
            .IsRequired();

        // Relación con Pago
        builder.HasMany(f => f.Pagos)
            .WithOne(p => p.Factura)
            .HasForeignKey("FacturaId")
            .IsRequired();
    }
}
