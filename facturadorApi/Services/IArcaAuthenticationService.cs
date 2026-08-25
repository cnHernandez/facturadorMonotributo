using Back.DTOs;

namespace Back.Services
{
    public interface IArcaAuthenticationService
    {
        Task<ArcaAuthenticationResponseDTO> AuthenticateAsync(CancellationToken cancellationToken = default);
        Task<ArcaAuthenticationTicket> GetTicketAsync(CancellationToken cancellationToken = default);
    }
}