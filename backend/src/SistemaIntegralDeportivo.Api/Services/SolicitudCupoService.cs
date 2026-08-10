using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// El alumno pide un lugar en una clase fija: ve la grilla semanal de SU club y pide
/// sumarse a una que tenga lugar. El profe acepta (lo suma al roster, que reconcilia el
/// calendario) o rechaza.
///
/// Reemplaza a ISolicitudGrupoService: el flujo es el mismo, pero sobre horarios.
/// </summary>
public interface ISolicitudCupoService
{
    /// <summary>
    /// La grilla de Reservar: TODAS las clases del club del alumno, cada una marcada como
    /// disponible, ocupada o suya. Lo ocupado va sin datos a propósito (ver
    /// <see cref="SlotReservaDto"/>).
    /// </summary>
    Task<IReadOnlyList<SlotReservaDto>> GrillaParaAlumnoAsync(Guid alumnoId, CancellationToken ct = default);
    Task<MiSolicitudCupoDto> SolicitarAsync(Guid alumnoId, Guid horarioId, CancellationToken ct = default);
    /// <summary>Los pendientes que ve el PROFE (con nombres, que ahí sí corresponden).</summary>
    Task<IReadOnlyList<SolicitudCupoDto>> ListarPendientesAsync(CancellationToken ct = default);
    /// <summary>Mis pedidos vistos desde el PORTAL: sin nombre de clase ni de alumno.</summary>
    Task<IReadOnlyList<MiSolicitudCupoDto>> MisAsync(Guid alumnoId, CancellationToken ct = default);
    Task<int> ContarPendientesAsync(CancellationToken ct = default);
    Task AceptarAsync(Guid solicitudId, CancellationToken ct = default);
    Task RechazarAsync(Guid solicitudId, CancellationToken ct = default);
}

public class SolicitudCupoService : ISolicitudCupoService
{
    private readonly ISolicitudCupoRepository _solicitudes;
    private readonly IAlumnoRepository _alumnos;
    private readonly IHorarioRepository _horarios;
    private readonly ICargoRepository _cargos;
    private readonly IHorarioService _horarioService;

    public SolicitudCupoService(
        ISolicitudCupoRepository solicitudes, IAlumnoRepository alumnos,
        IHorarioRepository horarios, ICargoRepository cargos,
        IHorarioService horarioService)
    {
        _solicitudes = solicitudes;
        _alumnos = alumnos;
        _horarios = horarios;
        _cargos = cargos;
        _horarioService = horarioService;
    }

