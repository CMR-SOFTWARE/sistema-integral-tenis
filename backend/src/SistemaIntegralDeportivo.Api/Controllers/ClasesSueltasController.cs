using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>Las clases sueltas de los alumnos que el profe resuelve (M5c).</summary>
[ApiController]
[Authorize(Policy = "Owner")]
[Route("api/clases-sueltas")]
public class ClasesSueltasController : ControllerBase
{
    private readonly IClaseSueltaService _service;

    public ClasesSueltasController(IClaseSueltaService service)
    {
        _service = service;
    }

    /// <summary>GET api/clases-sueltas — pendientes de resolver (con el estado del pago).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClaseSueltaDto>>> Pendientes(CancellationToken ct) =>
        Ok(await _service.ListarPendientesAsync(ct));

    /// <summary>GET api/clases-sueltas/{id}/canchas-libres — canchas libres de esa sede/fecha/hora.</summary>
    [HttpGet("{id:guid}/canchas-libres")]
    public async Task<ActionResult<IReadOnlyList<CanchaLibreDto>>> CanchasLibres(Guid id, CancellationToken ct) =>
        Ok(await _service.CanchasLibresParaClaseAsync(id, ct));

    /// <summary>
    /// GET api/clases-sueltas/canchas-libres — canchas libres de una combinación que
    /// TODAVÍA no es una clase (el profe está armándola) o que se está reprogramando
    /// (con <paramref name="excluirTurnoId"/>, para que el turno no se bloquee a sí
    /// mismo). La de arriba parte de una clase ya pedida por el alumno.
    /// </summary>
    [HttpGet("canchas-libres")]
    public async Task<ActionResult<IReadOnlyList<CanchaLibreDto>>> CanchasLibresPara(
        [FromQuery] Guid sedeId, [FromQuery] DateOnly fecha, [FromQuery] TimeOnly horaInicio,
        [FromQuery] int duracionMinutos, [FromQuery] Guid? excluirTurnoId, CancellationToken ct) =>
        Ok(await _service.CanchasLibresAsync(sedeId, fecha, horaInicio, duracionMinutos, ct, excluirTurnoId));

    /// <summary>
    /// POST api/clases-sueltas — el profe ASIGNA una clase suelta (nace confirmada, con su
    /// turno). Con generaCargo=false es una clase de prueba y no se le cobra.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ClaseSueltaDto>> Asignar(AsignarClaseSueltaDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.AsignarAsync(
                dto.AlumnoId, dto.CanchaId, dto.Fecha, dto.HoraInicio, dto.DuracionMinutos,
                dto.ProfesorUserId, dto.GeneraCargo, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/clases-sueltas/{id}/confirmar — elijo cancha: nace el turno suelto y se marca pagado.</summary>
    [HttpPost("{id:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid id, ConfirmarClaseSueltaDto dto, CancellationToken ct)
    {
        try
        {
            await _service.ConfirmarAsync(id, dto.CanchaId, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/clases-sueltas/{id}/rechazar — rechazo (se borra el cargo).</summary>
    [HttpPost("{id:guid}/rechazar")]
    public async Task<IActionResult> Rechazar(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.RechazarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// PUT api/clases-sueltas/turno/{turnoId} — reprograma una clase suelta ya asignada
    /// (solo lo agendable: fecha, hora, cancha, duración, profe). Va por el turnoId y no
    /// por el id de la clase suelta porque es lo que tiene la Agenda a mano.
    /// </summary>
    [HttpPut("turno/{turnoId:guid}")]
    public async Task<IActionResult> Editar(Guid turnoId, EditarClaseSueltaDto dto, CancellationToken ct)
    {
        try
        {
            await _service.EditarAsync(
                turnoId, dto.CanchaId, dto.Fecha, dto.HoraInicio, dto.DuracionMinutos,
                dto.ProfesorUserId, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
