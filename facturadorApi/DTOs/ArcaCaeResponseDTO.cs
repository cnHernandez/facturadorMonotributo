namespace Back.DTOs
{
    public class ArcaCaeResponseDTO
    {
        public int PuntoDeVenta { get; set; }
        public int TipoComprobante { get; set; }
        public long NumeroComprobante { get; set; }
        public string Cae { get; set; } = null!;
        public DateOnly FechaVencimientoCae { get; set; }
    }
}