
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenteFit.Data.Configurations;

public class PlanMensualConfiguration : IEntityTypeConfiguration<PlanMensual>
{
    public void Configure(EntityTypeBuilder<PlanMensual> builder)
    {
        builder.ToTable("planes_mensuales");

        // Clave primaria
        builder.HasKey(p => p.Id);

        // Propiedades
        builder.Property(p => p.Name)
            .IsRequired();

        builder.Property(p => p.Description)
            .IsRequired();

        builder.Property(p => p.ClassesPerWeek)
            .IsRequired();

        builder.Property(p => p.MonthlyFee)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.Active)
            .HasDefaultValue(true);

        builder.Property(p => p.DateCreated)
            .IsRequired();
    }
}
