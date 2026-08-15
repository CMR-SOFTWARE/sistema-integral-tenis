using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>Desafíos de ranking: cross-tenant, cualquier jugador inscripto participa.</summary>
[ApiController]
[Authorize(Policy = "Admin")] // EN PAUSA: ver RankingController
[Route("api/desafios")]
public class DesafioController : ControllerBase
{
    private readonly IDesafioService _desafios;

    public DesafioController(IDesafioService desafios)
    {
        _desafios = desafios;
    }

    /// <summary>POST api/desafios — desafío a otro jugador del ranking.</summary>
    [HttpPost]
    public async Task<ActionResult<DesafioDto>> Proponer(ProponerDesafioDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _desafios.ProponerAsync(userId, dto.RivalJugadorId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/desafios/mis-pendientes — los propuestos/aceptados donde participo.</summary>
    [HttpGet("mis-pendientes")]
    public async Task<ActionResult<IReadOnlyList<DesafioDto>>> MisPendientes(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        return Ok(await _desafios.MisPendientesAsync(userId, ct));
    }

    /// <summary>GET api/desafios/mis-finalizados — mi historial, para pedir revisión.</summary>
    [HttpGet("mis-finalizados")]
    public async Task<ActionResult<IReadOnlyList<DesafioDto>>> MisFinalizados(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        return Ok(await _desafios.MisFinalizadosAsync(userId, ct));
    }

    /// <summary>GET api/desafios/jugador/{jugadorId}/finalizados — historial de CUALQUIER
    /// jugador del ranking (perfil público, el mismo criterio cross-tenant que el resto).</summary>
    [HttpGet("jugador/{jugadorId:guid}/finalizados")]
    public async Task<ActionResult<IReadOnlyList<DesafioDto>>> FinalizadosDeJugador(Guid jugadorId, CancellationToken ct) =>
        Ok(await _desafios.FinalizadosDeJugadorAsync(jugadorId, ct));

    [HttpPost("{id:guid}/aceptar")]
    public async Task<IActionResult> Aceptar(Guid id, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            await _desafios.AceptarAsync(userId, id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPost("{id:guid}/rechazar")]
    public async Task<IActionResult> Rechazar(Guid id, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            await _desafios.RechazarAsync(userId, id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            await _desafios.CancelarAsync(userId, id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/desafios/{id}/finalizar — cargo quién ganó (nada de resultado en texto).</summary>
    [HttpPost("{id:guid}/finalizar")]
    public async Task<ActionResult<DesafioDto>> Finalizar(Guid id, FinalizarDesafioDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _desafios.FinalizarAsync(userId, id, dto.GanadorJugadorId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private Guid? UserId() =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
}
