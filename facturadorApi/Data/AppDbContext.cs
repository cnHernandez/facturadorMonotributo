using Microsoft.EntityFrameworkCore;
using Back.Models;
using Back.Data;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Back.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    UniqueConstraintsConfig.ApplyUniqueConstraints(modelBuilder);
    SeedData.Configure(modelBuilder);

    // Mail de Obra Social es opcional
    modelBuilder.Entity<ObraSocial>()
        .Property(x => x.Mail)
        .IsRequired(false)
        .HasMaxLength(150);

    // Excluir campos específicos de la conversión automática a mayúsculas
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        foreach (var property in entityType.GetProperties())
        {
            if (property.ClrType == typeof(string) &&
                property.Name == "Password")
            {
                property.SetValueConverter((ValueConverter?)null);
            }

            if (property.ClrType == typeof(string) &&
                property.Name == "Email")
            {
                property.SetValueConverter((ValueConverter?)null);
            }

            // IMPORTANTE: agregar Mail
            if (property.ClrType == typeof(string) &&
                property.Name == "Mail")
            {
                property.SetValueConverter((ValueConverter?)null);
            }

            if (property.ClrType == typeof(string) &&
                property.Name == "UserName")
            {
                property.SetValueConverter((ValueConverter?)null);
            }

            if (property.ClrType == typeof(string) &&
                property.Name == "Notas")
            {
                property.SetValueConverter((ValueConverter?)null);
            }
        }
    }
}
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Aplica el convertidor de capitalización a todos los strings
        configurationBuilder
            .Properties<string>()
            .HaveConversion<CapitalizeStringConverter>();
    }

    public DbSet<Paciente> Pacientes { get; set; }
    public DbSet<ObraSocial> ObrasSociales { get; set; }
}
