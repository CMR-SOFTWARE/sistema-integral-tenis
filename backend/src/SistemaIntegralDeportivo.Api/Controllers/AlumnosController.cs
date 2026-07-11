using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>
/// Puerta HTTP del módulo Alumnos: recibe, delega al service y responde.
/// Acá NO hay reglas de negocio ni acceso a datos.
/// </summary>
[ApiController]
[Authorize(Policy = "Profesor")]
[Route("api/alumnos")]
public class AlumnosController : ControllerBase
{
    private readonly IAlumnoService _service;

    public AlumnosController(IAlumnoService service)
    {
        _service = service;
    }

    /// <summary>GET api/alumnos?categoria=Cuarta&amp;estado=Activo</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlumnoResponseDto>>> Listar(
        [FromQuery] CategoriaAlumno? categoria,
        [FromQuery] EstadoAlumno? estado,
        CancellationToken ct) =>
        Ok(await _service.ListarAsync(categoria, estado, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AlumnoResponseDto>> Obtener(Guid id, CancellationToken ct)
    {
        var alumno = await _service.ObtenerAsync(id, ct);
        return alumno is null ? NotFound() : Ok(alumno);
    }

    [HttpPost]
    public async Task<ActionResult<AlumnoResponseDto>> Crear(CreateAlumnoDto dto, CancellationToken ct)
    {
        try
        {
            var creado = await _service.CrearAsync(dto, ct);
            return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
        }
        catch (ReglaDeNegocioException ex)
        {
            // Violación de regla de negocio → 400 con ProblemDetails (detail legible)
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>PATCH api/alumnos/{id}/estado — pausar (Suspendido) / reactivar (Activo).</summary>
    [HttpPatch("{id:guid}/estado")]
    public async Task<ActionResult<AlumnoResponseDto>> CambiarEstado(
        Guid id, UpdateEstadoDto dto, CancellationToken ct)
    {
        var actualizado = await _service.CambiarEstadoAsync(id, dto.Estado, ct);
        return actualizado is null ? NotFound() : Ok(actualizado);
    }

    /// <summary>DELETE api/alumnos/{id} — baja LÓGICA (estado → Inactivo).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DarDeBaja(Guid id, CancellationToken ct) =>
        await _service.DarDeBajaAsync(id, ct) ? NoContent() : NotFound();
}
