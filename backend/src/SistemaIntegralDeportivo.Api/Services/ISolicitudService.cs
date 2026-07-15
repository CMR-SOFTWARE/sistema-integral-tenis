using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Solicitudes alumno→profe (plan v2, reemplaza al reclamo): el jugador
/// pide entrar a un club activo; el profe aprueba (nace/se vincula la ficha
/// con los datos del registro) o rechaza.
/// </summary>
public interface ISolicitudService
{
    /// <summary>
    /// Nueva solicitud. Reglas: club activo, sin pendiente previa en ese
    /// club, sin club actual (un club por alumno, por ahora) y datos del
    /// perfil completos (DNI/teléfono/fecha de nacimiento).
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si viola una regla.</exception>
    Task<IReadOnlyList<MiSolicitudDto>> CrearAsync(
        Usuario usuario, CrearSolicitudDto dto, CancellationToken ct = default);

    /// <summary>Mis solicitudes con estado, la más nueva primero.</summary>
    Task<IReadOnlyList<MiSolicitudDto>> MisAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Pendientes de MI club, con los datos del solicitante.</summary>
    Task<IReadOnlyList<SolicitudPendienteDto>> PendientesAsync(CancellationToken ct = default);

    /// <summary>Conteo para el badge del sidebar.</summary>
    Task<int> ContarPendientesAsync(CancellationToken ct = default);

    /// <summary>Aprueba: crea/vincula la ficha en MI club y marca la solicitud.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">No existe/no es mía, o la ficha viola una regla (queda pendiente).</exception>
    Task<AlumnoResponseDto> AprobarAsync(Guid solicitudId, CancellationToken ct = default);

    /// <summary>Rechaza: solo marca (el alumno puede volver a solicitar).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">No existe o no es de mi club.</exception>
    Task RechazarAsync(Guid solicitudId, CancellationToken ct = default);
}
