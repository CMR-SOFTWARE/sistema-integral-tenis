using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Contrato de negocio de Alumnos. Crece a medida que las verticales lo exigen.</summary>
public interface IAlumnoService
{
    /// <summary>
    /// Alta de alumno. Reglas: DNI único por tenant; si es menor de 18,
    /// tutor y consentimiento de datos obligatorios (Ley 25.326).
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si viola una regla.</exception>
    Task<AlumnoResponseDto> CrearAsync(CreateAlumnoDto dto, CancellationToken ct = default);

    /// <summary>Lista del tenant, filtrable por categoría y estado.</summary>
    Task<IReadOnlyList<AlumnoResponseDto>> ListarAsync(
        CategoriaAlumno? categoria, EstadoAlumno? estado, CancellationToken ct = default);

    /// <summary>Un alumno por id, o null si no existe en el tenant.</summary>
    Task<AlumnoResponseDto?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <summary>Pausar (Suspendido) o reactivar (Activo). Null si no existe.</summary>
    Task<AlumnoResponseDto?> CambiarEstadoAsync(Guid id, EstadoAlumno estado, CancellationToken ct = default);

    /// <summary>Baja lógica: estado → Inactivo, nunca DELETE físico. False si no existe.</summary>
    Task<bool> DarDeBajaAsync(Guid id, CancellationToken ct = default);
}