    public async Task<IReadOnlyList<SlotReservaDto>> GrillaParaAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default)
    {
        var alumno = await _alumnos.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("El alumno no existe.");

        var horarios = await _horarios.ListarActivosAsync(ct);
        var pendientes = (await _solicitudes.ListarPorAlumnoAsync(alumnoId, ct))
            .Where(s => s.Estado == EstadoSolicitudGrupo.Pendiente)
            .Select(s => s.HorarioId)
            .ToHashSet();

        // Solo su club. Si la ficha no tiene club asignado se muestran todos: una
        // pantalla vacía sin explicación es peor que ver de más (mismo criterio que
        // AsignarHorarioModal del lado del profe).
        var slots = horarios
            .Where(h => alumno.SedeId is null || h.Cancha?.Sede?.Id == alumno.SedeId)
            .Select(h => Slot(h, alumno, pendientes.Contains(h.Id)));

        return [.. slots.OrderBy(c => c.Dia).ThenBy(c => c.HoraInicio)];
    }

    /// <summary>
    /// Clasifica una clase para ESTE alumno. Lo ocupado sale sin id, sin categoría y sin
    /// lugares: si no lo puede pedir, no necesita saber nada de esa clase.
    /// </summary>
    private static SlotReservaDto Slot(Horario h, Alumno alumno, bool pendiente)
    {
        var activos = h.Alumnos.Count(m => m.FechaBaja is null);
        var yaViene = h.Alumnos.Any(m => m.AlumnoId == alumno.Id && m.FechaBaja is null);
        var sinLugar = h.CupoMaximo is not null && activos >= h.CupoMaximo;
        var otraCategoria = !Categorias.EsCompatible(h.Categoria, alumno.Categoria);

        var estado = yaViene ? EstadoSlot.Mia
            : sinLugar || otraCategoria ? EstadoSlot.Ocupado
            : EstadoSlot.Disponible;
        var libre = estado == EstadoSlot.Disponible;

        return new SlotReservaDto
        {
            HorarioId = libre ? h.Id : null,
            Estado = estado.ToString(),
            Dia = h.Dia.ToString(),
            HoraInicio = h.HoraInicio,
            DuracionMinutos = h.DuracionMinutos,
            Sede = h.Cancha?.Sede?.Nombre ?? string.Empty,
            Categoria = libre ? h.Categoria?.ToString() : null,
            // Null con cupo abierto: "hay lugar" sin decir cuántos son.
            LugaresLibres = libre && h.CupoMaximo is { } cupo ? cupo - activos : null,
            SolicitudPendiente = libre && pendiente,
        };
    }

    public async Task<MiSolicitudCupoDto> SolicitarAsync(
        Guid alumnoId, Guid horarioId, CancellationToken ct = default)
    {
        var alumno = await _alumnos.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("El alumno no existe.");
        var horario = await _horarios.ObtenerConRosterAsync(horarioId, ct)
            ?? throw new ReglaDeNegocioException("Esa clase no existe.");

        if (!horario.Activo)
            throw new ReglaDeNegocioException("Esa clase ya no está disponible.");
        if (horario.Alumnos.Any(m => m.AlumnoId == alumnoId && m.FechaBaja is null))
            throw new ReglaDeNegocioException("Ya venís a esa clase.");
        if (!Categorias.EsCompatible(horario.Categoria, alumno.Categoria))
            throw new ReglaDeNegocioException("Esa clase es de otra categoría.");

        // Nadie pide clases NUEVAS con la cuota vencida. La regla vive acá y no en
        // HorarioService: al profe no lo frena (él suma a quien quiera desde la
        // agenda), frena al alumno que se anota solo. Misma regla que pedir una
        // clase individual o una suelta.
        var impagos = await _cargos.ListarImpagosAsync([alumnoId], ct);
        if (CuotaService.TieneDeudaVencida(impagos, DateOnly.FromDateTime(DateTime.UtcNow)))
            throw new ReglaDeNegocioException(
                "Tenés la cuota vencida: regularizala antes de pedir clases nuevas.");

        var activos = horario.Alumnos.Count(m => m.FechaBaja is null);
        if (horario.CupoMaximo is not null && activos >= horario.CupoMaximo)
            throw new ReglaDeNegocioException("Esa clase ya no tiene lugar.");

        if (await _solicitudes.ExistePendienteAsync(alumnoId, horarioId, ct))
            throw new ReglaDeNegocioException("Ya pediste sumarte a esa clase; esperá la respuesta del profe.");

        var solicitud = new SolicitudCupo { AlumnoId = alumnoId, HorarioId = horarioId };
        await _solicitudes.AgregarAsync(solicitud, ct);
        await _solicitudes.GuardarCambiosAsync(ct);

        solicitud.Horario = horario; // para el Mapear
        return MapearParaElAlumno(solicitud);
    }

    public async Task<IReadOnlyList<SolicitudCupoDto>> ListarPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _solicitudes.ListarPorEstadoAsync(EstadoSolicitudGrupo.Pendiente, ct);
        return pendientes.Select(Mapear).ToList();
    }

    public async Task<IReadOnlyList<MiSolicitudCupoDto>> MisAsync(Guid alumnoId, CancellationToken ct = default)
    {
        var mias = await _solicitudes.ListarPorAlumnoAsync(alumnoId, ct);
        return mias.Select(MapearParaElAlumno).ToList();
    }

    public Task<int> ContarPendientesAsync(CancellationToken ct = default) =>
        _solicitudes.ContarPorEstadoAsync(EstadoSolicitudGrupo.Pendiente, ct);

    public async Task AceptarAsync(Guid solicitudId, CancellationToken ct = default)
    {
        var solicitud = await _solicitudes.ObtenerAsync(solicitudId, ct)
            ?? throw new ReglaDeNegocioException("La solicitud no existe.");
        if (solicitud.Estado != EstadoSolicitudGrupo.Pendiente)
            throw new ReglaDeNegocioException("Esa solicitud ya fue resuelta.");

        // Lo suma al roster: revalida cupo/estado/deuda y RECONCILIA los turnos
        // futuros. Si algo falla —p.ej. el cupo se llenó— tira y la solicitud queda
        // Pendiente para reintentar.
        await _horarioService.AgregarAlumnoAsync(solicitud.HorarioId, solicitud.AlumnoId, ct);

        solicitud.Estado = EstadoSolicitudGrupo.Aceptada;
        solicitud.ResueltoEl = DateTime.UtcNow;
        await _solicitudes.GuardarCambiosAsync(ct);
    }

    public async Task RechazarAsync(Guid solicitudId, CancellationToken ct = default)
    {
        var solicitud = await _solicitudes.ObtenerAsync(solicitudId, ct)
            ?? throw new ReglaDeNegocioException("La solicitud no existe.");
        if (solicitud.Estado != EstadoSolicitudGrupo.Pendiente)
            throw new ReglaDeNegocioException("Esa solicitud ya fue resuelta.");

        solicitud.Estado = EstadoSolicitudGrupo.Rechazada;
        solicitud.ResueltoEl = DateTime.UtcNow;
        await _solicitudes.GuardarCambiosAsync(ct);
    }

    /// <summary>Para el PROFE: necesita saber quién pide y a qué clase.</summary>
    private static SolicitudCupoDto Mapear(SolicitudCupo s) => new()
    {
        Id = s.Id,
        AlumnoId = s.AlumnoId,
        AlumnoNombre = s.Alumno is null ? string.Empty : $"{s.Alumno.Nombre} {s.Alumno.Apellido}",
        HorarioId = s.HorarioId,
        ClaseNombre = s.Horario is null ? string.Empty : HorarioService.TituloDe(s.Horario.Nombre, s.Horario.Alumnos),
        Estado = s.Estado.ToString(),
        CreadoEl = s.CreadoEl,
        ResueltoEl = s.ResueltoEl,
    };

    /// <summary>
    /// Para el ALUMNO: la clase se identifica por cuándo es. Nada de títulos — el de una
    /// clase de una persona es el nombre de esa persona.
    /// </summary>
    private static MiSolicitudCupoDto MapearParaElAlumno(SolicitudCupo s) => new()
    {
        Id = s.Id,
        Dia = s.Horario?.Dia.ToString() ?? string.Empty,
        HoraInicio = s.Horario?.HoraInicio ?? default,
        DuracionMinutos = s.Horario?.DuracionMinutos ?? 0,
        Sede = s.Horario?.Cancha?.Sede?.Nombre ?? string.Empty,
        Estado = s.Estado.ToString(),
        CreadoEl = s.CreadoEl,
        ResueltoEl = s.ResueltoEl,
    };
}
