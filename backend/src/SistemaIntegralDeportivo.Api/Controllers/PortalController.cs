using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>
/// El portal del ALUMNO logueado. Alcanza con estar autenticado: el service
/// resuelve la ficha desde el userId del token, nunca desde parámetros.
/// </summary>
[ApiController]
[Route("api/portal")]
[Authorize]
public class PortalController : ControllerBase
{
    private readonly IPortalService _portal;

    public PortalController(IPortalService portal)
    {
        _portal = portal;
    }

    /// <summary>GET api/portal/mis-turnos — próximas clases + historial reciente.</summary>
    [HttpGet("mis-turnos")]
    public async Task<ActionResult<MisTurnosDto>> MisTurnos(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.MisTurnosAsync(userId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/portal/mi-cuota/2026/7 — mi liquidación del mes (204 si no hay movimientos).</summary>
    [HttpGet("mi-cuota/{anio:int}/{mes:int:range(1,12)}")]
    public async Task<ActionResult<AlumnoLiquidacionDto>> MiCuota(int anio, int mes, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            var mia = await _portal.MiCuotaAsync(userId, anio, mes, ct);
            return mia is null ? NoContent() : Ok(mia);
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/portal/perfil — mi ficha, como me ve el club.</summary>
    [HttpGet("perfil")]
    public async Task<ActionResult<MiPerfilDto>> Perfil(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.MiPerfilAsync(userId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>PUT api/portal/perfil — edito MIS datos de contacto (teléfono/email).</summary>
    [HttpPut("perfil")]
    public async Task<ActionResult<MiPerfilDto>> ActualizarPerfil(
        ActualizarMiPerfilDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.ActualizarMiPerfilAsync(userId, dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/portal/mis-turnos/{id}/cancelar — aviso que no vengo (mi cargo queda).</summary>
    [HttpPost("mis-turnos/{turnoId:guid}/cancelar")]
    public async Task<IActionResult> CancelarMiTurno(
        Guid turnoId, CancelarMiTurnoDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            await _portal.CancelarMiTurnoAsync(userId, turnoId, dto.Motivo, ct);
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
