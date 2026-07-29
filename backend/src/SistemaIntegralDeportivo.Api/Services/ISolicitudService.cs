using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Lista de espera: el jugador se UNE a un club activo y su ficha nace directo en
/// espera (sin aprobación). El profe la ve, la puede quitar, y se vuelve alumno
/// cuando le asigna una clase.
/// </summary>
public interface ISolicitudService
{
    /// <summary>
    /// Unirse a un club: crea la ficha directo en la lista de espera (EnEspera).
    /// Reglas: club activo, sin club actual (un club por persona, por ahora) y
    /// datos del perfil completos (DNI/teléfono/fecha de nacimiento).
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si viola una regla.</exception>
    Task<IReadOnlyList<MiSolicitudDto>> CrearAsync(
        Usuario usuario, CrearSolicitudDto dto, CancellationToken ct = default);

    /// <summary>Compat con el portal: ya no hay solicitudes con estado (devuelve vacío).</summary>
    Task<IReadOnlyList<MiSolicitudDto>> MisAsync(Guid userId, CancellationToken ct = default);

    /// <summary>La lista de espera de MI club: fichas EnEspera (miembros sin clase todavía).</summary>
    Task<IReadOnlyList<SolicitudPendienteDto>> PendientesAsync(CancellationToken ct = default);

    /// <summary>Conteo para el badge del sidebar (cuántos hay en espera).</summary>
    Task<int> ContarPendientesAsync(CancellationToken ct = default);

    /// <summary>Quitar de la lista de espera: borra la ficha EnEspera (conserva su login).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">No está en la lista de espera.</exception>
    Task QuitarDeEsperaAsync(Guid alumnoId, CancellationToken ct = default);
}
