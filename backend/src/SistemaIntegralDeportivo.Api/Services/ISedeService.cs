using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>CRUD mínimo de sedes/canchas (config de la agenda). Queries finas, sin TDD (ADR-0005).</summary>
public interface ISedeService
{
    Task<IReadOnlyList<SedeResponseDto>> ListarAsync(CancellationToken ct = default);
    Task<SedeResponseDto> CrearAsync(CreateSedeDto dto, CancellationToken ct = default);
    /// <exception cref="Common.ReglaDeNegocioException">Si la sede no existe.</exception>
    Task<CanchaResponseDto> AgregarCanchaAsync(Guid sedeId, CreateCanchaDto dto, CancellationToken ct = default);
}
