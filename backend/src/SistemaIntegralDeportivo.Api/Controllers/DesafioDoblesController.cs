using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>Desafíos de dobles: cross-tenant, espejo de DesafioController.</summary>
[ApiController]
[Authorize(Policy = "Admin")] // EN PAUSA: ver RankingController
[Route("api/desafios/dobles")]
public class DesafioDoblesController : ControllerBase
{
    private readonly IDesafioDoblesService _desafios;

    public DesafioDoblesController(IDesafioDoblesService desafios)
    {
        _desafios = desafios;
    }

    /// <summary>POST api/desafios/dobles — armo el partido: mi compañero + los dos rivales.</summary>
    [HttpPost]
    public async Task<ActionResult<DesafioDoblesDto>> Proponer(ProponerDesafioDoblesDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _desafios.ProponerAsync(userId, dto.CompaneroJugadorId, dto.Rival1JugadorId, dto.Rival2JugadorId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/desafios/dobles/mis-pendientes.</summary>
    [HttpGet("mis-pendientes")]
    public async Task<ActionResult<IReadOnlyList<DesafioDoblesDto>>> MisPendientes(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        return Ok(await _desafios.MisPendientesAsync(userId, ct));
    }

    /// <summary>GET api/desafios/dobles/mis-finalizados — mi historial, para pedir revisión.</summary>
    [HttpGet("mis-finalizados")]
    public async Task<ActionResult<IReadOnlyList<DesafioDoblesDto>>> MisFinalizados(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        return Ok(await _desafios.MisFinalizadosAsync(userId, ct));
    }

    /// <summary>GET api/desafios/dobles/jugador/{jugadorId}/finalizados — historial de dobles de
    /// CUALQUIER jugador (perfil público).</summary>
    [HttpGet("jugador/{jugadorId:guid}/finalizados")]
    public async Task<ActionResult<IReadOnlyList<DesafioDoblesDto>>> FinalizadosDeJugador(Guid jugadorId, CancellationToken ct) =>
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

    /// <summary>POST api/desafios/dobles/{id}/finalizar — cargo quién ganó (cualquiera de los 4).</summary>
    [HttpPost("{id:guid}/finalizar")]
    public async Task<ActionResult<DesafioDoblesDto>> Finalizar(Guid id, FinalizarDesafioDoblesDto dto, CancellationToken ct)
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
