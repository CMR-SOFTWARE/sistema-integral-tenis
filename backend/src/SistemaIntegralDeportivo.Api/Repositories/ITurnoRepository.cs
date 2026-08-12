using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Contrato de datos de Turnos (instancias concretas de la agenda).</summary>
public interface ITurnoRepository
{
    /// <summary>
    /// Turnos del tenant en un rango, ya proyectados a lo que se muestra. NO devuelve
    /// entidades: traía la ficha entera de cada alumno (con la foto en base64) por cada
    /// fila del roster Y de los participantes, multiplicadas entre sí por el JOIN.
    /// </summary>
    Task<IReadOnlyList<TurnoAgenda>> ListarEntreAsync(
        DateOnly desde, DateOnly hasta, CancellationToken ct = default);

    /// <summary>
    /// Fechas que YA tienen turno generado, por horario (idempotencia de la generación
    /// perezosa). Recibe TODOS los horarios de una: antes se preguntaba de a uno y eso
    /// eran 40+ idas y vueltas a la base por cada carga de la agenda — con ~115 ms de
    /// red cada una, los 5,5 segundos que tardaba la semana en producción.
    /// </summary>
    Task<ILookup<Guid, DateOnly>> FechasGeneradasAsync(
        IReadOnlyCollection<Guid> horarioIds, DateOnly desde, DateOnly hasta, CancellationToken ct = default);

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

    /// <summary>Últimos AVISOS de alumnos (participaciones con CanceloEl), más recientes primero.</summary>
    Task<IReadOnlyList<TurnoParticipante>> ListarAvisosRecientesAsync(
        int cantidad, CancellationToken ct = default);

    /// <summary>
    /// Turnos PROGRAMADOS desde una fecha, opcionalmente de una cancha, con
    /// participantes y contexto (cascada e impacto de bloqueos).
    /// </summary>
    Task<IReadOnlyList<Turno>> ListarProgramadosDesdeAsync(
        DateOnly desde, Guid? canchaId, CancellationToken ct = default);

    /// <summary>
    /// Turnos PROGRAMADOS futuros donde participa un alumno, TRACKEADOS y con
    /// su roster (para sacarlo del calendario al pausarlo/darlo de baja).
    /// </summary>
    Task<IReadOnlyList<Turno>> ListarFuturosDeAlumnoAsync(
        Guid alumnoId, DateOnly desde, CancellationToken ct = default);

    Task AgregarAsync(Turno turno, CancellationToken ct = default);

    /// <summary>Marca el turno para borrar (se persiste con GuardarCambiosAsync).</summary>
    void Eliminar(Turno turno);

    /// <summary>Saca a UN alumno del roster de un turno (el turno sigue para el resto).</summary>
    void QuitarParticipante(TurnoParticipante participante);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}

/// <summary>
/// Un turno de la agenda con lo que de verdad se muestra. Es la lectura de cinco
/// pantallas (semana, mes, inicio, sueldos y clases sueltas), así que junta lo que
/// necesitan todas — que sigue siendo mucho menos que la entidad entera.
/// </summary>
/// <param name="MiembrosActivos">
/// Cuántos vienen a la CLASE hoy (no al turno). Junto con <paramref name="UnicoMiembro"/>
/// alcanza para el título, y evita traer el roster: esa era la segunda colección, la que
/// multiplicaba las filas contra los participantes.
/// </param>
/// <param name="UnicoMiembro">Nombre y apellido del primero del roster; solo se usa cuando son exactamente uno.</param>
/// <param name="HorarioDia">Día y hora DE LA PLANTILLA (el detalle de sueldos los muestra, y pueden diferir del turno ya generado).</param>
public record TurnoAgenda(
    Guid Id,
    DateOnly Fecha,
    TimeOnly HoraInicio,
    int DuracionMinutos,
    EstadoTurno Estado,
    string? CanceladoMotivo,
    Guid CanchaId,
    string Cancha,
    string Sede,
    Guid? HorarioId,
    string? HorarioNombre,
    Guid? ProfesorUserId,
    decimal? ValorHoraProfe,
    DayOfWeek? HorarioDia,
    TimeOnly? HorarioHoraInicio,
    int MiembrosActivos,
    string? UnicoMiembro,
    IReadOnlyList<ParticipanteAgenda> Participantes);

/// <summary>Un alumno anotado en ese turno, con lo justo para la tarjeta y la asistencia.</summary>
public record ParticipanteAgenda(Guid AlumnoId, string Nombre, string Apellido, bool Presente);
