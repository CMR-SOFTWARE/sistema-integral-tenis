using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>
/// Panel de PLATAFORMA (cross-tenant): solo el admin (dueño de la app). Es el
/// único controller que trabaja sobre todos los clubes a la vez.
/// </summary>
[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly IRankingCierreOficialService _cierreOficial;

    public AdminController(IAdminService admin, IRankingCierreOficialService cierreOficial)
    {
        _admin = admin;
        _cierreOficial = cierreOficial;
    }

    /// <summary>GET api/admin/metricas — números globales de la plataforma.</summary>
    [HttpGet("metricas")]
    public async Task<ActionResult<MetricasPlataformaDto>> Metricas(CancellationToken ct) =>
        Ok(await _admin.MetricasAsync(ct));

    /// <summary>GET api/admin/clubes — todos los clubes con su profe y su tamaño.</summary>
    [HttpGet("clubes")]
    public async Task<ActionResult<IReadOnlyList<ClubAdminDto>>> Clubes(CancellationToken ct) =>
        Ok(await _admin.ListarClubesAsync(ct));

    /// <summary>
    /// POST api/admin/clubes — el admin da de alta una academia (Bloque 6, pedido 10):
    /// crea el club + la cuenta del director, ya ACTIVA (sin checkout de Mercado Pago).
    /// </summary>
    [HttpPost("clubes")]
    public async Task<ActionResult<ClubCreadoDto>> CrearClub(AltaClubDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _admin.CrearClubAsync(dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/admin/personas — el padrón de personas de la plataforma (Bloque 6, pedido 11).</summary>
    [HttpGet("personas")]
    public async Task<ActionResult<IReadOnlyList<PersonaAdminDto>>> Personas(CancellationToken ct) =>
        Ok(await _admin.ListarPersonasAsync(ct));

    /// <summary>PATCH api/admin/clubes/{id}/estado — activar o suspender un club.</summary>
    [HttpPatch("clubes/{id:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoClubDto dto, CancellationToken ct)
    {
        try
        {
            await _admin.CambiarEstadoClubAsync(id, dto.Estado, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/admin/ranking/cerrar-oficial — fuerza el cierre oficial del
    /// ranking (día 1/16) a mano, para probar sin esperar la fecha real.</summary>
    [HttpPost("ranking/cerrar-oficial")]
    public async Task<IActionResult> CerrarRankingOficial(CancellationToken ct)
    {
        try
        {
            var cantidad = await _cierreOficial.CerrarOficialAsync(ct);
            return Ok(new { cantidad });
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
