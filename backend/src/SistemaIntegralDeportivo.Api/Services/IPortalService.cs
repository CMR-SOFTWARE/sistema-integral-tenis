using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// El portal del ALUMNO: todo se resuelve desde la ficha vinculada al
/// usuario del token (Alumno.UserId) — nunca por parámetros del cliente.
/// </summary>
public interface IPortalService
{
    /// <summary>
    /// Mis clases: próximas (hoy → fin del mes que viene, materializando lo
    /// que falte) e historial (mes pasado → ayer, lo más reciente primero).
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si el usuario no tiene ficha vinculada.</exception>
    Task<MisTurnosDto> MisTurnosAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Mi liquidación del mes (null = sin movimientos).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si el usuario no tiene ficha vinculada.</exception>
    Task<AlumnoLiquidacionDto?> MiCuotaAsync(Guid userId, int anio, int mes, CancellationToken ct = default);

    /// <summary>Mi ficha, como me ve el club.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si el usuario no tiene ficha vinculada.</exception>
    Task<MiPerfilDto> MiPerfilAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Edita MIS datos de contacto (teléfono/email); el resto es del profe.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Sin ficha vinculada o teléfono vacío.</exception>
    Task<MiPerfilDto> ActualizarMiPerfilAsync(
        Guid userId, ActualizarMiPerfilDto dto, CancellationToken ct = default);

    /// <summary>
    /// Aviso de cancelación de MI turno (hasta la hora de inicio): el turno
    /// sigue para el resto y mi cargo queda (falta con aviso; la recuperación
    /// la decide el profe). No mueve plata.
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">
    /// Sin ficha, turno inexistente/ajeno/pasado/cancelado, ya avisado o sin motivo.
    /// </exception>
    Task CancelarMiTurnoAsync(Guid userId, Guid turnoId, string motivo, CancellationToken ct = default);
}
