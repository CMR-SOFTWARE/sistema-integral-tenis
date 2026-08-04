using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>
/// Los perfiles como los ve el alumno: los de su club y los del club que está por
/// elegir. Pide sesión (cualquier usuario logueado) pero NO un tenant: el jugador
/// que todavía no se unió a nadie no tiene ninguno. Por eso el club viene en la URL
/// y el Service usa las consultas no scopeadas del repositorio, sin tocar el tenant
/// del token (ADR-0010).
/// </summary>
[ApiController]
[Authorize]
[Route("api/publico")]
public class PerfilesPublicosController : ControllerBase
{
    private readonly IPerfilProfesorService _perfiles;

    public PerfilesPublicosController(IPerfilProfesorService perfiles)
    {
        _perfiles = perfiles;
    }

    /// <summary>GET api/publico/clubes/{tenantId}/profesores — los profes del club.</summary>
    [HttpGet("clubes/{tenantId:guid}/profesores")]
    public async Task<ActionResult<IReadOnlyList<ProfesorTarjetaDto>>> Profesores(Guid tenantId, CancellationToken ct) =>
        Ok(await _perfiles.ListarProfesoresDelClubAsync(tenantId, ct));

    /// <summary>
    /// GET api/publico/clubes/{tenantId}/profesores/{userId} — la carta de presentación.
    /// Si no está publicada (o el profe ya no trabaja ahí) devuelve 404 y no 403: no
    /// tiene por qué revelarse que existe.
    /// </summary>
    [HttpGet("clubes/{tenantId:guid}/profesores/{userId:guid}")]
    public async Task<ActionResult<PerfilProfesorPublicoDto>> Perfil(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var perfil = await _perfiles.ObtenerPublicoAsync(tenantId, userId, ct);
        return perfil is null ? NotFound() : Ok(perfil);
    }
}
