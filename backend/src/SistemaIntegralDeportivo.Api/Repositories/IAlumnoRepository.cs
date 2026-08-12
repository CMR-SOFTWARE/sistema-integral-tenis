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
    /// ¿Ya existe una ficha con el MISMO nombre y celular en el tenant? Detecta el
    /// duplicado de la misma persona cuando no hay DNI (la cuenta familiar comparte
    /// celular pero con distinto nombre, así que no la frena).
    /// </summary>
    Task<bool> ExisteFichaConNombreYTelefonoAsync(
        string nombre, string apellido, string telefono, CancellationToken ct = default);

    /// <summary>
    /// ¿Ese usuario tiene una ficha de alumno EN ESTE tenant? Se usa para reusar la
    /// persona como Staff sin crear otra cuenta (un usuario, varias facetas): solo
    /// si ya es alumno de la academia (relación conocida), no una cuenta ajena.
    /// </summary>
    Task<bool> EsAlumnoDelTenantAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Persiste el alumno (y su tutor, si viene en el grafo).</summary>
    Task<Alumno> AgregarAsync(Alumno alumno, CancellationToken ct = default);

    /// <summary>
    /// Alumnos del tenant, filtrables por categoría y estado. Sin filtro devuelve
    /// TODAS las fichas: quién es "alumno" y quién está esperando lo decide el
    /// filtro por clase (<see cref="ListarConClaseAsync"/>), no una columna.
    /// </summary>
    Task<IReadOnlyList<Alumno>> ListarAsync(
        CategoriaAlumno? categoria, EstadoAlumno? estado, CancellationToken ct = default);

    /// <summary>Un alumno del tenant por id (trackeado, apto para modificar).</summary>
    Task<Alumno?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Vincula un tutor a un alumno YA existente (completar la ficha después de
    /// marcarlo menor). Si ya hay un tutor con ese DNI en el tenant, lo REUTILIZA
    /// (índice único TenantId+Dni); si no, crea el nuevo. Persiste con GuardarCambiosAsync.
    /// </summary>
    Task VincularTutorAsync(Alumno alumno, Tutor tutor, CancellationToken ct = default);

    /// <summary>
    /// Ids de los alumnos del tenant con al menos una clase asignada (membresía sin
    /// baja en un horario activo). Es la línea que separa a un alumno de alguien que
    /// está esperando, y también la que decide a quién se le cobra cuota (el que no
    /// toma clases no paga, aunque tenga arancel cargado).
    /// </summary>
    Task<HashSet<Guid>> ListarConClaseAsync(CancellationToken ct = default);

    /// <summary>
    /// De los ids dados, cuáles tienen clase vigente. Mismo criterio que
    /// <see cref="ListarConClaseAsync"/> pero acotado, y <b>sin scopear por tenant</b>:
    /// lo usa la sesión del portal, que arma la ficha ANTES de que haya club
    /// establecido (igual que <see cref="ListarPorUserIdAsync"/>).
    /// </summary>
    Task<HashSet<Guid>> FiltrarConClaseAsync(
        IReadOnlyCollection<Guid> alumnoIds, CancellationToken ct = default);

    /// <summary>
    /// Una fila liviana por alumno del tenant (todos, sin filtrar): con esto el
    /// dashboard saca sus cuatro números —activos con clase, altas del mes, pausados
    /// y el desglose por categoría— en UNA sola ida a la base en vez de cuatro.
    /// </summary>
    Task<IReadOnlyList<AlumnoResumenFila>> ResumenAsync(CancellationToken ct = default);

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

    /// <summary>
    /// Recarga la navegación <see cref="Alumno.Sede"/> desde su FK actual (tras
    /// cambiar <see cref="Alumno.SedeId"/>), para que el mapeo devuelva el club nuevo.
    /// </summary>
    Task RecargarSedeAsync(Alumno alumno, CancellationToken ct = default);

    /// <summary>La ficha del tenant con ese DNI (para vincular al aprobar solicitudes).</summary>
    Task<Alumno?> ObtenerPorDniAsync(string dni, CancellationToken ct = default);

    // ── Auth / portal (ADR-0007: membresía del jugador) ──

    /// <summary>La ficha vinculada a un usuario global, con su Tenant (null si no tiene club).</summary>
    Task<Alumno?> ObtenerPorUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>TODAS las fichas de un usuario global (la familia: el titular ve a varios miembros).</summary>
    Task<IReadOnlyList<Alumno>> ListarPorUserIdAsync(Guid userId, CancellationToken ct = default);

    // ── Agregados para el dashboard (queries de solo lectura) ──

    /// <summary>Suma de aranceles de los alumnos activos (ingreso estimado).</summary>
    Task<decimal> SumarArancelActivosAsync(CancellationToken ct = default);

    /// <summary>
    /// Conteo por categoría de los alumnos CON CLASE, para que el desglose sume lo
    /// mismo que el total de arriba (si contara también a los que esperan, el
    /// dashboard mostraría dos números que no cierran).
    /// </summary>
    Task<Dictionary<CategoriaAlumno, int>> ContarPorCategoriaAsync(CancellationToken ct = default);
}

/// <summary>
/// Lo mínimo de un alumno para contarlo, sin traer la ficha entera (que arrastra la
/// foto en base64).
/// </summary>
/// <param name="TieneClase">
/// Tiene lugar vigente en alguna clase activa: es lo que separa a un alumno del que
/// todavía está esperando.
/// </param>
public record AlumnoResumenFila(
    EstadoAlumno Estado, CategoriaAlumno Categoria, DateTime CreadoEl, bool TieneClase);
