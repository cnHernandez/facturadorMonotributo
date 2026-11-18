using System.ComponentModel.DataAnnotations;
using Back.Extensions;
using Back.Models;

namespace Back.DTOs
{
    public class ObraSocialDTO : IValidatableObject
    {
        [Required(ErrorMessage = "El CUIT es requerido.")]
        [Cuit(ErrorMessage = "El CUIT debe contener exactamente 11 dígitos.")]
        public string Cuit { get; set; } = null!;

        [Required(ErrorMessage = "El nombre es requerido.")]
        [LettersOnly(ErrorMessage = "El nombre solo puede contener letras, espacios y guiones.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El domicilio comercial es requerido.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "El domicilio debe tener entre 5 y 200 caracteres.")]
        public string DomicilioComercial { get; set; } = null!;

        [Required(ErrorMessage = "La condición de IVA es requerida.")]
        public CondicionObraSocial Condicion { get; set; }

        [Required(ErrorMessage = "La condición de IVA es requerida.")]
        [EnumDataType(typeof(CondicionIVA), ErrorMessage = "La condición de IVA no es válida.")]
        public CondicionIVA? CondicionIVA { get; set; }

        public bool CondicionIVAConfirmada { get; set; }

        [EmailValidation(ErrorMessage = "El correo electrónico debe ser válido.")]
        [StringLength(150, ErrorMessage = "El correo no debe exceder 150 caracteres.")]
        public string? Mail { get; set; }

        public bool Estado { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (!CondicionIVAConfirmada)
            {
                yield return new ValidationResult(
                    "Debe confirmar que la condición frente al IVA registrada es correcta.",
                    new[] { nameof(CondicionIVAConfirmada) }
                );
            }
        }
    }

    public class ObraSocialResponseDTO : ObraSocialDTO
    {
        public int Id { get; set; }
    }
}