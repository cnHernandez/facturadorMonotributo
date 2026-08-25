using Microsoft.EntityFrameworkCore;

namespace Back.Data
{
    public static class SeedData
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            // Agregar aquí los datos semilla iniciales
            // Ejemplo:
            // modelBuilder.Entity<Rol>().HasData(
            //     new Rol { Id = 1, Nombre = "Admin", Descripcion = "Administrador del sistema" },
            //     new Rol { Id = 2, Nombre = "Usuario", Descripcion = "Usuario estándar" }
            // );
        }
    }
}
