using System.ComponentModel.DataAnnotations;

namespace Back.DTOs
{
    public class ArcaSolicitudCaeDTO
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
    }
}