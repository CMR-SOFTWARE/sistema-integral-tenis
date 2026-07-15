using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>Lado PROFE de las solicitudes: ver pendientes, aprobar, rechazar.</summary>
[ApiController]
[Authorize(Policy = "Profesor")]
[Route("api/solicitudes")]
public class SolicitudesController : ControllerBase
{
    private readonly ISolicitudService _service;

    public SolicitudesController(ISolicitudService service)
    {
        _service = service;
    }

    /// <summary>Pendientes de MI club, con los datos del solicitante.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SolicitudPendienteDto>>> Pendientes(CancellationToken ct) =>
        Ok(await _service.PendientesAsync(ct));

    /// <summary>Conteo para el badge del sidebar.</summary>
    [HttpGet("conteo")]
    public async Task<ActionResult<ConteoSolicitudesDto>> Conteo(CancellationToken ct) =>
        Ok(new ConteoSolicitudesDto { Pendientes = await _service.ContarPendientesAsync(ct) });

    /// <summary>Aprueba: crea/vincula la ficha en mi club.</summary>
    [HttpPost("{id:guid}/aprobar")]
    public async Task<ActionResult<AlumnoResponseDto>> Aprobar(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.AprobarAsync(id, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Rechaza (el alumno puede volver a solicitar).</summary>
    [HttpPost("{id:guid}/rechazar")]
    public async Task<IActionResult> Rechazar(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.RechazarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
