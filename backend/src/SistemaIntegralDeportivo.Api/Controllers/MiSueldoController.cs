using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>
/// La liquidación del PROPIO profe (su panel ve sus horas y lo que va a cobrar).
/// Policy "Profesor" (no "Owner"): el profe empleado también entra. Devuelve solo
/// SU liquidación, resuelta por el userId del token.
/// </summary>
[ApiController]
[Authorize(Policy = "Profesor")]
[Route("api/mi-sueldo")]
public class MiSueldoController : ControllerBase
{
    private readonly ISueldoService _service;

    public MiSueldoController(ISueldoService service)
    {
        _service = service;
    }

    /// <summary>GET api/mi-sueldo/2026/7 — mi liquidación del mes.</summary>
    [HttpGet("{anio:int}/{mes:int:range(1,12)}")]
    public async Task<ActionResult<EmpleadoSueldoDto>> Mes(int anio, int mes, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.ObtenerMioAsync(anio, mes, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
