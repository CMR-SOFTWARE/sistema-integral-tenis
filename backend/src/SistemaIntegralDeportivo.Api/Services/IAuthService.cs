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
    /// La sesión que ve el front: es profesor si es dueño de un tenant, su
    /// ficha de alumno si reclamó una, y las fichas que puede reclamar
    /// (match por DNI/teléfono) si todavía no tiene ninguna.
    /// </summary>
    Task<SesionDto> ArmarSesionAsync(Usuario usuario, bool incluirToken, CancellationToken ct = default);

    /// <summary>
    /// Vincula una ficha al usuario. Solo procede si la ficha está libre
    /// (sin UserId) Y coincide con el DNI o teléfono del usuario.
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si no es reclamable por este usuario.</exception>
    Task ReclamarFichaAsync(Usuario usuario, Guid alumnoId, CancellationToken ct = default);
}
