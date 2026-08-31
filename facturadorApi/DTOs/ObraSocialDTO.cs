using System.ComponentModel.DataAnnotations;
using Back.Models;

namespace Back.DTOs
{
    public class ObraSocialDTO
    {
        [Required, StringLength(13, MinimumLength = 11)]
        public string Cuit { get; set; } = null!;

        [Required, StringLength(150)]
        public string Nombre { get; set; } = null!;

        [Required, StringLength(200)]
        public string DomicilioComercial { get; set; } = null!;

        [Required]
        public CondicionObraSocial Condicion { get; set; }

        public CondicionObraSocialIVA CondicionIVA { get; set; }

        [EmailAddress, StringLength(150)]
        public string? Mail { get; set; }

        public bool Estado { get; set; }
    }

    public class ObraSocialResponseDTO : ObraSocialDTO
    {
        public int Id { get; set; }
    }
}