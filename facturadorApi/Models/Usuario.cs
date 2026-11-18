namespace Back.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Rol { get; set; } = "Admin";
        public bool Activo { get; set; } = true;
    }
}
