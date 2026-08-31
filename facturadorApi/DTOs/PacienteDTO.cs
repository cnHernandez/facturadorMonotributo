using System.ComponentModel.DataAnnotations;

namespace Back.DTOs
{
    public class PacienteDTO
    {
        [Required, StringLength(11, MinimumLength = 7)]
        public string Dni { get; set; } = null!;

        [Required, StringLength(50)]
        public string NumAfiliado { get; set; } = null!;

        [Required, StringLength(100)]
        public string Nombre { get; set; } = null!;

        [Required, StringLength(100)]
        public string Apellido { get; set; } = null!;

        [Required, StringLength(200)]
        public string Domicilio { get; set; } = null!;

        [Url, StringLength(500)]
        public string? PlanillaAsistenciaImagenUrl { get; set; }

        public string? Observaciones { get; set; }

        public bool Estado { get; set; }
        public int? ObraSocialId { get; set; }
    }

    public class PacienteResponseDTO : PacienteDTO
    {
        public int Id { get; set; }
    }
}