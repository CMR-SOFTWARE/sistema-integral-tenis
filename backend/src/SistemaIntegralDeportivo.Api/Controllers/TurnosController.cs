using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

[ApiController]
[Route("api/turnos")]
public class TurnosController : ControllerBase
{
    private readonly ITurnoService _service;

    public TurnosController(ITurnoService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET api/turnos/semana?lunes=2026-07-13 — turnos de la semana,
    /// generando perezosamente los que falten.
    /// </summary>
    [HttpGet("semana")]
    public async Task<ActionResult<IReadOnlyList<TurnoResponseDto>>> Semana(
        [FromQuery] DateOnly lunes, CancellationToken ct) =>
        Ok(await _service.ObtenerSemanaAsync(lunes, ct));

    [HttpPatch("{id:guid}/asistencia")]
    public async Task<IActionResult> Asistencia(Guid id, AsistenciaDto dto, CancellationToken ct)
    {
        try
        {
            await _service.MarcarAsistenciaAsync(id, dto.AlumnoId, dto.Presente, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancelarTurnoDto dto, CancellationToken ct)
    {
        try
        {
            await _service.CancelarAsync(id, dto.Motivo, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
