using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>
/// El perfil público del profe, del lado de quien lo edita. Policy "Profesor": tanto
/// el dueño como sus empleados editan EL SUYO (el userId sale del token, nunca de la
/// URL, así nadie puede tocar el perfil de otro).
/// </summary>
[ApiController]
[Authorize(Policy = "Profesor")]
[Route("api/perfil-profesor")]
public class PerfilProfesorController : ControllerBase
{
    /// <summary>Techo del request; el Service vuelve a validar el tamaño real de la imagen.</summary>
    private const int TamañoMaximo = 6 * 1024 * 1024;

    private readonly IPerfilProfesorService _perfiles;

    public PerfilProfesorController(IPerfilProfesorService perfiles)
    {
        _perfiles = perfiles;
    }

    [HttpGet("mio")]
    public async Task<ActionResult<MiPerfilProfesorDto>> ObtenerMio(CancellationToken ct) =>
        Ok(await _perfiles.ObtenerMioAsync(ct));

    [HttpPut("mio")]
    public async Task<ActionResult<MiPerfilProfesorDto>> GuardarMio(GuardarPerfilProfesorDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _perfiles.GuardarMioAsync(dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // ── Portada y avatar ──

    [HttpPost("mio/portada")]
    [RequestSizeLimit(TamañoMaximo)]
    public async Task<ActionResult<ImagenSubidaDto>> SubirPortada(IFormFile archivo, CancellationToken ct) =>
        await SubirAsync(archivo, imagen => _perfiles.SubirPortadaAsync(imagen, ct), ct);

    [HttpPost("mio/avatar")]
    [RequestSizeLimit(TamañoMaximo)]
    public async Task<ActionResult<ImagenSubidaDto>> SubirAvatar(IFormFile archivo, CancellationToken ct) =>
        await SubirAsync(archivo, imagen => _perfiles.SubirAvatarAsync(imagen, ct), ct);

    [HttpDelete("mio/portada")]
    public async Task<IActionResult> QuitarPortada(CancellationToken ct)
    {
        await _perfiles.QuitarPortadaAsync(ct);
        return NoContent();
    }

    [HttpDelete("mio/avatar")]
    public async Task<IActionResult> QuitarAvatar(CancellationToken ct)
    {
        await _perfiles.QuitarAvatarAsync(ct);
        return NoContent();
    }

    // ── Galería ──

    [HttpPost("mio/fotos")]
    [RequestSizeLimit(TamañoMaximo)]
    public async Task<ActionResult<FotoPerfilDto>> AgregarFoto(
        IFormFile archivo, [FromForm] string? pieDeFoto, CancellationToken ct)
    {
        try
        {
            var imagen = await LeerAsync(archivo, ct);
            return Ok(await _perfiles.AgregarFotoAsync(imagen, pieDeFoto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPatch("mio/fotos/{id:guid}")]
    public async Task<IActionResult> CambiarPieDeFoto(Guid id, GuardarPieDeFotoDto dto, CancellationToken ct)
    {
        try
        {
            await _perfiles.CambiarPieDeFotoAsync(id, dto.PieDeFoto, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpDelete("mio/fotos/{id:guid}")]
    public async Task<IActionResult> EliminarFoto(Guid id, CancellationToken ct)
    {
        try
        {
            await _perfiles.EliminarFotoAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPut("mio/fotos/orden")]
    public async Task<IActionResult> ReordenarFotos(ReordenarDto dto, CancellationToken ct)
    {
        try
        {
            await _perfiles.ReordenarFotosAsync(dto.Ids, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // ── Trayectoria ──

    [HttpPost("mio/hitos")]
    public async Task<ActionResult<HitoTrayectoriaDto>> AgregarHito(GuardarHitoDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _perfiles.AgregarHitoAsync(dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPut("mio/hitos/{id:guid}")]
    public async Task<IActionResult> EditarHito(Guid id, GuardarHitoDto dto, CancellationToken ct)
    {
        try
        {
            await _perfiles.EditarHitoAsync(id, dto, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpDelete("mio/hitos/{id:guid}")]
    public async Task<IActionResult> EliminarHito(Guid id, CancellationToken ct)
    {
        try
        {
            await _perfiles.EliminarHitoAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPut("mio/hitos/orden")]
    public async Task<IActionResult> ReordenarHitos(ReordenarDto dto, CancellationToken ct)
    {
        try
        {
            await _perfiles.ReordenarHitosAsync(dto.Ids, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // ── Ayudantes de subida ──

    private async Task<ActionResult<ImagenSubidaDto>> SubirAsync(
        IFormFile archivo, Func<ImagenSubida, Task<ImagenSubidaDto>> subir, CancellationToken ct)
    {
        try
        {
            return Ok(await subir(await LeerAsync(archivo, ct)));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Pasa el archivo a bytes para el Service (que no conoce HTTP). El tamaño ya
    /// quedó acotado por RequestSizeLimit, así que entra en memoria sin riesgo.
    /// </summary>
    private static async Task<ImagenSubida> LeerAsync(IFormFile? archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
            throw new ReglaDeNegocioException("No llegó ninguna imagen.");

        using var memoria = new MemoryStream();
        await using (var origen = archivo.OpenReadStream())
            await origen.CopyToAsync(memoria, ct);

        return new ImagenSubida(memoria.ToArray());
    }
}
