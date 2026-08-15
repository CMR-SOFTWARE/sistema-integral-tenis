using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>
/// Ranking R.U.T.A.: cross-tenant, cualquier usuario autenticado (alumno o
/// profe) puede ver el leaderboard e inscribirse — por eso [Authorize] sin
/// policy, igual que PortalController, y no policy "Owner"/"Admin".
/// </summary>
[ApiController]
[Authorize]
[Route("api/ranking")]
public class RankingController : ControllerBase
{
    private readonly IRankingService _ranking;

    public RankingController(IRankingService ranking)
    {
        _ranking = ranking;
    }

    /// <summary>GET api/ranking — el leaderboard provisional completo.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RankingFilaDto>>> Leaderboard(CancellationToken ct) =>
        Ok(await _ranking.ListarLeaderboardAsync(ct));

    /// <summary>GET api/ranking/oficial?scope=Global|Ciudad|Provincia|Pais&amp;valor= — el
    /// último cierre oficial vigente (vacío si nunca se cerró).</summary>
    [HttpGet("oficial")]
    public async Task<ActionResult<IReadOnlyList<RankingFilaDto>>> Oficial(
        [FromQuery] ScopeRanking scope, [FromQuery] string? valor, CancellationToken ct) =>
        Ok(await _ranking.ListarOficialAsync(scope, valor, ct));

    /// <summary>GET api/ranking/mi-perfil — mi inscripción y posición actual.</summary>
    [HttpGet("mi-perfil")]
    public async Task<ActionResult<MiPerfilRankingDto>> MiPerfil(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        return Ok(await _ranking.MiPerfilAsync(userId, ct));
    }

    /// <summary>GET api/ranking/jugadores/{jugadorId} — perfil público de cualquier jugador
    /// (se abre al tocar su fila en la tabla de posiciones).</summary>
    [HttpGet("jugadores/{jugadorId:guid}")]
    public async Task<ActionResult<PerfilPublicoRankingDto>> PerfilPublico(Guid jugadorId, CancellationToken ct)
    {
        var perfil = await _ranking.PerfilPublicoAsync(jugadorId, ct);
        return perfil is null ? NotFound() : Ok(perfil);
    }

    /// <summary>POST api/ranking/inscribirme — alta al ranking (datos opcionales).</summary>
    [HttpPost("inscribirme")]
    public async Task<ActionResult<RankingFilaDto>> Inscribirme(InscribirmeRankingDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _ranking.InscribirmeAsync(userId, dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private Guid? UserId() =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
}
