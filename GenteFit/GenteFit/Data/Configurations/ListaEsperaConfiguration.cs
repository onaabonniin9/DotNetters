
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenteFit.Data.Configurations;

public class ListaEsperaConfiguration : IEntityTypeConfiguration<ListaEspera>
{
    public void Configure(EntityTypeBuilder<ListaEspera> builder)
    {
        builder.ToTable("listas_de_espera");

        // Clave primaria
        builder.HasKey(le => le.Id);

        // Propiedades
        builder.Property(le => le.Position)
            .IsRequired();

        builder.Property(le => le.JoinedAt)
            .IsRequired();

        builder.Property(le => le.Status)
            .HasConversion<string>()
            .IsRequired();

        // Relación con Cliente
        builder.HasOne(le => le.Cliente)
            .WithMany()
            .HasForeignKey("ClienteId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Relación con Sesion
        builder.HasOne(le => le.Sesion)
            .WithMany()
            .HasForeignKey("SesionId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
