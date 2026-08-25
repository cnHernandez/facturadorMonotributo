using Back.DTOs;
using Back.Services;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers
{
    [Route("api/obras-sociales")]
    [ApiController]
    public class ObrasSocialesController : ControllerBase
    {
        private readonly IObraSocialService _service;

        public ObrasSocialesController(IObraSocialService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ObraSocialResponseDTO>>> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ObraSocialResponseDTO>> GetById(int id)
        {
            var obraSocial = await _service.GetByIdAsync(id);
            return obraSocial == null ? NotFound() : Ok(obraSocial);
        }

        [HttpPost]
        public async Task<ActionResult<ObraSocialResponseDTO>> Create(ObraSocialDTO dto)
        {
            var obraSocial = await _service.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = obraSocial.Id }, obraSocial);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ObraSocialResponseDTO>> Update(int id, ObraSocialDTO dto)
        {
            var obraSocial = await _service.UpdateAsync(id, dto);
            return obraSocial == null ? NotFound() : Ok(obraSocial);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
            => await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}