using Back.DTOs;

namespace Back.Services
{
    public interface IArcaPdfService
    {
        byte[] GenerarFacturaC(ArcaSolicitudCaeDTO solicitud, ArcaCaeResponseDTO comprobante);
    }
}