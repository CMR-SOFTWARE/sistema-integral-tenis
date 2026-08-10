using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Lista de espera: quién está esperando una clase. No es un estado guardado, se
/// deriva — están los activos SIN ninguna clase y los que pidieron sumarse a una y
/// el profe todavía no resolvió (esos pueden ser alumnos y estar acá a la vez).
/// </summary>
public interface ISolicitudService
{
    /// <summary>
    /// Unirse a un club: crea la ficha, que queda esperando porque todavía no tiene
    /// clase. Reglas: club activo, sin club actual (un club por persona, por ahora) y
    /// datos del perfil completos (DNI/teléfono/fecha de nacimiento).
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si viola una regla.</exception>
    Task<IReadOnlyList<MiSolicitudDto>> CrearAsync(
        Usuario usuario, CrearSolicitudDto dto, CancellationToken ct = default);

    /// <summary>Compat con el portal: ya no hay solicitudes con estado (devuelve vacío).</summary>
    Task<IReadOnlyList<MiSolicitudDto>> MisAsync(Guid userId, CancellationToken ct = default);

    /// <summary>La lista de espera de MI club: los sin clase + los pedidos sin resolver.</summary>
    Task<IReadOnlyList<SolicitudPendienteDto>> PendientesAsync(CancellationToken ct = default);

    /// <summary>Conteo para el badge de la pestaña (cuántos hay esperando).</summary>
    Task<int> ContarPendientesAsync(CancellationToken ct = default);

    /// <summary>
    /// Saca de la academia al que está esperando SIN clase: borrado real de la ficha
    /// (conserva su login). No sirve para el que espera por un pedido — a ese se le
    /// rechaza el pedido, la ficha no se toca.
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Ya tiene clase asignada.</exception>
    Task QuitarDeEsperaAsync(Guid alumnoId, CancellationToken ct = default);
}
