using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

[ApiController]
[Authorize(Policy = "Profesor")]
[Route("api/configuracion")]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfigService _service;
    private readonly IServicioService _servicios;

    public ConfiguracionController(IConfigService service, IServicioService servicios)
    {
        _service = service;
        _servicios = servicios;
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

    // ── Catálogo de servicios que ofrece el profe (M4) ──

    /// <summary>Todos los servicios del catálogo (activos e inactivos).</summary>
    [HttpGet("servicios")]
    public async Task<ActionResult<IReadOnlyList<ServicioDto>>> Servicios(CancellationToken ct) =>
        Ok(await _servicios.ListarAsync(soloActivos: false, ct));

    [HttpPost("servicios")]
    public async Task<ActionResult<ServicioDto>> CrearServicio(GuardarServicioDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _servicios.CrearAsync(dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPut("servicios/{id:guid}")]
    public async Task<ActionResult<ServicioDto>> EditarServicio(Guid id, GuardarServicioDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _servicios.EditarAsync(id, dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Baja/reactivación lógica del servicio (no se borra: hay pedidos que lo referencian).</summary>
    [HttpPatch("servicios/{id:guid}/activo")]
    public async Task<ActionResult<ServicioDto>> CambiarActivoServicio(Guid id, CambiarActivoDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _servicios.CambiarActivoAsync(id, dto.Activo, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
