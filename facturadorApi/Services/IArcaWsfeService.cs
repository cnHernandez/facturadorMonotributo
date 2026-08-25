using Back.DTOs;

namespace Back.Services
{
    public interface IArcaWsfeService
    {
        Task<ArcaUltimoComprobanteResponseDTO> GetUltimoComprobanteAsync(int tipoComprobante, CancellationToken cancellationToken = default);
        Task<ArcaCaeResponseDTO> SolicitarCaeAsync(ArcaSolicitudCaeDTO solicitud, CancellationToken cancellationToken = default);
    }
}