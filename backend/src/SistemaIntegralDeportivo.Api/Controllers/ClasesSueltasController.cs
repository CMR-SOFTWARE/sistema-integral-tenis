using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>Las clases sueltas de los alumnos que el profe resuelve (M5c).</summary>
[ApiController]
[Authorize(Policy = "Profesor")]
[Route("api/clases-sueltas")]
public class ClasesSueltasController : ControllerBase
{
    private readonly IClaseSueltaService _service;

    public ClasesSueltasController(IClaseSueltaService service)
    {
        _service = service;
    }

    /// <summary>GET api/clases-sueltas — pendientes de resolver (con el estado del pago).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClaseSueltaDto>>> Pendientes(CancellationToken ct) =>
        Ok(await _service.ListarPendientesAsync(ct));

    /// <summary>GET api/clases-sueltas/{id}/canchas-libres — canchas libres de esa sede/fecha/hora.</summary>
    [HttpGet("{id:guid}/canchas-libres")]
    public async Task<ActionResult<IReadOnlyList<CanchaLibreDto>>> CanchasLibres(Guid id, CancellationToken ct) =>
        Ok(await _service.CanchasLibresParaClaseAsync(id, ct));

    /// <summary>POST api/clases-sueltas/{id}/confirmar — elijo cancha: nace el turno suelto y se marca pagado.</summary>
    [HttpPost("{id:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid id, ConfirmarClaseSueltaDto dto, CancellationToken ct)
    {
        try
        {
            await _service.ConfirmarAsync(id, dto.CanchaId, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/clases-sueltas/{id}/rechazar — rechazo (se borra el cargo).</summary>
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
