using System.ComponentModel.DataAnnotations;

namespace Back.DTOs
{
    public class ArcaSolicitudCaeDTO : IValidatableObject
    {
        [Range(1, int.MaxValue)]
        public int TipoComprobante { get; set; } = 11;

        [Range(0, 99)]
        public int TipoDocumentoReceptor { get; set; } = 99;

        [Range(0, long.MaxValue)]
        public long NumeroDocumentoReceptor { get; set; }

        [StringLength(150)]
        public string? NombreReceptor { get; set; }

        [StringLength(200)]
        public string? DomicilioReceptor { get; set; }

        public decimal ImporteTotal { get; set; }

        public DateOnly? FechaComprobante { get; set; }

        [StringLength(150)]
        public string? PacienteNombre { get; set; }

        [StringLength(20)]
        public string? PacienteDni { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (TipoDocumentoReceptor == 99 &&
                NumeroDocumentoReceptor != 0)
            {
                yield return new ValidationResult(
                    "Para facturas C con Consumidor Final (TipoDocumento=99), el número de documento debe ser 0.",
                    new[] { nameof(NumeroDocumentoReceptor) }
                );
            }

            if (TipoDocumentoReceptor == 80 &&
                string.IsNullOrWhiteSpace(PacienteNombre))
            {
                yield return new ValidationResult(
                    "Cuando el receptor es una obra social, debe indicarse el paciente correspondiente.",
                    new[] { nameof(PacienteNombre) }
                );
            }
        }
    }
}