using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>
/// Contrato de datos de Alumnos. Toda implementación scopea por tenant
/// (hoy: el tenant demo) — es parte del contrato, no un detalle.
/// </summary>
public interface IAlumnoRepository
{
    /// <summary>¿Ya existe un alumno con este DNI en el tenant?</summary>
    Task<bool> ExisteDniAsync(string dni, CancellationToken ct = default);

    /// <summary>Persiste el alumno (y su tutor, si viene en el grafo).</summary>
    Task<Alumno> AgregarAsync(Alumno alumno, CancellationToken ct = default);

    /// <summary>Alumnos del tenant, filtrables por categoría y estado.</summary>
    Task<IReadOnlyList<Alumno>> ListarAsync(
        CategoriaAlumno? categoria, EstadoAlumno? estado, CancellationToken ct = default);

    /// <summary>Un alumno del tenant por id (trackeado, apto para modificar).</summary>
    Task<Alumno?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <summary>Confirma en la base los cambios hechos a entidades trackeadas.</summary>
    Task GuardarCambiosAsync(CancellationToken ct = default);

    /// <summary>La ficha del tenant con ese DNI (para vincular al aprobar solicitudes).</summary>
    Task<Alumno?> ObtenerPorDniAsync(string dni, CancellationToken ct = default);

    // ── Auth / portal (ADR-0007: membresía del jugador) ──

    /// <summary>La ficha vinculada a un usuario global, con su Tenant (null si no tiene club).</summary>
    Task<Alumno?> ObtenerPorUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>TODAS las fichas de un usuario global (la familia: el titular ve a varios miembros).</summary>
    Task<IReadOnlyList<Alumno>> ListarPorUserIdAsync(Guid userId, CancellationToken ct = default);

    // ── Agregados para el dashboard (queries de solo lectura) ──

    /// <summary>Cantidad de alumnos del tenant en un estado dado.</summary>
    Task<int> ContarPorEstadoAsync(EstadoAlumno estado, CancellationToken ct = default);

    /// <summary>Alumnos creados desde una fecha (altas del período).</summary>
    Task<int> ContarNuevosDesdeAsync(DateTime desde, CancellationToken ct = default);

    /// <summary>Suma de aranceles de los alumnos activos (ingreso estimado).</summary>
    Task<decimal> SumarArancelActivosAsync(CancellationToken ct = default);

    /// <summary>Conteo por categoría, excluyendo dados de baja (Inactivo).</summary>
    Task<Dictionary<CategoriaAlumno, int>> ContarPorCategoriaAsync(CancellationToken ct = default);
}
