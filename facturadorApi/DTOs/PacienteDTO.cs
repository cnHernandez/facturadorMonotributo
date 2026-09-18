using System.ComponentModel.DataAnnotations;
using Back.Extensions;

namespace Back.DTOs
{
    public class PacienteDTO
    {
        [Required(ErrorMessage = "El DNI es requerido.")]
        [Dni(ErrorMessage = "El DNI debe contener exactamente 8 dígitos.")]
        public string Dni { get; set; } = null!;

        [Required(ErrorMessage = "El número de afiliado es requerido.")]
        [NumAfiliado(ErrorMessage = "El número de afiliado contiene caracteres inválidos.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "El número de afiliado debe tener entre 1 y 50 caracteres.")]
        public string NumAfiliado { get; set; } = null!;

        [Required(ErrorMessage = "El nombre es requerido.")]
        [LettersOnly(ErrorMessage = "El nombre solo puede contener letras, espacios y guiones.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El apellido es requerido.")]
        [LettersOnly(ErrorMessage = "El apellido solo puede contener letras, espacios y guiones.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres.")]
        public string Apellido { get; set; } = null!;

        [Required(ErrorMessage = "El domicilio es requerido.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "El domicilio debe tener entre 5 y 200 caracteres.")]
        public string Domicilio { get; set; } = null!;

        [Url(ErrorMessage = "La URL de la planilla debe ser válida.")]
        [StringLength(500, ErrorMessage = "La URL de la planilla no debe exceder 500 caracteres.")]
        public string? PlanillaAsistenciaImagenUrl { get; set; }

        [StringLength(500, ErrorMessage = "Las observaciones no deben exceder 500 caracteres.")]
        public string? Observaciones { get; set; }

        public bool Estado { get; set; }
        public int? ObraSocialId { get; set; }
    }

    public class PacienteResponseDTO : PacienteDTO
    {
        public int Id { get; set; }
    }
}