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
    private readonly ISolicitudGrupoService _solicitudes;

    public GruposController(IGrupoService service, ISolicitudGrupoService solicitudes)
    {
        _service = service;
        _solicitudes = solicitudes;
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

    // ── Solicitudes de alumnos para sumarse a un grupo (M5a) ──

    /// <summary>GET api/grupos/solicitudes — solicitudes pendientes de sumarse a un grupo.</summary>
    [HttpGet("solicitudes")]
    public async Task<ActionResult<IReadOnlyList<SolicitudGrupoDto>>> Solicitudes(CancellationToken ct) =>
        Ok(await _solicitudes.ListarPendientesAsync(ct));

    /// <summary>POST api/grupos/solicitudes/{id}/aceptar — acepto: sumo al alumno al grupo.</summary>
    [HttpPost("solicitudes/{id:guid}/aceptar")]
    public async Task<IActionResult> AceptarSolicitud(Guid id, CancellationToken ct)
    {
        try
        {
            await _solicitudes.AceptarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/grupos/solicitudes/{id}/rechazar — rechazo la solicitud.</summary>
    [HttpPost("solicitudes/{id:guid}/rechazar")]
    public async Task<IActionResult> RechazarSolicitud(Guid id, CancellationToken ct)
    {
        try
        {
            await _solicitudes.RechazarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
