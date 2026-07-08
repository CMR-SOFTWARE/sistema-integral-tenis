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

    /// <summary>Desactiva la plantilla (los turnos ya generados no se tocan).</summary>
    Task DesactivarAsync(Guid id, CancellationToken ct = default);
}
