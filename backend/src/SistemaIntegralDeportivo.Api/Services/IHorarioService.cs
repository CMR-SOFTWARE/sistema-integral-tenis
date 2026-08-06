using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Negocio de horarios recurrentes (la grilla semanal del profe).</summary>
public interface IHorarioService
{
    /// <summary>
    /// Alta de una clase fija con su cupo y sus alumnos. Reglas: sin solapamiento con
    /// otras clases de la MISMA cancha (mismo día y rango), y cada alumno tiene que
    /// poder sumarse (activo o en espera, y sin la cuota vencida).
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si viola una regla.</exception>
    Task<HorarioResponseDto> CrearAsync(CreateHorarioDto dto, CancellationToken ct = default);

    /// <summary>
    /// Suma un alumno al roster: respeta el cupo, no duplica, reactiva al que vuelve,
    /// promueve al que estaba en espera y lo repone en los turnos futuros.
    /// </summary>
    Task AgregarAlumnoAsync(Guid horarioId, Guid alumnoId, CancellationToken ct = default);

    /// <summary>Lo saca del roster (baja lógica) y de sus turnos futuros.</summary>
    Task QuitarAlumnoAsync(Guid horarioId, Guid alumnoId, CancellationToken ct = default);

    Task<IReadOnlyList<HorarioResponseDto>> ListarAsync(CancellationToken ct = default);

    /// <summary>(Re)asigna el profe del horario (null = sin asignar). Valida que sea del club.</summary>
    Task<HorarioResponseDto> AsignarProfesorAsync(Guid id, Guid? profesorUserId, decimal? valorHoraProfe, CancellationToken ct = default);

    /// <summary>
    /// Edita un horario (cancha/día/hora/duración/profe/valor hora). Si cambia el
    /// horario en sí, los turnos futuros se reprograman (los pasados quedan). El
    /// grupo/alumno no se toca acá.
    /// </summary>
    Task<HorarioResponseDto> EditarAsync(Guid id, UpdateHorarioDto dto, CancellationToken ct = default);

    /// <summary>
    /// Desactiva la plantilla y limpia el futuro: los turnos con fecha ≥ hoy
    /// se borran junto con sus cargos impagos; los que tienen algún cargo
    /// pagado se conservan (plata cobrada). Lo pasado es historia y no se toca.
    /// </summary>
    Task DesactivarAsync(Guid id, CancellationToken ct = default);
}
