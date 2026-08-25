using Back.DTOs;
using Back.Models;
using Back.Repositories;

namespace Back.Services
{
    public class ObraSocialService : IObraSocialService
    {
        private readonly IRepository<ObraSocial> _repository;

        public ObraSocialService(IRepository<ObraSocial> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ObraSocialResponseDTO>> GetAllAsync()
            => (await _repository.GetAllAsync()).Select(ToResponse).ToList();

        public async Task<ObraSocialResponseDTO?> GetByIdAsync(int id)
        {
            var obraSocial = await _repository.GetByIdAsync(id);
            return obraSocial == null ? null : ToResponse(obraSocial);
        }

        public async Task<ObraSocialResponseDTO> AddAsync(ObraSocialDTO dto)
        {
            var obraSocial = new ObraSocial();
            Apply(dto, obraSocial);
            await _repository.AddAsync(obraSocial);
            return ToResponse(obraSocial);
        }

        public async Task<ObraSocialResponseDTO?> UpdateAsync(int id, ObraSocialDTO dto)
        {
            var obraSocial = await _repository.GetByIdAsync(id);
            if (obraSocial == null) return null;

            Apply(dto, obraSocial);
            await _repository.UpdateAsync(obraSocial);
            return ToResponse(obraSocial);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (await _repository.GetByIdAsync(id) == null) return false;
            await _repository.DeleteAsync(id);
            return true;
        }

        private static void Apply(ObraSocialDTO dto, ObraSocial obraSocial)
        {
            obraSocial.Cuit = dto.Cuit;
            obraSocial.Nombre = dto.Nombre;
            obraSocial.DomicilioComercial = dto.DomicilioComercial;
            obraSocial.Condicion = dto.Condicion;
            obraSocial.Estado = dto.Estado;
        }

        private static ObraSocialResponseDTO ToResponse(ObraSocial obraSocial) => new()
        {
            Id = obraSocial.Id,
            Cuit = obraSocial.Cuit,
            Nombre = obraSocial.Nombre,
            DomicilioComercial = obraSocial.DomicilioComercial,
            Condicion = obraSocial.Condicion,
            Estado = obraSocial.Estado
        };
    }
}