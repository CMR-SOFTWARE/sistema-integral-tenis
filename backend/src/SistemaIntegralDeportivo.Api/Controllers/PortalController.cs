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

    /// <summary>GET api/portal/servicios — el catálogo del club (lo que puedo pedir).</summary>
    [HttpGet("servicios")]
    public async Task<ActionResult<IReadOnlyList<ServicioDto>>> Servicios(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.ServiciosAsync(userId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/portal/pedidos — pido un servicio (queda pendiente del profe).</summary>
    [HttpPost("pedidos")]
    public async Task<ActionResult<PedidoDto>> Pedir(CrearPedidoDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.PedirServicioAsync(userId, dto.ServicioId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/portal/pedidos — mis pedidos con su estado.</summary>
    [HttpGet("pedidos")]
    public async Task<ActionResult<IReadOnlyList<PedidoDto>>> MisPedidos(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.MisPedidosAsync(userId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/portal/grupos-disponibles — grupos a los que me podría sumar (cupo + mi categoría).</summary>
    [HttpGet("grupos-disponibles")]
    public async Task<ActionResult<IReadOnlyList<GrupoDisponibleDto>>> GruposDisponibles(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.GruposDisponiblesAsync(userId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/portal/solicitudes-grupo — pido sumarme a un grupo (pendiente del profe).</summary>
    [HttpPost("solicitudes-grupo")]
    public async Task<ActionResult<SolicitudGrupoDto>> SolicitarGrupo(SolicitarGrupoDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.SolicitarGrupoAsync(userId, dto.GrupoId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/portal/solicitudes-grupo — mis solicitudes de grupo con su estado.</summary>
    [HttpGet("solicitudes-grupo")]
    public async Task<ActionResult<IReadOnlyList<SolicitudGrupoDto>>> MisSolicitudesGrupo(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.MisSolicitudesGrupoAsync(userId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/portal/sedes — las sedes del club (para elegir dónde quiero la clase).</summary>
    [HttpGet("sedes")]
    public async Task<ActionResult<IReadOnlyList<SedeReservaDto>>> Sedes(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.SedesAsync(userId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/portal/hay-lugar?sede={id}&amp;dia=Tuesday&amp;hora=18:00&amp;duracion=60 — ¿hay cancha libre en esa sede?</summary>
    [HttpGet("hay-lugar")]
    public async Task<ActionResult<DisponibilidadDto>> HayLugar(
        Guid sede, string dia, string hora, int duracion, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        if (!Enum.TryParse<DayOfWeek>(dia, out var diaSemana) || !TimeOnly.TryParse(hora, out var horaInicio))
            return BadRequest();
        try
        {
            return Ok(await _portal.DisponibilidadHorarioAsync(userId, sede, diaSemana, horaInicio, duracion, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/portal/solicitudes-horario — propongo una clase individual fija.</summary>
    [HttpPost("solicitudes-horario")]
    public async Task<ActionResult<SolicitudHorarioDto>> SolicitarHorario(SolicitarHorarioDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.SolicitarHorarioAsync(userId, dto.SedeId, dto.Dia, dto.HoraInicio, dto.DuracionMinutos, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/portal/solicitudes-horario — mis solicitudes de clase individual.</summary>
    [HttpGet("solicitudes-horario")]
    public async Task<ActionResult<IReadOnlyList<SolicitudHorarioDto>>> MisSolicitudesHorario(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.MisSolicitudesHorarioAsync(userId, ct));
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

    /// <summary>PUT api/portal/perfil/foto — cambio o quito mi foto de perfil.</summary>
    [HttpPut("perfil/foto")]
    public async Task<ActionResult<MiPerfilDto>> ActualizarFoto(ActualizarFotoDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.ActualizarFotoAsync(userId, dto.FotoUrl, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/portal/raquetas — mis raquetas.</summary>
    [HttpGet("raquetas")]
    public async Task<ActionResult<IReadOnlyList<RaquetaDto>>> MisRaquetas(CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.MisRaquetasAsync(userId, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>POST api/portal/raquetas — agrego una raqueta.</summary>
    [HttpPost("raquetas")]
    public async Task<ActionResult<RaquetaDto>> AgregarRaqueta(GuardarRaquetaDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.AgregarRaquetaAsync(userId, dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>PUT api/portal/raquetas/{id} — edito una raqueta mía.</summary>
    [HttpPut("raquetas/{raquetaId:guid}")]
    public async Task<ActionResult<RaquetaDto>> EditarRaqueta(Guid raquetaId, GuardarRaquetaDto dto, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            return Ok(await _portal.EditarRaquetaAsync(userId, raquetaId, dto, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>DELETE api/portal/raquetas/{id} — borro una raqueta mía.</summary>
    [HttpDelete("raquetas/{raquetaId:guid}")]
    public async Task<IActionResult> BorrarRaqueta(Guid raquetaId, CancellationToken ct)
    {
        if (UserId() is not { } userId) return Unauthorized();
        try
        {
            await _portal.BorrarRaquetaAsync(userId, raquetaId, ct);
            return NoContent();
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
