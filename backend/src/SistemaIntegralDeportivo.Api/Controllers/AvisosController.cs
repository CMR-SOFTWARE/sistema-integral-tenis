using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>Avisos generales del club (tablón). Los administra el profe.</summary>
[ApiController]
[Authorize(Policy = "Owner")]
[Route("api/avisos")]
public class AvisosController : ControllerBase
{
    private readonly IAvisoService _avisos;

    public AvisosController(IAvisoService avisos)
    {
        _avisos = avisos;
    }

    /// <summary>Todos los avisos (activos, apagados y vencidos) para gestionarlos.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AvisoDto>>> Listar(CancellationToken ct) =>
        Ok(await _avisos.ListarAsync(soloVigentes: false, ct));

    [HttpPost]
    public async Task<ActionResult<AvisoDto>> Crear(GuardarAvisoDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _avisos.CrearAsync(dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPatch("{id:guid}/activo")]
    public async Task<ActionResult<AvisoDto>> CambiarActivo(Guid id, CambiarActivoAvisoDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _avisos.CambiarActivoAsync(id, dto.Activo, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Borrar(Guid id, CancellationToken ct)
    {
        try
        {
            await _avisos.EliminarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
