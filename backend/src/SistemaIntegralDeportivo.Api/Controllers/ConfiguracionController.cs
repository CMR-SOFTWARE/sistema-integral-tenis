using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

[ApiController]
[Authorize(Policy = "Profesor")]
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

    /// <summary>Datos de transferencia (alias/CBU + titular) que ve el alumno al informar un pago.</summary>
    [HttpGet("datos-pago")]
    public async Task<ActionResult<DatosPagoConfigDto>> DatosPago(CancellationToken ct) =>
        Ok(await _service.ObtenerDatosPagoAsync(ct));

    [HttpPut("datos-pago")]
    public async Task<ActionResult<DatosPagoConfigDto>> ActualizarDatosPago(
        DatosPagoConfigDto dto, CancellationToken ct) =>
        Ok(await _service.ActualizarDatosPagoAsync(dto, ct));
}
