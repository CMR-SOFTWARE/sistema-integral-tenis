using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>Avisos in-app, cross-tenant (por Usuario, no por tenant).</summary>
[ApiController]
[Authorize]
[Route("api/notificaciones")]
public class NotificacionController : ControllerBase
{
    private readonly INotificacionService _notificaciones;

    public NotificacionController(INotificacionService notificaciones)
    {
        _notificaciones = notificaciones;
    }

    /// <summary>GET api/notificaciones — mis últimos 50 avisos.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificacionDto>>> Mias(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        return Ok(await _notificaciones.MisAsync(userId, ct));
    }

    /// <summary>GET api/notificaciones/no-leidas/contador — para el badge de la campana.</summary>
    [HttpGet("no-leidas/contador")]
    public async Task<ActionResult<int>> ContadorNoLeidas(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        return Ok(await _notificaciones.ContarNoLeidasAsync(userId, ct));
    }

    /// <summary>POST api/notificaciones/marcar-todas-leidas.</summary>
    [HttpPost("marcar-todas-leidas")]
    public async Task<IActionResult> MarcarTodasLeidas(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        await _notificaciones.MarcarTodasLeidasAsync(userId, ct);
        return NoContent();
    }

    private Guid? UserId() =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
}
