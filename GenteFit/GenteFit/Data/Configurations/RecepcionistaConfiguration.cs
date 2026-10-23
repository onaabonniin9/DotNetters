
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenteFit.Data.Configurations;

public class RecepcionistaConfiguration : IEntityTypeConfiguration<Recepcionista>
{
    public void Configure(EntityTypeBuilder<Recepcionista> builder)
    {
        builder.ToTable("recepcionistas");

        // Herencia de Usuario
        builder.HasBaseType<Usuario>();
    }
}
