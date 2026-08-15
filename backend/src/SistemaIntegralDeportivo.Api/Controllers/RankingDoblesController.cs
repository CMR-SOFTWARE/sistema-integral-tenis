using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>Ranking de DOBLES: cross-tenant, requiere estar inscripto en singles primero.</summary>
[ApiController]
[Authorize]
[Route("api/ranking/dobles")]
public class RankingDoblesController : ControllerBase
{
    private readonly IRankingDoblesService _ranking;

    public RankingDoblesController(IRankingDoblesService ranking)
    {
        _ranking = ranking;
    }

    /// <summary>GET api/ranking/dobles — leaderboard de dobles, en vivo.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RankingFilaDoblesDto>>> Leaderboard(CancellationToken ct) =>
        Ok(await _ranking.ListarLeaderboardAsync(ct));

    /// <summary>GET api/ranking/dobles/oficial?scope=&amp;valor= — el último cierre oficial vigente.</summary>
    [HttpGet("oficial")]
    public async Task<ActionResult<IReadOnlyList<RankingFilaDoblesDto>>> Oficial(
        [FromQuery] ScopeRanking scope, [FromQuery] string? valor, CancellationToken ct) =>
        Ok(await _ranking.ListarOficialAsync(scope, valor, ct));

    /// <summary>GET api/ranking/dobles/mi-perfil.</summary>
    [HttpGet("mi-perfil")]
    public async Task<ActionResult<MiPerfilDoblesDto>> MiPerfil(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        return Ok(await _ranking.MiPerfilAsync(userId, ct));
    }

    /// <summary>GET api/ranking/dobles/jugadores/{jugadorId} — perfil público de dobles de
    /// cualquier jugador (se abre al tocar su fila en la tabla).</summary>
    [HttpGet("jugadores/{jugadorId:guid}")]
    public async Task<ActionResult<PerfilPublicoRankingDoblesDto>> PerfilPublico(Guid jugadorId, CancellationToken ct)
    {
        var perfil = await _ranking.PerfilPublicoAsync(jugadorId, ct);
        return perfil is null ? NotFound() : Ok(perfil);
    }

    /// <summary>POST api/ranking/dobles/inscribirme.</summary>
    [HttpPost("inscribirme")]
    public async Task<ActionResult<RankingFilaDoblesDto>> Inscribirme(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _ranking.InscribirmeAsync(userId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private Guid? UserId() =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
}
