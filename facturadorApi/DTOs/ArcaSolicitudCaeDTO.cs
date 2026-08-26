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

        public decimal ImporteTotal { get; set; }

        public DateOnly? FechaComprobante { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Si TipoDocumentoReceptor es 99 (Consumidor Final), NumeroDocumentoReceptor debe ser 0
            if (TipoDocumentoReceptor == 99 && NumeroDocumentoReceptor != 0)
            {
                yield return new ValidationResult(
                    "Para facturas C con Consumidor Final (TipoDocumento=99), el número de documento debe ser 0.",
                    new[] { nameof(NumeroDocumentoReceptor) });
            }
        }
    }
}