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

    /// <summary>
    /// ¿Ese usuario tiene una ficha de alumno EN ESTE tenant? Se usa para reusar la
    /// persona como Staff sin crear otra cuenta (un usuario, varias facetas): solo
    /// si ya es alumno de la academia (relación conocida), no una cuenta ajena.
    /// </summary>
    Task<bool> EsAlumnoDelTenantAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Persiste el alumno (y su tutor, si viene en el grafo).</summary>
    Task<Alumno> AgregarAsync(Alumno alumno, CancellationToken ct = default);

    /// <summary>
    /// Alumnos del tenant, filtrables por categoría y estado. Sin filtro de estado
    /// devuelve la lista "de alumnos de verdad": EXCLUYE a los de la lista de espera
    /// (<see cref="EstadoAlumno.EnEspera"/>). Para verlos, pedir estado=EnEspera.
    /// </summary>
    Task<IReadOnlyList<Alumno>> ListarAsync(
        CategoriaAlumno? categoria, EstadoAlumno? estado, CancellationToken ct = default);

    /// <summary>
    /// Promueve al alumno de la lista de espera: si está <see cref="EstadoAlumno.EnEspera"/>,
    /// pasa a <see cref="EstadoAlumno.Activo"/> (le asignaron su primera clase). Idempotente
    /// y scopeado; no toca a los que ya son Activo/Suspendido/Inactivo.
    /// </summary>
    Task PromoverDeEsperaAsync(Guid alumnoId, CancellationToken ct = default);

    /// <summary>Un alumno del tenant por id (trackeado, apto para modificar).</summary>
    Task<Alumno?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Vincula un tutor a un alumno YA existente (completar la ficha después de
    /// marcarlo menor). Si ya hay un tutor con ese DNI en el tenant, lo REUTILIZA
    /// (índice único TenantId+Dni); si no, crea el nuevo. Persiste con GuardarCambiosAsync.
    /// </summary>
    Task VincularTutorAsync(Alumno alumno, Tutor tutor, CancellationToken ct = default);

    /// <summary>
    /// Ids de los alumnos del tenant con al menos una clase asignada: miembro de un
    /// grupo activo (membresía sin baja) u horario individual activo. Se usa para
    /// cobrar la cuota SOLO a los que efectivamente toman clases (el que no tiene
    /// clase no tiene cuota, aunque tenga arancel cargado).
    /// </summary>
    Task<HashSet<Guid>> ListarConClaseAsync(CancellationToken ct = default);

    /// <summary>
    /// Borrado REAL de la ficha (el profe pidió poder borrar a los que no vienen
    /// más). Borra el alumno y, en cascada, su historial dependiente (cargos,
    /// participaciones de turno, membresías, raquetas, notas, solicitudes, clases
    /// sueltas); además elimina sus horarios individuales y sus turnos (FK SetNull).
    /// NO es la baja lógica (esa es <see cref="Services.IAlumnoService.DarDeBajaAsync"/>).
    /// </summary>
    Task EliminarDefinitivoAsync(Alumno alumno, CancellationToken ct = default);

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
