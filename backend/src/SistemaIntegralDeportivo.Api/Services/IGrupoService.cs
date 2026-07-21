using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Negocio de grupos fijos: alta, listado, y membresías con historia.</summary>
public interface IGrupoService
{
    Task<GrupoResponseDto> CrearAsync(CreateGrupoDto dto, CancellationToken ct = default);

    Task<IReadOnlyList<GrupoResponseDto>> ListarAsync(CancellationToken ct = default);

    Task<GrupoResponseDto?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <summary>(Re)asigna el profe a cargo del grupo (null = sin asignar). Valida que sea del club.</summary>
    Task<GrupoResponseDto> AsignarProfesorAsync(Guid grupoId, Guid? profesorUserId, CancellationToken ct = default);

    /// <summary>
    /// Asigna un alumno al grupo. Reglas: grupo existente y con cupo,
    /// alumno Activo, sin membresía activa duplicada; si tuvo una baja
    /// previa en el grupo, se reactiva.
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si viola una regla.</exception>
    Task AsignarAlumnoAsync(Guid grupoId, Guid alumnoId, CancellationToken ct = default);

    /// <summary>Baja de la membresía (FechaBaja = hoy; se conserva la historia).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si no es miembro activo.</exception>
    Task QuitarAlumnoAsync(Guid grupoId, Guid alumnoId, CancellationToken ct = default);
}
