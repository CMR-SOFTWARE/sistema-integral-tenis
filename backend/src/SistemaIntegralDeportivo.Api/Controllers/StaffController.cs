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
}
