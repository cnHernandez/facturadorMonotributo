using Back.DTOs;

namespace Back.Services
{
    public interface IPacienteService
    {
        Task<IEnumerable<PacienteResponseDTO>> GetAllAsync();
        Task<PacienteResponseDTO?> GetByIdAsync(int id);
        Task<PacienteResponseDTO> AddAsync(PacienteDTO dto);
        Task<PacienteResponseDTO?> UpdateAsync(int id, PacienteDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}