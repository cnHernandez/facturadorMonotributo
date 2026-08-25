using Microsoft.EntityFrameworkCore;
using Back.Models;

namespace Back.Data
{
    public static class UniqueConstraintsConfig
    {
        public static void ApplyUniqueConstraints(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Paciente>()
                .HasIndex(p => p.Dni)
                .IsUnique();

            modelBuilder.Entity<ObraSocial>()
                .HasIndex(o => o.Cuit)
                .IsUnique();

            modelBuilder.Entity<Paciente>()
                .HasOne(p => p.ObraSocial)
                .WithMany(o => o.Pacientes)
                .HasForeignKey(p => p.ObraSocialId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
