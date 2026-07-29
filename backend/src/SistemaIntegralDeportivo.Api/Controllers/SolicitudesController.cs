using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>Lado PROFE de las solicitudes: ver pendientes, aprobar, rechazar.</summary>
[ApiController]
[Authorize(Policy = "Owner")]
[Route("api/solicitudes")]
public class SolicitudesController : ControllerBase
{
    private readonly ISolicitudService _service;

    public SolicitudesController(ISolicitudService service)
    {
        _service = service;
    }

    /// <summary>La lista de espera de MI club (fichas EnEspera), con sus datos.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SolicitudPendienteDto>>> Pendientes(CancellationToken ct) =>
        Ok(await _service.PendientesAsync(ct));

    /// <summary>Conteo para el badge del sidebar (cuántos hay en espera).</summary>
    [HttpGet("conteo")]
    public async Task<ActionResult<ConteoSolicitudesDto>> Conteo(CancellationToken ct) =>
        Ok(new ConteoSolicitudesDto { Pendientes = await _service.ContarPendientesAsync(ct) });

    /// <summary>Quitar de la lista de espera (borra la ficha; conserva su login).</summary>
    [HttpPost("{id:guid}/quitar")]
    public async Task<IActionResult> Quitar(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.QuitarDeEsperaAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
