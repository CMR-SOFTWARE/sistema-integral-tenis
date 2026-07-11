using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

[ApiController]
[Authorize(Policy = "Profesor")]
[Route("api/grupos")]
public class GruposController : ControllerBase
{
    private readonly IGrupoService _service;

    public GruposController(IGrupoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GrupoResponseDto>>> Listar(CancellationToken ct) =>
        Ok(await _service.ListarAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GrupoResponseDto>> Obtener(Guid id, CancellationToken ct)
    {
        var grupo = await _service.ObtenerAsync(id, ct);
        return grupo is null ? NotFound() : Ok(grupo);
    }

    [HttpPost]
    public async Task<ActionResult<GrupoResponseDto>> Crear(CreateGrupoDto dto, CancellationToken ct)
    {
        var creado = await _service.CrearAsync(dto, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>POST api/grupos/{id}/alumnos — asignar un alumno al grupo.</summary>
    [HttpPost("{id:guid}/alumnos")]
    public async Task<IActionResult> AsignarAlumno(Guid id, AsignarAlumnoDto dto, CancellationToken ct)
    {
        try
        {
            await _service.AsignarAlumnoAsync(id, dto.AlumnoId, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>DELETE api/grupos/{id}/alumnos/{alumnoId} — baja de la membresía (con historia).</summary>
    [HttpDelete("{id:guid}/alumnos/{alumnoId:guid}")]
    public async Task<IActionResult> QuitarAlumno(Guid id, Guid alumnoId, CancellationToken ct)
    {
        try
        {
            await _service.QuitarAlumnoAsync(id, alumnoId, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
