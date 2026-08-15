using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>Pedidos de revisión sobre un partido de ranking ya finalizado (singles o dobles).</summary>
[ApiController]
[Authorize]
[Route("api/revisiones")]
public class RevisionesController : ControllerBase
{
    private readonly IJuegoRevisionService _revisiones;

    public RevisionesController(IJuegoRevisionService revisiones)
    {
        _revisiones = revisiones;
    }

    /// <summary>POST api/revisiones — pido revisión de un partido mío ya finalizado.</summary>
    [HttpPost]
    public async Task<ActionResult<JuegoRevisionDto>> Crear(CrearRevisionDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _revisiones.CrearAsync(userId, dto.JuegoPendienteId, dto.JuegoDoblesPendienteId, dto.Comentario, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/revisiones/pendientes — panel de moderación, solo admin.</summary>
    [HttpGet("pendientes")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<IReadOnlyList<RevisionPendienteDto>>> Pendientes(CancellationToken ct) =>
        Ok(await _revisiones.ListarPendientesAsync(ct));

    /// <summary>POST api/revisiones/{id}/resolver — solo el admin de plataforma.</summary>
    [HttpPost("{id:guid}/resolver")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Resolver(Guid id, ResolverRevisionDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            await _revisiones.ResolverAsync(userId, id, dto.Respuesta, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private Guid? UserId() =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
}
