using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>Los pedidos de servicios que el profe resuelve (M4).</summary>
[ApiController]
[Authorize(Policy = "Profesor")]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    private readonly IPedidoService _service;

    public PedidosController(IPedidoService service)
    {
        _service = service;
    }

    /// <summary>GET api/pedidos/pendientes — los que esperan que el profe acepte/rechace.</summary>
    [HttpGet("pendientes")]
    public async Task<ActionResult<IReadOnlyList<PedidoDto>>> Pendientes(CancellationToken ct) =>
        Ok(await _service.ListarPendientesAsync(ct));

    /// <summary>GET api/pedidos/pendientes/cuenta — cuántos hay (contador del dashboard).</summary>
    [HttpGet("pendientes/cuenta")]
    public async Task<ActionResult<int>> ContarPendientes(CancellationToken ct) =>
        Ok(await _service.ContarPendientesAsync(ct));

    /// <summary>POST api/pedidos/{id}/aceptar — lo hago: nace el cargo en la cuenta del alumno.</summary>
    [HttpPost("{id:guid}/aceptar")]
    public async Task<IActionResult> Aceptar(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.AceptarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/pedidos/{id}/rechazar — no lo hago: el pedido se descarta, sin deuda.</summary>
    [HttpPost("{id:guid}/rechazar")]
    public async Task<IActionResult> Rechazar(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.RechazarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
