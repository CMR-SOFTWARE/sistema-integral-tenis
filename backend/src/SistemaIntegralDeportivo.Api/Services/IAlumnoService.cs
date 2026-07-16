using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Contrato de negocio de Alumnos. Crece a medida que las verticales lo exigen.</summary>
public interface IAlumnoService
{
    /// <summary>
    /// Alta de alumno CON credenciales (plan v2: registro único — el profe
    /// crea usuario + ficha juntos; la temporal se devuelve UNA vez).
    /// Reglas: DNI único por tenant; menor → tutor + consentimiento; email
    /// sin cuenta previa.
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si viola una regla.</exception>
    Task<AlumnoCreadoDto> CrearAsync(CreateAlumnoDto dto, CancellationToken ct = default);

    /// <summary>Acceso al portal para una ficha vieja SIN usuario (genera la temporal).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Ya tiene acceso, o falta email.</exception>
    Task<AccesoCreadoDto> CrearAccesoAsync(Guid alumnoId, string? email, CancellationToken ct = default);

    /// <summary>
    /// Ficha para un usuario que YA existe (aprobación de solicitud): si hay
    /// ficha libre con el mismo DNI, la vincula; si no, crea una nueva con
    /// el UserId. Nunca toca Identity.
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">DNI de otra cuenta, o regla del menor.</exception>
    Task<AlumnoResponseDto> CrearVinculadoAsync(CreateAlumnoDto dto, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// El profe corrige los datos de la ficha. Reglas: DNI único (salvo el
    /// propio); si la fecha nueva lo vuelve menor, necesita tutor cargado.
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">No existe o viola una regla.</exception>
    Task<AlumnoResponseDto> EditarAsync(Guid id, UpdateAlumnoDto dto, CancellationToken ct = default);

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
