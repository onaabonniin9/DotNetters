
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenteFit.Data.Configurations;

public class EncargadoConfiguration : IEntityTypeConfiguration<Encargado>
{
    public void Configure(EntityTypeBuilder<Encargado> builder)
    {
        builder.ToTable("encargados");

        builder.HasBaseType<Usuario>();
    }
}
