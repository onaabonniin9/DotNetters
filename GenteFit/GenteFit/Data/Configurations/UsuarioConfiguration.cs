
using GenteFit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenteFit.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        // Tabla principal y estrategia de herencia
        builder.UseTptMappingStrategy();
        builder.ToTable("usuarios");

        // Clave primaria
        builder.HasKey(u => u.Id);

        // Datos personales
        builder.Property(u => u.Name)
            .IsRequired();

        builder.Property(u => u.Surname)
            .IsRequired();

        // Datos de acceso
        builder.Property(u => u.Username)
            .IsRequired();

        builder.Property(u => u.Password)
            .IsRequired();

        // Datos de contacto
        builder.Property(u => u.Email)
            .IsRequired();

        builder.Property(u => u.Phone)
            .IsRequired();

        // Estado y fecha de creación
        builder.Property(u => u.Active)
            .HasDefaultValue(true);

        builder.Property(u => u.DateCreated)
            .IsRequired();
    }
}
