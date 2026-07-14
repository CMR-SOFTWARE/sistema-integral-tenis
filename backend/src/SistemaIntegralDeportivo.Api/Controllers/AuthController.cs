using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>
/// Identidad global (ADR-0007): registro gratis de jugador, login con JWT,
/// sesión con membresías y reclamo de ficha. El hash y las validaciones de
/// contraseña los maneja Identity (UserManager); las reglas, IAuthService.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<Usuario> _userManager;
    private readonly IAuthService _auth;
    private readonly ITenantRepository _tenants;

    public AuthController(UserManager<Usuario> userManager, IAuthService auth, ITenantRepository tenants)
    {
        _userManager = userManager;
        _auth = auth;
        _tenants = tenants;
    }

    /// <summary>POST api/auth/registro — alta GRATIS de jugador (datos completos).</summary>
    [HttpPost("registro")]
    public async Task<ActionResult<SesionDto>> Registro(RegistroJugadorDto dto, CancellationToken ct)
    {
        var usuario = new Usuario
        {
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Dni = dto.Dni.Trim(),
            PhoneNumber = dto.Telefono.Trim(),
            FechaNacimiento = dto.FechaNacimiento,
            Categoria = dto.Categoria,
        };

        var resultado = await _userManager.CreateAsync(usuario, dto.Password);
        if (!resultado.Succeeded)
            return Problem(
                // Distinct: UserName == Email, y los duplicados dicen lo mismo
                detail: string.Join(" ", resultado.Errors.Select(e => e.Description).Distinct()),
                statusCode: StatusCodes.Status400BadRequest);

        return Ok(await _auth.ArmarSesionAsync(usuario, incluirToken: true, ct));
    }

    /// <summary>
    /// POST api/auth/registro-profesor — identidad + su club en PENDIENTE_PAGO.
    /// Después del checkout (POST activar-tenant) recién habilita la gestión.
    /// </summary>
    [HttpPost("registro-profesor")]
    public async Task<ActionResult<SesionDto>> RegistroProfesor(
        RegistroProfesorDto dto, CancellationToken ct)
    {
        var usuario = new Usuario
        {
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Dni = string.IsNullOrWhiteSpace(dto.Dni) ? null : dto.Dni.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
        };

        var resultado = await _userManager.CreateAsync(usuario, dto.Password);
        if (!resultado.Succeeded)
            return Problem(
                detail: string.Join(" ", resultado.Errors.Select(e => e.Description).Distinct()),
                statusCode: StatusCodes.Status400BadRequest);

        try
        {
            await _auth.CrearTenantParaAsync(usuario, dto.NombreClub, ct);
        }
        catch (ReglaDeNegocioException ex)
        {
            // Compensación: sin club no hay registro de profe a medias
            await _userManager.DeleteAsync(usuario);
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(await _auth.ArmarSesionAsync(usuario, incluirToken: true, ct));
    }

    /// <summary>
    /// POST api/auth/activar-tenant — el "pago" del checkout SIMULADO.
    /// Al desplegar: el webhook de Mercado Pago llama al mismo ActivarTenantAsync
    /// y este endpoint se limita a Development.
    /// </summary>
    [Authorize]
    [HttpPost("activar-tenant")]
    public async Task<ActionResult<SesionDto>> ActivarTenant(CancellationToken ct)
    {
        var usuario = await UsuarioActualAsync();
        if (usuario is null) return Unauthorized();

        try
        {
            return Ok(await _auth.ActivarTenantAsync(usuario, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>GET api/auth/profesores?buscar= — búsqueda pública de clubes activos.</summary>
    [HttpGet("profesores")]
    public async Task<ActionResult<IReadOnlyList<ProfesorPublicoDto>>> Profesores(
        CancellationToken ct, [FromQuery] string? buscar = null)
    {
        var activos = await _tenants.ListarActivosAsync(buscar, ct);
        // Solo datos públicos: club y nombre del profe (nada de precios/contactos)
        return Ok(activos.Select(x => new ProfesorPublicoDto
        {
            TenantId = x.Tenant.Id,
            Club = x.Tenant.Nombre,
            Profesor = x.Profesor,
        }).ToList());
    }

    /// <summary>POST api/auth/login — email + contraseña → JWT + membresías.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<SesionDto>> Login(LoginDto dto, CancellationToken ct)
    {
        var usuario = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (usuario is null || !await _userManager.CheckPasswordAsync(usuario, dto.Password))
            return Problem(
                detail: "Email o contraseña incorrectos.",
                statusCode: StatusCodes.Status401Unauthorized);

        return Ok(await _auth.ArmarSesionAsync(usuario, incluirToken: true, ct));
    }

    /// <summary>GET api/auth/yo — la sesión del token, con las membresías al día.</summary>
    [Authorize]
    [HttpGet("yo")]
    public async Task<ActionResult<SesionDto>> Yo(CancellationToken ct)
    {
        var usuario = await UsuarioActualAsync();
        if (usuario is null) return Unauthorized();

        return Ok(await _auth.ArmarSesionAsync(usuario, incluirToken: false, ct));
    }

    /// <summary>POST api/auth/reclamar — vincula una ficha coincidente a mi cuenta.</summary>
    [Authorize]
    [HttpPost("reclamar")]
    public async Task<ActionResult<SesionDto>> Reclamar(ReclamarFichaDto dto, CancellationToken ct)
    {
        var usuario = await UsuarioActualAsync();
        if (usuario is null) return Unauthorized();

        try
        {
            await _auth.ReclamarFichaAsync(usuario, dto.AlumnoId, ct);
            return Ok(await _auth.ArmarSesionAsync(usuario, incluirToken: false, ct));
        }
        catch (ReglaDeNegocioException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// POST api/auth/mis-datos — corrige el DNI/teléfono de MI cuenta y
    /// devuelve la sesión con las coincidencias recalculadas (reclamo).
    /// </summary>
    [Authorize]
    [HttpPost("mis-datos")]
    public async Task<ActionResult<SesionDto>> ActualizarMisDatos(
        ActualizarMisDatosDto dto, CancellationToken ct)
    {
        var usuario = await UsuarioActualAsync();
        if (usuario is null) return Unauthorized();

        usuario.Dni = string.IsNullOrWhiteSpace(dto.Dni) ? null : dto.Dni.Trim();
        usuario.PhoneNumber = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();

        var resultado = await _userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
            return Problem(
                detail: string.Join(" ", resultado.Errors.Select(e => e.Description).Distinct()),
                statusCode: StatusCodes.Status400BadRequest);

        return Ok(await _auth.ArmarSesionAsync(usuario, incluirToken: false, ct));
    }

    /// <summary>El usuario del claim "sub" del JWT (MapInboundClaims está apagado).</summary>
    private async Task<Usuario?> UsuarioActualAsync()
    {
        var sub = User.FindFirst("sub")?.Value;
        return sub is null ? null : await _userManager.FindByIdAsync(sub);
    }
}
