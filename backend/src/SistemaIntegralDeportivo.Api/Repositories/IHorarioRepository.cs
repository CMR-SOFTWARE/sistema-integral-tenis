using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Contrato de datos de Horarios (plantillas recurrentes).</summary>
public interface IHorarioRepository
{
    /// <summary>Horarios ACTIVOS de una cancha en un día (para chequear solapamiento).</summary>
    Task<IReadOnlyList<Horario>> ListarPorCanchaYDiaAsync(
        Guid canchaId, DayOfWeek dia, CancellationToken ct = default);

    /// <summary>Todos los horarios activos del tenant, con cancha/sede/grupo/alumno.</summary>
    Task<IReadOnlyList<Horario>> ListarActivosAsync(CancellationToken ct = default);

    /// <summary>
    /// Horarios INDIVIDUALES activos de un alumno, TRACKEADOS: la baja del
    /// alumno los desactiva para liberar el slot de la cancha.
    /// </summary>
    Task<IReadOnlyList<Horario>> ListarIndividualesDeAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default);

    Task<Horario?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<Horario> AgregarAsync(Horario horario, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
