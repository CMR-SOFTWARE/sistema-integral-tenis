using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Contrato de datos de Turnos (instancias concretas de la agenda).</summary>
public interface ITurnoRepository
{
    /// <summary>Turnos del tenant en un rango de fechas, con participantes y contexto.</summary>
    Task<IReadOnlyList<Turno>> ListarEntreAsync(
        DateOnly desde, DateOnly hasta, CancellationToken ct = default);

    /// <summary>Fechas que YA tienen turno generado para un horario (idempotencia).</summary>
    Task<IReadOnlyList<DateOnly>> FechasGeneradasAsync(
        Guid horarioId, DateOnly desde, DateOnly hasta, CancellationToken ct = default);

    Task<Turno?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task AgregarAsync(Turno turno, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
