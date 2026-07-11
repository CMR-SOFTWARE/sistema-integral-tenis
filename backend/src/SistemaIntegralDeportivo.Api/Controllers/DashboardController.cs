using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

[ApiController]
[Authorize(Policy = "Profesor")]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    /// <summary>GET api/dashboard — resumen con datos reales del tenant.</summary>
    [HttpGet]
    public async Task<ActionResult<DashboardResumenDto>> Resumen(CancellationToken ct) =>
        Ok(await _service.ObtenerResumenAsync(ct));
}
