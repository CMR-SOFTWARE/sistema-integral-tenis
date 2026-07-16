using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// CRUD de sedes/canchas (config de la agenda). El alta es query fina (sin
/// TDD, ADR-0005); la baja SÍ tiene regla propia (ver DesactivarAsync).
/// </summary>
public interface ISedeService
{
    Task<IReadOnlyList<SedeResponseDto>> ListarAsync(CancellationToken ct = default);
    Task<SedeResponseDto> CrearAsync(CreateSedeDto dto, CancellationToken ct = default);

    /// <exception cref="Common.ReglaDeNegocioException">Si la sede no existe.</exception>
    Task<CanchaResponseDto> AgregarCanchaAsync(Guid sedeId, CreateCanchaDto dto, CancellationToken ct = default);

    /// <summary>
    /// Baja LÓGICA: la sede deja de ofrecerse para horarios nuevos, pero su
    /// historia queda. No procede si todavía tiene horarios ACTIVOS.
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">No existe, o tiene horarios activos.</exception>
    Task DesactivarAsync(Guid id, CancellationToken ct = default);

    /// <summary>Vuelve a habilitar una sede dada de baja.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si la sede no existe.</exception>
    Task ReactivarAsync(Guid id, CancellationToken ct = default);
}
