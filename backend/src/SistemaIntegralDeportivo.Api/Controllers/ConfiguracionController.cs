using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

[ApiController]
[Route("api/configuracion")]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfigService _service;

    public ConfiguracionController(IConfigService service)
    {
        _service = service;
    }

    [HttpGet("precios")]
    public async Task<ActionResult<PreciosDto>> Precios(CancellationToken ct) =>
        Ok(await _service.ObtenerPreciosAsync(ct));

    [HttpPut("precios")]
    public async Task<ActionResult<PreciosDto>> ActualizarPrecios(PreciosDto dto, CancellationToken ct) =>
        Ok(await _service.ActualizarPreciosAsync(dto, ct));
}
