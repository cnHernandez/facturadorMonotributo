using Back.DTOs;
using Back.Services;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers
{
    [Route("api/pacientes")]
    [ApiController]
    public class PacientesController : ControllerBase
    {
        private readonly IPacienteService _service;

        public PacientesController(IPacienteService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PacienteResponseDTO>>> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PacienteResponseDTO>> GetById(int id)
        {
            var paciente = await _service.GetByIdAsync(id);
            return paciente == null ? NotFound() : Ok(paciente);
        }

        [HttpPost]
        public async Task<ActionResult<PacienteResponseDTO>> Create(PacienteDTO dto)
        {
            var paciente = await _service.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = paciente.Id }, paciente);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<PacienteResponseDTO>> Update(int id, PacienteDTO dto)
        {
            var paciente = await _service.UpdateAsync(id, dto);
            return paciente == null ? NotFound() : Ok(paciente);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
            => await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}