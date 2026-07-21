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

    public AdminController(IAdminService admin)
    {
        _admin = admin;
    }

    /// <summary>GET api/admin/metricas — números globales de la plataforma.</summary>
    [HttpGet("metricas")]
    public async Task<ActionResult<MetricasPlataformaDto>> Metricas(CancellationToken ct) =>
        Ok(await _admin.MetricasAsync(ct));

    /// <summary>GET api/admin/clubes — todos los clubes con su profe y su tamaño.</summary>
    [HttpGet("clubes")]
    public async Task<ActionResult<IReadOnlyList<ClubAdminDto>>> Clubes(CancellationToken ct) =>
        Ok(await _admin.ListarClubesAsync(ct));

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
}
