using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Negocio de identidad (ADR-0007): sesión con membresías y reclamo de ficha.
/// El alta/login de usuarios (hash, validación) lo maneja Identity en el
/// controller; acá viven las REGLAS propias del dominio.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// La sesión que ve el front: es profesor si es dueño de un tenant
    /// ACTIVO, su ficha de alumno si está vinculado a un club, y si debe
    /// cambiar la contraseña temporal antes de seguir.
    /// </summary>
    Task<SesionDto> ArmarSesionAsync(Usuario usuario, bool incluirToken, CancellationToken ct = default);

    /// <summary>
    /// Registro de profesor: crea SU tenant en PENDIENTE_PAGO (subdominio =
    /// slug del nombre, con sufijo si colisiona). Un tenant por profe.
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Nombre vacío o ya es dueño de un club.</exception>
    Task<Tenant> CrearTenantParaAsync(Usuario usuario, string nombreClub, CancellationToken ct = default);

    /// <summary>
    /// "Pagó la suscripción": PENDIENTE_PAGO → ACTIVO. IDEMPOTENTE (el webhook
    /// de MP reintenta). Hoy lo dispara el checkout simulado; al desplegar,
    /// el webhook real llama a este MISMO método — la costura es esta.
    /// Devuelve la sesión con token nuevo (claims profesor + tenant).
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si no tiene ningún club.</exception>
    Task<SesionDto> ActivarTenantAsync(Usuario usuario, CancellationToken ct = default);
}
