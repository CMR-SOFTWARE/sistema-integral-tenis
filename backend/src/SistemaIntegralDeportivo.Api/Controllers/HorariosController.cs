using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

[ApiController]
[Authorize(Policy = "Profesor")] // dueño Y staff; el staff queda acotado a su club en el service
[Route("api/horarios")]
public class HorariosController : ControllerBase
{
    private readonly IHorarioService _service;
    private readonly ISolicitudHorarioService _solicitudes;
    private readonly ISolicitudCupoService _cupos;

    public HorariosController(
        IHorarioService service, ISolicitudHorarioService solicitudes, ISolicitudCupoService cupos)
    {
        _service = service;
        _solicitudes = solicitudes;
        _cupos = cupos;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HorarioResponseDto>>> Listar(CancellationToken ct) =>
        Ok(await _service.ListarAsync(ct));

    [HttpPost]
    public async Task<ActionResult<HorarioResponseDto>> Crear(CreateHorarioDto dto, CancellationToken ct)
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

    /// <summary>PATCH api/horarios/{id}/profesor — (re)asigna el profe de la clase.</summary>
    [HttpPatch("{id:guid}/profesor")]
    public async Task<ActionResult<HorarioResponseDto>> AsignarProfesor(
        Guid id, AsignarProfesorDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.AsignarProfesorAsync(id, dto.ProfesorUserId, dto.ValorHoraProfe, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>PUT api/horarios/{id} — edita el horario (cancha/día/hora/duración/profe/valor hora).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<HorarioResponseDto>> Editar(
        Guid id, UpdateHorarioDto dto, CancellationToken ct)
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

    /// <summary>DELETE api/horarios/{id} — desactiva la plantilla (turnos generados intactos).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Owner")] // desactivar un horario es del dueño; el staff solo crea/edita/reasigna
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

    // ── Solicitudes de clase individual fija de los alumnos (M5b) ──
    // Resolverlas es del dueño (el staff no ve el panel de solicitudes).

    /// <summary>GET api/horarios/solicitudes — solicitudes de clase individual pendientes.</summary>
    [HttpGet("solicitudes")]
    [Authorize(Policy = "Owner")]
    public async Task<ActionResult<IReadOnlyList<SolicitudHorarioDto>>> Solicitudes(CancellationToken ct) =>
        Ok(await _solicitudes.ListarPendientesAsync(ct));

    /// <summary>GET api/horarios/solicitudes/{id}/canchas-libres — canchas libres de la SEDE que pidió el alumno.</summary>
    [HttpGet("solicitudes/{id:guid}/canchas-libres")]
    [Authorize(Policy = "Owner")]
    public async Task<ActionResult<IReadOnlyList<CanchaLibreDto>>> CanchasLibres(Guid id, CancellationToken ct) =>
        Ok(await _solicitudes.CanchasLibresParaSolicitudAsync(id, ct));

    /// <summary>POST api/horarios/solicitudes/{id}/aceptar — acepto eligiendo una cancha: crea el horario.</summary>
    [HttpPost("solicitudes/{id:guid}/aceptar")]
    [Authorize(Policy = "Owner")]
    public async Task<IActionResult> AceptarSolicitud(Guid id, AceptarHorarioDto dto, CancellationToken ct)
    {
        try
        {
            await _solicitudes.AceptarAsync(id, dto.CanchaId, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // ── Pedidos de lugar en una clase (el alumno los manda desde el portal) ──

    /// <summary>GET api/horarios/solicitudes-cupo — pedidos de lugar pendientes.</summary>
    [HttpGet("solicitudes-cupo")]
    [Authorize(Policy = "Owner")]
    public async Task<ActionResult<IReadOnlyList<SolicitudCupoDto>>> SolicitudesCupo(CancellationToken ct) =>
        Ok(await _cupos.ListarPendientesAsync(ct));

    /// <summary>POST api/horarios/solicitudes-cupo/{id}/aceptar — lo sumo a la clase.</summary>
    [HttpPost("solicitudes-cupo/{id:guid}/aceptar")]
    [Authorize(Policy = "Owner")]
    public async Task<IActionResult> AceptarCupo(Guid id, CancellationToken ct)
    {
        try
        {
            await _cupos.AceptarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/horarios/solicitudes-cupo/{id}/rechazar — rechazo el pedido.</summary>
    [HttpPost("solicitudes-cupo/{id:guid}/rechazar")]
    [Authorize(Policy = "Owner")]
    public async Task<IActionResult> RechazarCupo(Guid id, CancellationToken ct)
    {
        try
        {
            await _cupos.RechazarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // ── El roster: quiénes vienen a la clase ──

    /// <summary>POST api/horarios/{id}/alumnos — suma un alumno a la clase (respeta el cupo).</summary>
    [HttpPost("{id:guid}/alumnos")]
    public async Task<IActionResult> AgregarAlumno(Guid id, AgregarAlumnoHorarioDto dto, CancellationToken ct)
    {
        try
        {
            await _service.AgregarAlumnoAsync(id, dto.AlumnoId, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>DELETE api/horarios/{id}/alumnos/{alumnoId} — lo saca de la clase (baja lógica).</summary>
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

    /// <summary>POST api/horarios/solicitudes/{id}/rechazar — rechazo la solicitud.</summary>
    [HttpPost("solicitudes/{id:guid}/rechazar")]
    [Authorize(Policy = "Owner")]
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
