using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

[ApiController]
[Authorize(Policy = "Owner")]
[Route("api/reportes")]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _service;

    public ReportesController(IReporteService service)
    {
        _service = service;
    }

    /// <summary>Recaudación últimos 6 meses + alumnos por categoría.</summary>
    [HttpGet]
    public async Task<ActionResult<ReportesDto>> Obtener(CancellationToken ct) =>
        Ok(await _service.ObtenerAsync(DateOnly.FromDateTime(DateTime.UtcNow), ct));
}
