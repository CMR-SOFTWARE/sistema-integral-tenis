using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Vista unificada de cancelaciones para el profe: turnos enteros cancelados
/// (por él o por bloqueos) + avisos individuales de alumnos, en una sola
/// lista cronológica.
/// </summary>
public interface ICancelacionService
{
    Task<IReadOnlyList<CancelacionDto>> ListarRecientesAsync(
        int cantidad, CancellationToken ct = default);
}
