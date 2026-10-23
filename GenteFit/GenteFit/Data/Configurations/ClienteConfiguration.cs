
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenteFit.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");

        builder.HasBaseType<Usuario>();

        builder.Property(c => c.DNI)
            .IsRequired();

        builder.Property(c => c.BirthDate)
            .IsRequired();

        builder.Property(c => c.Address)
            .IsRequired();

        builder.Property(c => c.City)
            .IsRequired();

        builder.Property(c => c.PostalCode)
            .IsRequired();

        builder.Property(c => c.IBAN)
            .IsRequired();
    }
}
