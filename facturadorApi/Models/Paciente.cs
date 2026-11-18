using System.ComponentModel.DataAnnotations;

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
        [MaxLength(30)]
        public string? ResponsableVinculo { get; set; }
        [MaxLength(100)]
        public string? ResponsableNombre { get; set; }
        [MaxLength(100)]
        public string? ResponsableApellido { get; set; }
        [MaxLength(8)]
        public string? ResponsableDni { get; set; }
        [MaxLength(200)]
        public string? ResponsableDomicilio { get; set; }
        public CondicionIVA? ResponsableCondicionIVA { get; set; }
        public string? PlanillaAsistenciaImagenUrl { get; set; }
        public string Observaciones { get; set; } = "";
        public bool Estado { get; set; }
        public int? ObraSocialId { get; set; }
        public ObraSocial? ObraSocial { get; set; }
    }
}