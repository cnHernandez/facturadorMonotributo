using Back.DTOs;

namespace Back.Services
{
    public interface IObraSocialService
    {
        Task<IEnumerable<ObraSocialResponseDTO>> GetAllAsync();
        Task<ObraSocialResponseDTO?> GetByIdAsync(int id);
        Task<ObraSocialResponseDTO> AddAsync(ObraSocialDTO dto);
        Task<ObraSocialResponseDTO?> UpdateAsync(int id, ObraSocialDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}