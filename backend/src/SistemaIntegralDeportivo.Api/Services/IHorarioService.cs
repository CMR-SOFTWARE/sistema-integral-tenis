using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Negocio de horarios recurrentes (la grilla semanal del profe).</summary>
public interface IHorarioService
{
    /// <summary>
    /// Alta de horario. Reglas: grupal XOR individual; sin solapamiento
    /// con otros horarios activos de la MISMA cancha (mismo día y rango).
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si viola una regla.</exception>
    Task<HorarioResponseDto> CrearAsync(CreateHorarioDto dto, CancellationToken ct = default);

    Task<IReadOnlyList<HorarioResponseDto>> ListarAsync(CancellationToken ct = default);

    /// <summary>(Re)asigna el profe del horario (null = sin asignar). Valida que sea del club.</summary>
    Task<HorarioResponseDto> AsignarProfesorAsync(Guid id, Guid? profesorUserId, CancellationToken ct = default);

    /// <summary>
    /// Desactiva la plantilla y limpia el futuro: los turnos con fecha ≥ hoy
    /// se borran junto con sus cargos impagos; los que tienen algún cargo
    /// pagado se conservan (plata cobrada). Lo pasado es historia y no se toca.
    /// </summary>
    Task DesactivarAsync(Guid id, CancellationToken ct = default);
}
