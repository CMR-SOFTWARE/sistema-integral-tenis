using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

[ApiController]
[Authorize(Policy = "Profesor")]
[Route("api/cancelaciones")]
public class CancelacionesController : ControllerBase
{
    private const int TopeDefault = 20;

    private readonly ICancelacionService _service;

    public CancelacionesController(ICancelacionService service)
    {
        _service = service;
    }

    /// <summary>Cancelaciones recientes (turnos enteros + avisos de alumnos), la más nueva primero.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CancelacionDto>>> Listar(
        CancellationToken ct, [FromQuery] int cantidad = TopeDefault) =>
        Ok(await _service.ListarRecientesAsync(Math.Clamp(cantidad, 1, 100), ct));
}
