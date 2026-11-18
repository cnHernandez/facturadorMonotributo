using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Back.Extensions;
using Back.Models;

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

        [LettersOnly(ErrorMessage = "El nombre del receptor solo puede contener letras, espacios y guiones.")]
        [StringLength(150, ErrorMessage = "El nombre del receptor no debe exceder 150 caracteres.")]
        public string? NombreReceptor { get; set; }

        [StringLength(200, ErrorMessage = "El domicilio no debe exceder 200 caracteres.")]
        public string? DomicilioReceptor { get; set; }

        [Required(ErrorMessage = "La condición frente al IVA del receptor es requerida.")]
        [EnumDataType(typeof(CondicionIVA), ErrorMessage = "La condición frente al IVA del receptor no es válida.")]
        public CondicionIVA? CondicionIVAReceptor { get; set; }

        [Required(ErrorMessage = "La condición de venta es requerida.")]
        [EnumDataType(typeof(CondicionObraSocial), ErrorMessage = "La condición de venta no es válida.")]
        public CondicionObraSocial? CondicionVenta { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El importe total debe ser mayor a 0.")]
        public decimal ImporteTotal { get; set; }

        public DateOnly? FechaComprobante { get; set; }

        [LettersOnly(ErrorMessage = "El nombre del paciente solo puede contener letras, espacios y guiones.")]
        [StringLength(150, ErrorMessage = "El nombre del paciente no debe exceder 150 caracteres.")]
        public string? PacienteNombre { get; set; }

        [Dni(ErrorMessage = "El DNI del paciente debe contener exactamente 8 dígitos.")]
        [StringLength(20, ErrorMessage = "El DNI no debe exceder 20 caracteres.")]
        public string? PacienteDni { get; set; }

        [NumAfiliado(ErrorMessage = "El número de afiliado contiene caracteres inválidos.")]
        [StringLength(20, ErrorMessage = "El número de afiliado no debe exceder 20 caracteres.")]
        public string? PacienteNumAfiliado { get; set; }

        [StringLength(500, ErrorMessage = "La descripción del servicio no debe exceder 500 caracteres.")]
        public string? DescripcionServicio { get; set; }

        // Campos nuevos para facturación por sesiones
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad de sesiones debe ser mayor a 0.")]
        public int? CantidadSesiones { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio por sesión debe ser mayor a 0.")]
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
            if (!CondicionIVAReceptor.HasValue ||
                !Enum.IsDefined(CondicionIVAReceptor.Value))
            {
                yield return new ValidationResult(
                    "Debe indicar una condición frente al IVA válida para el receptor.",
                    new[] { nameof(CondicionIVAReceptor) }
                );
            }

            if (!CondicionVenta.HasValue ||
                !Enum.IsDefined(CondicionVenta.Value))
            {
                yield return new ValidationResult(
                    "Debe indicar una condición de venta válida.",
                    new[] { nameof(CondicionVenta) }
                );
            }

            if (TipoDocumentoReceptor == 99 &&
                NumeroDocumentoReceptor != 0)
            {
                yield return new ValidationResult(
                    "Para facturas C con Consumidor Final (TipoDocumento=99), el número de documento debe ser 0.",
                    new[] { nameof(NumeroDocumentoReceptor) }
                );
            }

            if (TipoDocumentoReceptor == 80 &&
                !EsFacturacionManual &&
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