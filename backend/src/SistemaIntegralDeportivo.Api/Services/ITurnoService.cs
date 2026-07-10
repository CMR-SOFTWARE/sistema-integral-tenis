using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Negocio de turnos: generación perezosa, asistencia y cancelación.</summary>
public interface ITurnoService
{
    /// <summary>
    /// Devuelve los turnos de la semana que arranca en <paramref name="lunes"/>,
    /// GENERANDO los que falten desde los horarios activos (idempotente:
    /// los existentes no se tocan). El roster se congela al generar.
    /// </summary>
    Task<IReadOnlyList<TurnoResponseDto>> ObtenerSemanaAsync(DateOnly lunes, CancellationToken ct = default);

    /// <summary>Marca presente/ausente a un participante (no afecta cargos).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si no participa del turno.</exception>
    Task MarcarAsistenciaAsync(Guid turnoId, Guid alumnoId, bool presente, CancellationToken ct = default);

    /// <summary>Cancela el turno con motivo (nunca se borra).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si ya está cancelado o no existe.</exception>
    Task CancelarAsync(Guid turnoId, string motivo, CancellationToken ct = default);
}
