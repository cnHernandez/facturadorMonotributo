using Back.DTOs;
using Back.Models;
using Back.Repositories;

namespace Back.Services
{
    public class PacienteService : IPacienteService
    {
        private readonly IRepository<Paciente> _repository;

        public PacienteService(IRepository<Paciente> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<PacienteResponseDTO>> GetAllAsync()
            => (await _repository.GetAllAsync()).Select(ToResponse).ToList();

        public async Task<PacienteResponseDTO?> GetByIdAsync(int id)
        {
            var paciente = await _repository.GetByIdAsync(id);
            return paciente == null ? null : ToResponse(paciente);
        }

        public async Task<PacienteResponseDTO> AddAsync(PacienteDTO dto)
        {
            var paciente = new Paciente();
            Apply(dto, paciente);
            await _repository.AddAsync(paciente);
            return ToResponse(paciente);
        }

        public async Task<PacienteResponseDTO?> UpdateAsync(int id, PacienteDTO dto)
        {
            var paciente = await _repository.GetByIdAsync(id);
            if (paciente == null) return null;

            Apply(dto, paciente);
            await _repository.UpdateAsync(paciente);
            return ToResponse(paciente);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (await _repository.GetByIdAsync(id) == null) return false;
            await _repository.DeleteAsync(id);
            return true;
        }

        private static void Apply(PacienteDTO dto, Paciente paciente)
        {
            paciente.Dni = dto.Dni;
            paciente.NumAfiliado = dto.NumAfiliado;
            paciente.Nombre = dto.Nombre;
            paciente.Apellido = dto.Apellido;
            paciente.Domicilio = dto.Domicilio;
            paciente.PlanillaAsistenciaImagenUrl = dto.PlanillaAsistenciaImagenUrl;
            paciente.Estado = dto.Estado;
            paciente.ObraSocialId = dto.ObraSocialId;
        }

        private static PacienteResponseDTO ToResponse(Paciente paciente) => new()
        {
            Id = paciente.Id,
            Dni = paciente.Dni,
            NumAfiliado = paciente.NumAfiliado,
            Nombre = paciente.Nombre,
            Apellido = paciente.Apellido,
            Domicilio = paciente.Domicilio,
            PlanillaAsistenciaImagenUrl = paciente.PlanillaAsistenciaImagenUrl,
            Estado = paciente.Estado,
            ObraSocialId = paciente.ObraSocialId
        };
    }
}