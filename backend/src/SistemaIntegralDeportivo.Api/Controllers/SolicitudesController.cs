using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>Lado PROFE de las solicitudes: ver pendientes, aprobar, rechazar.</summary>
[ApiController]
[Authorize(Policy = "Profesor")] // dueño Y staff; el service acota al staff a SU lista de espera
[Route("api/solicitudes")]
public class SolicitudesController : ControllerBase
{
    private readonly ISolicitudService _service;

    public SolicitudesController(ISolicitudService service)
    {
        _service = service;
    }

    /// <summary>La lista de espera de MI club: los sin clase + los pedidos sin resolver.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SolicitudPendienteDto>>> Pendientes(CancellationToken ct) =>
        Ok(await _service.PendientesAsync(ct));

    /// <summary>Conteo para el badge de la pestaña (cuántos hay esperando).</summary>
    [HttpGet("conteo")]
    public async Task<ActionResult<ConteoSolicitudesDto>> Conteo(CancellationToken ct) =>
        Ok(new ConteoSolicitudesDto { Pendientes = await _service.ContarPendientesAsync(ct) });

    /// <summary>
    /// Sacar de la espera. Al que anotó el profe le apaga la marca; al que espera SIN
    /// clase le borra la ficha (conserva su login). Para el que espera por un pedido va
    /// <c>POST api/horarios/solicitudes-cupo/{id}/rechazar</c>, que no toca la ficha.
    /// </summary>
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

    /// <summary>
    /// El profe anota (o desanota) a mano en la espera al alumno que le pidió otra
    /// clase hablando, sin pasar por el portal. <paramref name="id"/> es la ficha.
    /// </summary>
    [HttpPatch("{id:guid}/espera")]
    public async Task<IActionResult> CambiarEspera(Guid id, CambiarEsperaDto dto, CancellationToken ct)
    {
        try
        {
            await _service.CambiarEsperaAsync(id, dto.EnEspera, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
