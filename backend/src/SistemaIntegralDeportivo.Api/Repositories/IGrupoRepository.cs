using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Contrato de datos de Grupos. Scopeado por tenant, como todos.</summary>
public interface IGrupoRepository
{
    /// <summary>Grupo por id, con sus membresías y alumnos cargados.</summary>
    Task<Grupo?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <summary>Grupos del tenant (activos primero), con membresías y alumnos.</summary>
    Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken ct = default);

    Task<Grupo> AgregarAsync(Grupo grupo, CancellationToken ct = default);

    /// <summary>
    /// Borrado REAL del grupo (el profe lo pidió: lo que no usa más le ocupa lugar).
    /// Borra el grupo y, en cascada, sus membresías y solicitudes; además elimina
    /// sus horarios y los turnos de esos horarios (su FK es SetNull, no se van solos).
    /// Los cargos ya generados NO se tocan (plata/historia de cada alumno).
    /// </summary>
    Task EliminarAsync(Grupo grupo, CancellationToken ct = default);

    /// <summary>Membresía (activa o histórica) de un alumno en un grupo, si existe.</summary>
    Task<AlumnoGrupo?> ObtenerMembresiaAsync(Guid grupoId, Guid alumnoId, CancellationToken ct = default);

    /// <summary>Cantidad de miembros SIN fecha de baja del grupo.</summary>
    Task<int> ContarMiembrosActivosAsync(Guid grupoId, CancellationToken ct = default);

    /// <summary>
    /// Membresías VIGENTES de un alumno (todos sus grupos), TRACKEADAS: la
    /// baja del alumno les pone FechaBaja para liberar el cupo.
    /// </summary>
    Task<IReadOnlyList<AlumnoGrupo>> ListarMembresiasActivasDeAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default);

    Task AgregarMembresiaAsync(AlumnoGrupo membresia, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
