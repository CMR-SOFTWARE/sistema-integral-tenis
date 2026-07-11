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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SedeResponseDto>>> Listar(CancellationToken ct) =>
        Ok(await _service.ListarAsync(ct));

    [HttpPost]
    public async Task<ActionResult<SedeResponseDto>> Crear(CreateSedeDto dto, CancellationToken ct) =>
        Ok(await _service.CrearAsync(dto, ct));

    [HttpPost("{id:guid}/canchas")]
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
}
