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

    /// <summary>Acceso al portal para una ficha SIN usuario (usa su celular, o uno alternativo).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Ya tiene acceso, o el celular ya está usado.</exception>
    Task<AccesoCreadoDto> CrearAccesoAsync(Guid alumnoId, string? telefonoAlternativo, CancellationToken ct = default);

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

    /// <summary>
    /// Lista del tenant, filtrable por categoría, estado y lista. La lista es lo que
    /// separa las tres pestañas del profe: <see cref="ListaAlumnos.ConClase"/> son sus
    /// alumnos, <see cref="ListaAlumnos.SinClase"/> los que esperan y
    /// <see cref="ListaAlumnos.Todos"/> el padrón entero.
    /// </summary>
    Task<IReadOnlyList<AlumnoResponseDto>> ListarAsync(
        CategoriaAlumno? categoria, EstadoAlumno? estado,
        ListaAlumnos lista = ListaAlumnos.Todos, CancellationToken ct = default);

    /// <summary>
    /// Las fichas pedidas, mapeadas EXACTAMENTE igual que <see cref="ListarAsync"/>
    /// (deuda y "tiene clase" resueltos en batch, no una consulta por alumno). La usa la
    /// lista de espera, que arma su propio conjunto por motivo pero muestra la misma fila
    /// que Alumnos. No aplica el recorte del profe empleado: el caller ya eligió a quién
    /// pide, y el aislamiento por tenant lo garantiza el repositorio.
    /// </summary>
    Task<IReadOnlyList<AlumnoResponseDto>> ListarPorIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    /// <summary>Un alumno por id, o null si no existe en el tenant.</summary>
    Task<AlumnoResponseDto?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <summary>Horarios asignados del alumno (grupos activos + horarios individuales), para su ficha.</summary>
    Task<IReadOnlyList<AlumnoHorarioDto>> HorariosDeAsync(Guid id, CancellationToken ct = default);

    /// <summary>La cuenta corriente del alumno (deuda total + últimos cargos), para su ficha.</summary>
    Task<AlumnoCuentaDto> CuentaDeAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Pausar (Suspendido) o reactivar (Activo). El estado manda sobre el
    /// calendario: al pausar sale de sus turnos futuros (y de sus cargos
    /// impagos) pero se le GUARDA el lugar; al reactivar vuelve solo.
    /// Null si no existe.
    /// </summary>
    Task<AlumnoResponseDto?> CambiarEstadoAsync(Guid id, EstadoAlumno estado, CancellationToken ct = default);

    /// <summary>
    /// Asigna/cambia el PROFE TITULAR del alumno desde su ficha (null = desasignar).
    /// El club de la ficha sigue al profe (hereda su sede). Null si el alumno no existe.
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">El profe no es asignable en el club.</exception>
    Task<AlumnoResponseDto?> CambiarProfesorAsync(Guid id, Guid? profesorUserId, CancellationToken ct = default);

    /// <summary>
    /// Baja lógica: estado → Inactivo, nunca DELETE físico. Además de sacarlo
    /// del calendario, LIBERA su lugar (sale de sus grupos y se desactivan sus
    /// horarios individuales). False si no existe.
    /// </summary>
    Task<bool> DarDeBajaAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Borrado REAL (hard delete): elimina la ficha y TODO su historial (cargos,
    /// participaciones, membresías, raquetas, notas, solicitudes, horarios y
    /// turnos individuales). No es reversible. Si era la última ficha de la
    /// cuenta, borra también el login (una familia con más miembros lo conserva).
    /// Antes de borrar lo saca del calendario y recalcula el divisor de sus
    /// compañeros (como la baja). False si no existe.
    /// </summary>
    Task<bool> EliminarDefinitivoAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Alumnos ACTIVOS a los que se les pasó el día 15 sin pagar (candidatos
    /// a que el profe los saque del calendario).
    /// </summary>
    Task<IReadOnlyList<MorosoDto>> ListarMorososAsync(CancellationToken ct = default);

    /// <summary>
    /// Reconcilia los turnos futuros del alumno con su realidad actual (estado
    /// + membresías activas + horarios individuales activos). Idempotente: se
    /// llama después de CUALQUIER cambio que afecte dónde debe estar —pausar,
    /// reactivar, baja, entrar/salir de un grupo—. Un solo lugar para toda la
    /// sincronización estado↔calendario. NO persiste: el caller hace un único
    /// GuardarCambios (mismo DbContext).
    /// </summary>
    Task SincronizarCalendarioAsync(Guid alumnoId, CancellationToken ct = default);
}
