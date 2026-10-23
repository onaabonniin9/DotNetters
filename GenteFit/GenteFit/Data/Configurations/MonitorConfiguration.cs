
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenteFit.Data.Configurations;

public class MonitorConfiguration : IEntityTypeConfiguration<GenteFit.Models.Monitor>
{
    public void Configure(EntityTypeBuilder<GenteFit.Models.Monitor> builder)
    {
        builder.ToTable("monitores");

        // Herencia de Usuario
        builder.HasBaseType<Usuario>();

        // Propiedades específicas del monitor
        builder.Property(m => m.DNI)
            .IsRequired();

        builder.Property(m => m.Speciality)
            .IsRequired();
    }
}
