using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

[ApiController]
[Authorize(Policy = "Owner")]
[Route("api/bloqueos")]
public class BloqueosController : ControllerBase
{
    private readonly IBloqueoService _service;

    public BloqueosController(IBloqueoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BloqueoResponseDto>>> Listar(CancellationToken ct) =>
        Ok(await _service.ListarAsync(ct));

    /// <summary>Preview del modal Impacto: qué cancelaría el bloqueo, SIN persistir.
    /// POST con semántica de consulta (el DTO no entra en una querystring).</summary>
    [HttpPost("impacto")]
    public async Task<ActionResult<ImpactoBloqueoDto>> PrevisualizarImpacto(
        CreateBloqueoDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.PrevisualizarImpactoAsync(dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPost]
    public async Task<ActionResult<BloqueoCreadoDto>> Crear(CreateBloqueoDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.CrearAsync(dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.EliminarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
