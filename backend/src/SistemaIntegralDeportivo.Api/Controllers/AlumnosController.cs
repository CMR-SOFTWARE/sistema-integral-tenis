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
    private readonly INotaAlumnoService _notas;

    public AlumnosController(IAlumnoService service, INotaAlumnoService notas)
    {
        _service = service;
        _notas = notas;
    }

    /// <summary>GET api/alumnos?categoria=Cuarta&amp;estado=Activo</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlumnoResponseDto>>> Listar(
        [FromQuery] CategoriaAlumno? categoria,
        [FromQuery] EstadoAlumno? estado,
        CancellationToken ct) =>
        Ok(await _service.ListarAsync(categoria, estado, ct));

    /// <summary>
    /// GET api/alumnos/morosos — activos con la cuota impaga pasado el día 15
    /// (el profe decide si los saca del calendario; nunca es automático).
    /// </summary>
    [HttpGet("morosos")]
    [Authorize(Policy = "Owner")] // morosidad es plata: solo el dueño
    public async Task<ActionResult<IReadOnlyList<MorosoDto>>> Morosos(CancellationToken ct) =>
        Ok(await _service.ListarMorososAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AlumnoResponseDto>> Obtener(Guid id, CancellationToken ct)
    {
        var alumno = await _service.ObtenerAsync(id, ct);
        return alumno is null ? NotFound() : Ok(alumno);
    }

    /// <summary>POST api/alumnos — alta CON credenciales (la temporal viaja una sola vez).</summary>
    [HttpPost]
    [Authorize(Policy = "Owner")]
    public async Task<ActionResult<AlumnoCreadoDto>> Crear(CreateAlumnoDto dto, CancellationToken ct)
    {
        try
        {
            var creado = await _service.CrearAsync(dto, ct);
            return CreatedAtAction(nameof(Obtener), new { id = creado.Alumno.Id }, creado);
        }
        catch (ReglaDeNegocioException ex)
        {
            // Violación de regla de negocio → 400 con ProblemDetails (detail legible)
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/alumnos/{id}/acceso — credenciales para una ficha vieja sin usuario.</summary>
    [HttpPost("{id:guid}/acceso")]
    [Authorize(Policy = "Owner")]
    public async Task<ActionResult<AccesoCreadoDto>> CrearAcceso(
        Guid id, CrearAccesoDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.CrearAccesoAsync(id, dto.Telefono, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>PUT api/alumnos/{id} — el profe corrige los datos de la ficha.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Owner")]
    public async Task<ActionResult<AlumnoResponseDto>> Editar(
        Guid id, UpdateAlumnoDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.EditarAsync(id, dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>PATCH api/alumnos/{id}/estado — pausar (Suspendido) / reactivar (Activo).</summary>
    [HttpPatch("{id:guid}/estado")]
    [Authorize(Policy = "Owner")]
    public async Task<ActionResult<AlumnoResponseDto>> CambiarEstado(
        Guid id, UpdateEstadoDto dto, CancellationToken ct)
    {
        var actualizado = await _service.CambiarEstadoAsync(id, dto.Estado, ct);
        return actualizado is null ? NotFound() : Ok(actualizado);
    }

    /// <summary>DELETE api/alumnos/{id} — baja LÓGICA (estado → Inactivo).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Owner")]
    public async Task<IActionResult> DarDeBaja(Guid id, CancellationToken ct) =>
        await _service.DarDeBajaAsync(id, ct) ? NoContent() : NotFound();

    // ── Notas de seguimiento del profe sobre el alumno ──

    /// <summary>GET api/alumnos/{id}/notas — todas las notas (privadas y compartidas).</summary>
    [HttpGet("{id:guid}/notas")]
    public async Task<ActionResult<IReadOnlyList<NotaAlumnoDto>>> Notas(Guid id, CancellationToken ct) =>
        Ok(await _notas.ListarAsync(id, soloCompartidas: false, ct));

    [HttpPost("{id:guid}/notas")]
    public async Task<ActionResult<NotaAlumnoDto>> CrearNota(Guid id, CrearNotaAlumnoDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _notas.CrearAsync(id, dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpDelete("{id:guid}/notas/{notaId:guid}")]
    public async Task<IActionResult> BorrarNota(Guid id, Guid notaId, CancellationToken ct)
    {
        try
        {
            await _notas.EliminarAsync(notaId, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
