using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Contrato de datos de Horarios (plantillas recurrentes) y de su roster.</summary>
public interface IHorarioRepository
{
    /// <summary>Horarios ACTIVOS de una cancha en un día (para chequear solapamiento).</summary>
    Task<IReadOnlyList<Horario>> ListarPorCanchaYDiaAsync(
        Guid canchaId, DayOfWeek dia, CancellationToken ct = default);

    /// <summary>Todos los horarios activos del tenant, con cancha/sede y su roster.</summary>
    Task<IReadOnlyList<Horario>> ListarActivosAsync(CancellationToken ct = default);

    Task<Horario?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <summary>El horario con su roster cargado (para contar el cupo y mapear los miembros).</summary>
    Task<Horario?> ObtenerConRosterAsync(Guid id, CancellationToken ct = default);

    Task<Horario> AgregarAsync(Horario horario, CancellationToken ct = default);

    // ── El roster: quiénes toman la clase ──

    /// <summary>La membresía del alumno en ese horario (aunque esté dada de baja); null si nunca estuvo.</summary>
    Task<AlumnoHorario?> ObtenerMembresiaAsync(Guid horarioId, Guid alumnoId, CancellationToken ct = default);

    Task AgregarMembresiaAsync(AlumnoHorario membresia, CancellationToken ct = default);

    /// <summary>Cuántos lugares están ocupados hoy en ese horario (para respetar el cupo).</summary>
    Task<int> ContarActivosAsync(Guid horarioId, CancellationToken ct = default);

    /// <summary>
    /// Las clases que toma HOY un alumno (membresías sin baja), TRACKEADAS: la baja
    /// del alumno las cierra para liberar su lugar.
    /// </summary>
    Task<IReadOnlyList<AlumnoHorario>> ListarMembresiasActivasDeAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
