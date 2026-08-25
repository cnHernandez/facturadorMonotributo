using Back.DTOs;
using Back.Services;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers
{
    [Route("api/arca")]
    [ApiController]
    public class ArcaController : ControllerBase
    {
        private readonly IArcaAuthenticationService _authenticationService;
        private readonly IArcaWsfeService _wsfeService;
        private readonly IArcaPdfService _pdfService;

        public ArcaController(
            IArcaAuthenticationService authenticationService,
            IArcaWsfeService wsfeService,
            IArcaPdfService pdfService)
        {
            _authenticationService = authenticationService;
            _wsfeService = wsfeService;
            _pdfService = pdfService;
        }

        [HttpPost("autenticacion")]
        public async Task<ActionResult<ArcaAuthenticationResponseDTO>> Authenticate(CancellationToken cancellationToken)
            => Ok(await _authenticationService.AuthenticateAsync(cancellationToken));

        [HttpGet("ultimo-comprobante/{tipoComprobante:int}")]
        public async Task<ActionResult<ArcaUltimoComprobanteResponseDTO>> GetUltimoComprobante(
            int tipoComprobante,
            CancellationToken cancellationToken)
            => Ok(await _wsfeService.GetUltimoComprobanteAsync(tipoComprobante, cancellationToken));

        [HttpPost("cae")]
        public async Task<ActionResult<ArcaCaeResponseDTO>> SolicitarCae(
            ArcaSolicitudCaeDTO solicitud,
            CancellationToken cancellationToken)
            => Ok(await _wsfeService.SolicitarCaeAsync(solicitud, cancellationToken));

        [HttpPost("cae/pdf")]
        public async Task<IActionResult> SolicitarCaePdf(
            ArcaSolicitudCaeDTO solicitud,
            CancellationToken cancellationToken)
        {
            var comprobante = await _wsfeService.SolicitarCaeAsync(solicitud, cancellationToken);
            var pdf = _pdfService.GenerarFacturaC(solicitud, comprobante);
            var fileName = $"factura-c-{comprobante.PuntoDeVenta:0000}-{comprobante.NumeroComprobante:00000000}.pdf";

            return File(pdf, "application/pdf", fileName);
        }
    }
}