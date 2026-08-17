using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

[ApiController]
[Authorize(Policy = "Owner")]
[Route("api/configuracion")]
public class ConfiguracionController : ControllerBase
{
    /// <summary>Tope del request de subida; el service valida los bytes de nuevo (6 MB).</summary>
    private const int TamañoMaximoSubida = 6 * 1024 * 1024;

    private readonly IConfigService _service;
    private readonly IServicioService _servicios;
    private readonly IPublicidadService _publicidad;

    public ConfiguracionController(IConfigService service, IServicioService servicios, IPublicidadService publicidad)
    {
        _service = service;
        _servicios = servicios;
        _publicidad = publicidad;
    }

    [HttpGet("precios")]
    public async Task<ActionResult<PreciosDto>> Precios(CancellationToken ct) =>
        Ok(await _service.ObtenerPreciosAsync(ct));

    [HttpPut("precios")]
    public async Task<ActionResult<PreciosDto>> ActualizarPrecios(PreciosDto dto, CancellationToken ct) =>
        Ok(await _service.ActualizarPreciosAsync(dto, ct));

    /// <summary>Datos de transferencia (alias/CBU + titular) que ve el alumno al informar un pago.</summary>
    [HttpGet("datos-pago")]
    public async Task<ActionResult<DatosPagoConfigDto>> DatosPago(CancellationToken ct) =>
        Ok(await _service.ObtenerDatosPagoAsync(ct));

    [HttpPut("datos-pago")]
    public async Task<ActionResult<DatosPagoConfigDto>> ActualizarDatosPago(
        DatosPagoConfigDto dto, CancellationToken ct) =>
        Ok(await _service.ActualizarDatosPagoAsync(dto, ct));

    /// <summary>El dueño = Director: si da clases, se ofrece como profe asignable.</summary>
    [HttpGet("director")]
    public async Task<ActionResult<DirectorConfigDto>> Director(CancellationToken ct) =>
        Ok(await _service.ObtenerDirectorAsync(ct));

    [HttpPut("director")]
    public async Task<ActionResult<DirectorConfigDto>> ActualizarDirector(
        DirectorConfigDto dto, CancellationToken ct) =>
        Ok(await _service.ActualizarDirectorAsync(dto, ct));

    // ── Catálogo de servicios que ofrece el profe (M4) ──

    /// <summary>Todos los servicios del catálogo (activos e inactivos).</summary>
    [HttpGet("servicios")]
    public async Task<ActionResult<IReadOnlyList<ServicioDto>>> Servicios(CancellationToken ct) =>
        Ok(await _servicios.ListarAsync(soloActivos: false, ct));

    [HttpPost("servicios")]
    public async Task<ActionResult<ServicioDto>> CrearServicio(GuardarServicioDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _servicios.CrearAsync(dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPut("servicios/{id:guid}")]
    public async Task<ActionResult<ServicioDto>> EditarServicio(Guid id, GuardarServicioDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _servicios.EditarAsync(id, dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Baja/reactivación lógica del servicio (no se borra: hay pedidos que lo referencian).</summary>
    [HttpPatch("servicios/{id:guid}/activo")]
    public async Task<ActionResult<ServicioDto>> CambiarActivoServicio(Guid id, CambiarActivoDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _servicios.CambiarActivoAsync(id, dto.Activo, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// POST api/configuracion/servicios/{id}/fotos — sube una foto del producto.
    /// La imagen va al STORAGE y en la base queda su URL (mismo camino que las fotos del
    /// perfil), no en base64 dentro de la fila: con veinte productos eso serían megas
    /// viajando en cada carga del catálogo.
    /// </summary>
    [HttpPost("servicios/{id:guid}/fotos")]
    [RequestSizeLimit(TamañoMaximoSubida)]
    public async Task<ActionResult<FotoServicioDto>> SubirFoto(Guid id, IFormFile archivo, CancellationToken ct)
    {
        try
        {
            return Ok(await _servicios.AgregarFotoAsync(id, await LeerImagenAsync(archivo, ct), ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>DELETE api/configuracion/servicios/{id}/fotos/{fotoId} — borra la foto y su archivo.</summary>
    [HttpDelete("servicios/{id:guid}/fotos/{fotoId:guid}")]
    public async Task<IActionResult> BorrarFoto(Guid id, Guid fotoId, CancellationToken ct)
    {
        try
        {
            await _servicios.BorrarFotoAsync(id, fotoId, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Pasa el archivo a bytes para el Service (que no conoce HTTP).</summary>
    private static async Task<ImagenSubida> LeerImagenAsync(IFormFile? archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
            throw new ReglaDeNegocioException("No llegó ninguna imagen.");

        using var memoria = new MemoryStream();
        await using (var origen = archivo.OpenReadStream())
            await origen.CopyToAsync(memoria, ct);

        return new ImagenSubida(memoria.ToArray());
    }

    // ── Publicidad: los banners del club (M6) ──

    /// <summary>Todos los banners (activos e inactivos).</summary>
    [HttpGet("publicidad")]
    public async Task<ActionResult<IReadOnlyList<PublicidadDto>>> Publicidad(CancellationToken ct) =>
        Ok(await _publicidad.ListarAsync(soloActivas: false, ct));

    [HttpPost("publicidad")]
    public async Task<ActionResult<PublicidadDto>> CrearPublicidad(GuardarPublicidadDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _publicidad.CrearAsync(dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPatch("publicidad/{id:guid}/activo")]
    public async Task<ActionResult<PublicidadDto>> CambiarActivoPublicidad(Guid id, CambiarActivoPublicidadDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _publicidad.CambiarActivoAsync(id, dto.Activo, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpDelete("publicidad/{id:guid}")]
    public async Task<IActionResult> BorrarPublicidad(Guid id, CancellationToken ct)
    {
        try
        {
            await _publicidad.EliminarAsync(id, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
