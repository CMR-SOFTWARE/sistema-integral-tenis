using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

[ApiController]
[Authorize(Policy = "Owner")]
[Route("api/cuotas")]
public class CuotasController : ControllerBase
{
    private readonly ICuotaService _service;

    public CuotasController(ICuotaService service)
    {
        _service = service;
    }

    /// <summary>GET api/cuotas/2026/7 — liquidación del mes (genera cargos que falten).</summary>
    [HttpGet("{anio:int}/{mes:int:range(1,12)}")]
    public async Task<ActionResult<LiquidacionMesDto>> Mes(int anio, int mes, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.ObtenerMesAsync(anio, mes, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/cuotas/cargos — cargo manual (Producto o Ajuste).</summary>
    [HttpPost("cargos")]
    public async Task<ActionResult<CargoResponseDto>> AgregarCargo(
        CreateCargoManualDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.AgregarCargoManualAsync(dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>PUT api/cuotas/cargos/{id}/monto — cambia el monto de un cargo impago (ajustar la cuota al cobrar).</summary>
    [HttpPut("cargos/{id:guid}/monto")]
    public async Task<ActionResult<CargoResponseDto>> EditarMonto(
        Guid id, EditarMontoCargoDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.EditarMontoCargoAsync(id, dto.Monto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/cuotas/reporte?meses=6 — recaudado por mes (balance simple).</summary>
    [HttpGet("reporte")]
    public async Task<ActionResult<IReadOnlyList<RecaudadoMesDto>>> Reporte(
        [FromQuery] int meses, CancellationToken ct) =>
        Ok(await _service.ObtenerReporteAsync(meses <= 0 ? 6 : meses, ct));

    /// <summary>POST api/cuotas/2026/7/pagar — salda el mes de un alumno (modalidad Mensual).</summary>
    [HttpPost("{anio:int}/{mes:int:range(1,12)}/pagar")]
    public async Task<IActionResult> PagarMes(int anio, int mes, PagarMesDto dto, CancellationToken ct)
    {
        try
        {
            await _service.PagarMesAsync(dto.AlumnoId, anio, mes, dto.Medio, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/cuotas/cargos/{id}/pagar — salda UN cargo (modalidad PorClase).</summary>
    [HttpPost("cargos/{id:guid}/pagar")]
    public async Task<IActionResult> PagarCargo(Guid id, PagarCargoDto dto, CancellationToken ct)
    {
        try
        {
            await _service.PagarCargoAsync(id, dto.Medio, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // ── Pago informado: confirmar = pagar (arriba); rechazar = "no me llegó" ──

    /// <summary>POST api/cuotas/2026/7/rechazar — rechaza el pago informado del mes de un alumno.</summary>
    [HttpPost("{anio:int}/{mes:int:range(1,12)}/rechazar")]
    public async Task<IActionResult> RechazarMes(int anio, int mes, RechazarPagoMesDto dto, CancellationToken ct)
    {
        try
        {
            await _service.RechazarPagoMesAsync(dto.AlumnoId, anio, mes, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/cuotas/cargos/{id}/rechazar — rechaza el pago informado de UN cargo.</summary>
    [HttpPost("cargos/{id:guid}/rechazar")]
    public async Task<IActionResult> RechazarCargo(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.RechazarPagoCargoAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
