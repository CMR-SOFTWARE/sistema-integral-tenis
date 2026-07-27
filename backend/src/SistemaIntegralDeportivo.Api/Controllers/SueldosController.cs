using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>
/// Sueldos de los profes empleados (G3). Solo el DUEÑO los administra (policy Owner):
/// es el egreso del negocio, espejo de las cuotas.
/// </summary>
[ApiController]
[Authorize(Policy = "Owner")]
[Route("api/sueldos")]
public class SueldosController : ControllerBase
{
    private readonly ISueldoService _service;

    public SueldosController(ISueldoService service)
    {
        _service = service;
    }

    /// <summary>GET api/sueldos/2026/7 — liquidación del mes (calculado vs pagado por empleado).</summary>
    [HttpGet("{anio:int}/{mes:int:range(1,12)}")]
    public async Task<ActionResult<LiquidacionSueldosDto>> Mes(int anio, int mes, CancellationToken ct)
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

    /// <summary>GET api/sueldos/reporte?meses=6 — egreso de sueldos por mes.</summary>
    [HttpGet("reporte")]
    public async Task<ActionResult<IReadOnlyList<SueldoMesDto>>> Reporte(
        [FromQuery] int meses, CancellationToken ct) =>
        Ok(await _service.ObtenerReporteAsync(meses <= 0 ? 6 : meses, ct));

    /// <summary>POST api/sueldos/pagar — registra el pago del sueldo de un empleado por un mes.</summary>
    [HttpPost("pagar")]
    public async Task<IActionResult> Pagar(PagarSueldoDto dto, CancellationToken ct)
    {
        try
        {
            await _service.PagarAsync(dto.UserId, dto.Anio, dto.Mes, dto.Monto, dto.Medio, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/sueldos/revertir — borra el pago registrado de un empleado en un mes.</summary>
    [HttpPost("revertir")]
    public async Task<IActionResult> Revertir(RevertirSueldoDto dto, CancellationToken ct)
    {
        try
        {
            await _service.RevertirPagoAsync(dto.UserId, dto.Anio, dto.Mes, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
