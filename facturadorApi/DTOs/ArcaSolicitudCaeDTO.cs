using System.ComponentModel.DataAnnotations;
using System.Globalization;

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

        [StringLength(20)]
        public string? PacienteNumAfiliado { get; set; }

        // Campos nuevos para facturación por sesiones
        [Range(1, int.MaxValue)]
        public int? CantidadSesiones { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? PrecioSesion { get; set; }

        public DateOnly? FechaDesde { get; set; }

        public DateOnly? FechaHasta { get; set; }

        public DateOnly? FechaVencimiento { get; set; }

        /// <summary>
        /// Para OSDE: si es true, ImporteTotal es manual. Si es false, se calcula como CantidadSesiones * PrecioSesion
        /// </summary>
        public bool EsFacturacionManual { get; set; } = false;

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

            // Validar que si no es facturación manual, se deben proporcionar cantidad y precio
            if (!EsFacturacionManual)
            {
                if (!CantidadSesiones.HasValue || CantidadSesiones <= 0)
                {
                    yield return new ValidationResult(
                        "Debe indicar una cantidad de sesiones válida.",
                        new[] { nameof(CantidadSesiones) }
                    );
                }

                if (!PrecioSesion.HasValue || PrecioSesion <= 0)
                {
                    yield return new ValidationResult(
                        "Debe indicar un precio de sesión válido.",
                        new[] { nameof(PrecioSesion) }
                    );
                }

                if (CantidadSesiones.HasValue && PrecioSesion.HasValue)
                {
                    var importeCalculado = CantidadSesiones.Value * PrecioSesion.Value;

                    if (decimal.Round(ImporteTotal, 2, MidpointRounding.AwayFromZero) !=
                        decimal.Round(importeCalculado, 2, MidpointRounding.AwayFromZero))
                    {
                        yield return new ValidationResult(
                            $"El importe total debe ser igual a cantidad de sesiones por precio de sesión ({importeCalculado.ToString("N2", CultureInfo.InvariantCulture)}).",
                            new[] { nameof(ImporteTotal) }
                        );
                    }
                }
            }

            // Validar fechas del período
            if (FechaDesde.HasValue && FechaHasta.HasValue && FechaDesde > FechaHasta)
            {
                yield return new ValidationResult(
                    "La fecha de inicio no puede ser posterior a la fecha de fin del período.",
                    new[] { nameof(FechaDesde) }
                );
            }
        }
    }
}