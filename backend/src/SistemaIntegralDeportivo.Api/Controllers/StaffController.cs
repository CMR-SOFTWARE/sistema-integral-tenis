using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>
/// Profes empleados (Staff) del club. Solo el DUEÑO los administra (policy Owner):
/// un profe empleado no gestiona a otros profes.
/// </summary>
[ApiController]
[Authorize(Policy = "Owner")]
[Route("api/staff")]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staff;

    public StaffController(IStaffService staff)
    {
        _staff = staff;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StaffDto>>> Listar(CancellationToken ct) =>
        Ok(await _staff.ListarAsync(ct));

    /// <summary>Profes a los que asignar horarios/grupos/alumnos: el dueño + los staff activos.</summary>
    [HttpGet("asignables")]
    public async Task<ActionResult<IReadOnlyList<ProfesorAsignableDto>>> Asignables(CancellationToken ct) =>
        Ok(await _staff.ListarAsignablesAsync(ct));

    [HttpPost]
    public async Task<ActionResult<StaffCreadoDto>> Agregar(AgregarStaffDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _staff.AgregarAsync(dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/staff/desvincularme — el propio profe empleado se va del club.</summary>
    [HttpPost("desvincularme")]
    [Authorize(Policy = "Profesor")] // lo hace el staff sobre sí mismo, no el dueño
    public async Task<IActionResult> Desvincularme(CancellationToken ct)
    {
        try
        {
            await _staff.DesvincularmeAsync(ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPatch("{id:guid}/activo")]
    public async Task<IActionResult> CambiarActivo(Guid id, CambiarActivoStaffDto dto, CancellationToken ct)
    {
        try
        {
            await _staff.CambiarActivoAsync(id, dto.Activo, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>PATCH api/staff/{id}/valor-hora — setea el valor hora base del profe.</summary>
    [HttpPatch("{id:guid}/valor-hora")]
    public async Task<IActionResult> CambiarValorHora(Guid id, ValorHoraStaffDto dto, CancellationToken ct)
    {
        try
        {
            await _staff.CambiarValorHoraAsync(id, dto.ValorHora, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// DELETE api/staff/{id}/definitivo — borrado REAL del profe empleado (sin vuelta).
    /// Lo que lo tenía de profe queda sin asignar; su login se va si no cumple otro rol.
    /// </summary>
    [HttpDelete("{id:guid}/definitivo")]
    public async Task<IActionResult> EliminarDefinitivo(Guid id, CancellationToken ct)
    {
        try
        {
            await _staff.EliminarDefinitivoAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
