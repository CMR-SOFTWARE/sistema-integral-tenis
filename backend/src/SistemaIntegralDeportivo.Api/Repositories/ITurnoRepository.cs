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

    /// <summary>Turnos de un horario desde una fecha (para limpiar al desactivar).</summary>
    Task<IReadOnlyList<Turno>> ListarPorHorarioDesdeAsync(
        Guid horarioId, DateOnly desde, CancellationToken ct = default);

    /// <summary>Turnos donde PARTICIPA un alumno, en un rango (portal alumno).</summary>
    Task<IReadOnlyList<Turno>> ListarPorAlumnoEntreAsync(
        Guid alumnoId, DateOnly desde, DateOnly hasta, CancellationToken ct = default);

    /// <summary>Últimos turnos cancelados, más recientes primero (dashboard).</summary>
    Task<IReadOnlyList<Turno>> ListarCanceladosRecientesAsync(
        int cantidad, CancellationToken ct = default);

    /// <summary>
    /// Turnos PROGRAMADOS desde una fecha, opcionalmente de una cancha, con
    /// participantes y contexto (cascada e impacto de bloqueos).
    /// </summary>
    Task<IReadOnlyList<Turno>> ListarProgramadosDesdeAsync(
        DateOnly desde, Guid? canchaId, CancellationToken ct = default);

    Task AgregarAsync(Turno turno, CancellationToken ct = default);

    /// <summary>Marca el turno para borrar (se persiste con GuardarCambiosAsync).</summary>
    void Eliminar(Turno turno);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
