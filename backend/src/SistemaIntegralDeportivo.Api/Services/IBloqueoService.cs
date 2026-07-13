using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Negocio de bloqueos de agenda: validación fijo/rango, preview de impacto
/// y alta con cancelación en cascada de los turnos pisados (nadie paga:
/// se eliminan los cargos impagos; los pagados son intocables).
/// </summary>
public interface IBloqueoService
{
    Task<IReadOnlyList<BloqueoResponseDto>> ListarAsync(CancellationToken ct = default);

    /// <summary>Calcula qué cancelaría el bloqueo SIN persistir nada (modal Impacto).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si el bloqueo es inválido.</exception>
    Task<ImpactoBloqueoDto> PrevisualizarImpactoAsync(CreateBloqueoDto dto, CancellationToken ct = default);

    /// <summary>Crea el bloqueo y cancela en cascada los turnos programados que pisa.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si el bloqueo es inválido.</exception>
    Task<BloqueoCreadoDto> CrearAsync(CreateBloqueoDto dto, CancellationToken ct = default);

    /// <summary>Borra el bloqueo (configuración, no historia). Los turnos ya cancelados no se restauran.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si no existe.</exception>
    Task EliminarAsync(Guid id, CancellationToken ct = default);
}
