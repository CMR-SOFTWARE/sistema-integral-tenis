using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

[ApiController]
[Authorize(Policy = "Profesor")]
[Route("api/sedes")]
public class SedesController : ControllerBase
{
    private readonly ISedeService _service;

    public SedesController(ISedeService service)
    {
        _service = service;
    }

    // GET queda abierto a Profesor: el calendario del staff necesita las sedes
    // (para el filtro por sede). La gestión de sedes/canchas es dueño-only.

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SedeResponseDto>>> Listar(CancellationToken ct) =>
        Ok(await _service.ListarAsync(ct));

    [HttpPost]
    [Authorize(Policy = "Owner")]
    public async Task<ActionResult<SedeResponseDto>> Crear(CreateSedeDto dto, CancellationToken ct) =>
        Ok(await _service.CrearAsync(dto, ct));

    [HttpPost("{id:guid}/canchas")]
    [Authorize(Policy = "Owner")]
    public async Task<ActionResult<CanchaResponseDto>> AgregarCancha(
        Guid id, CreateCanchaDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.AgregarCanchaAsync(id, dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>DELETE api/sedes/{id} — baja LÓGICA (deja de ofrecerse; la historia queda).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Owner")]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.DesactivarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/sedes/{id}/reactivar — vuelve a habilitarla.</summary>
    [HttpPost("{id:guid}/reactivar")]
    [Authorize(Policy = "Owner")]
    public async Task<IActionResult> Reactivar(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.ReactivarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
