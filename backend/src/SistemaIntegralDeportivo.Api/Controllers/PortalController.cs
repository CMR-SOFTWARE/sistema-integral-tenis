using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>
/// El portal del ALUMNO logueado. Alcanza con estar autenticado: el service
/// resuelve la ficha desde el userId del token, nunca desde parámetros.
/// </summary>
[ApiController]
[Route("api/portal")]
[Authorize]
public class PortalController : ControllerBase
{
    private readonly IPortalService _portal;
    private readonly ISolicitudService _solicitudes;
    private readonly Microsoft.AspNetCore.Identity.UserManager<Models.Usuario> _userManager;

    public PortalController(
        IPortalService portal, ISolicitudService solicitudes,
        Microsoft.AspNetCore.Identity.UserManager<Models.Usuario> userManager)
    {
        _portal = portal;
        _solicitudes = solicitudes;
        _userManager = userManager;
    }

    /// <summary>GET api/portal/solicitudes — mis solicitudes con estado.</summary>
    [HttpGet("solicitudes")]
    public async Task<ActionResult<IReadOnlyList<MiSolicitudDto>>> MisSolicitudes(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        return Ok(await _solicitudes.MisAsync(userId, ct));
    }

    /// <summary>POST api/portal/solicitudes — pido entrar a un club.</summary>
    [HttpPost("solicitudes")]
    public async Task<ActionResult<IReadOnlyList<MiSolicitudDto>>> CrearSolicitud(
        CrearSolicitudDto dto, CancellationToken ct)
    {
        var sub = User.FindFirst("sub")?.Value;
        var usuario = sub is null ? null : await _userManager.FindByIdAsync(sub);
        if (usuario is null) return Unauthorized();

        try
        {
            return Ok(await _solicitudes.CrearAsync(usuario, dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/portal/mis-turnos — próximas clases + historial reciente.</summary>
    [HttpGet("mis-turnos")]
    public async Task<ActionResult<MisTurnosDto>> MisTurnos(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.MisTurnosAsync(userId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/portal/mi-cuota/2026/7 — mi liquidación del mes (204 si no hay movimientos).</summary>
    [HttpGet("mi-cuota/{anio:int}/{mes:int:range(1,12)}")]
    public async Task<ActionResult<AlumnoLiquidacionDto>> MiCuota(int anio, int mes, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            var mia = await _portal.MiCuotaAsync(userId, anio, mes, ct);
            return mia is null ? NoContent() : Ok(mia);
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/portal/datos-pago — a dónde transfiero (alias/CBU del club).</summary>
    [HttpGet("datos-pago")]
    public async Task<ActionResult<DatosPagoDto>> DatosPago(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.DatosPagoAsync(userId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/portal/mi-cuota/2026/7/informar — aviso que transferí el mes.</summary>
    [HttpPost("mi-cuota/{anio:int}/{mes:int:range(1,12)}/informar")]
    public async Task<IActionResult> InformarMes(int anio, int mes, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            await _portal.InformarPagoMesAsync(userId, anio, mes, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/portal/cargos/{id}/informar — aviso que transferí un cargo puntual.</summary>
    [HttpPost("cargos/{cargoId:guid}/informar")]
    public async Task<IActionResult> InformarCargo(Guid cargoId, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            await _portal.InformarPagoCargoAsync(userId, cargoId, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/portal/perfil — mi ficha, como me ve el club.</summary>
    [HttpGet("perfil")]
    public async Task<ActionResult<MiPerfilDto>> Perfil(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.MiPerfilAsync(userId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>PUT api/portal/perfil — edito MIS datos de contacto (teléfono/email).</summary>
    [HttpPut("perfil")]
    public async Task<ActionResult<MiPerfilDto>> ActualizarPerfil(
        ActualizarMiPerfilDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.ActualizarMiPerfilAsync(userId, dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/portal/mis-turnos/{id}/cancelar — aviso que no vengo (mi cargo queda).</summary>
    [HttpPost("mis-turnos/{turnoId:guid}/cancelar")]
    public async Task<IActionResult> CancelarMiTurno(
        Guid turnoId, CancelarMiTurnoDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            await _portal.CancelarMiTurnoAsync(userId, turnoId, dto.Motivo, ct);
            return NoContent();
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private Guid? UserId() =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
}
