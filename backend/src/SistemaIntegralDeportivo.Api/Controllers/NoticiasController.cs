using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>Noticias del club. Las publica el director (dueño del tenant).</summary>
[ApiController]
[Authorize(Policy = "Owner")]
[Route("api/noticias")]
public class NoticiasController : ControllerBase
{
    private readonly INoticiaService _noticias;

    public NoticiasController(INoticiaService noticias)
    {
        _noticias = noticias;
    }

    /// <summary>Todas las noticias (activas, apagadas y vencidas) para gestionarlas.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NoticiaDto>>> Listar(CancellationToken ct) =>
        Ok(await _noticias.ListarAsync(soloVigentes: false, ct));

    [HttpPost]
    public async Task<ActionResult<NoticiaDto>> Crear(GuardarNoticiaDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _noticias.CrearAsync(dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<NoticiaDto>> Editar(Guid id, GuardarNoticiaDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _noticias.EditarAsync(id, dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPatch("{id:guid}/activo")]
    public async Task<ActionResult<NoticiaDto>> CambiarActivo(Guid id, CambiarActivoNoticiaDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _noticias.CambiarActivoAsync(id, dto.Activo, ct));
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
            await _noticias.EliminarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
