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
}
