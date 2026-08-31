namespace Back.Models
{
    public class Paciente
    {
        public int Id { get; set; }
        public string Dni { get; set; } = null!;
        public string NumAfiliado { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Apellido { get; set; } = null!;
        public string Domicilio { get; set; } = null!;
        public string? PlanillaAsistenciaImagenUrl { get; set; }
        public string Observaciones { get; set; } = "";
        public bool Estado { get; set; }
        public int? ObraSocialId { get; set; }
        public ObraSocial? ObraSocial { get; set; }
    }
}