using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Clase individual fija (M5b): el alumno PROPONE día + hora + duración; el
/// sistema valida que haya al menos una cancha libre. El profe la acepta
/// eligiendo una cancha libre (se crea el <see cref="Horario"/> individual, que
/// genera los turnos) o la rechaza.
/// </summary>
public interface ISolicitudHorarioService
{
    Task<SolicitudHorarioDto> SolicitarAsync(
        Guid alumnoId, Guid sedeId, DayOfWeek dia, TimeOnly hora, int duracionMinutos, CancellationToken ct = default);

    /// <summary>
    /// Canchas activas libres a ese día/hora EN UNA SEDE. Lo usa el profe
    /// (dropdown al aceptar) y el portal (cuenta = "¿hay lugar en esa sede?").
    /// </summary>
    Task<IReadOnlyList<CanchaLibreDto>> CanchasLibresAsync(
        Guid sedeId, DayOfWeek dia, TimeOnly hora, int duracionMinutos, CancellationToken ct = default);

    /// <summary>Canchas libres para resolver una solicitud (usa su sede/día/hora). Para el profe.</summary>
    Task<IReadOnlyList<CanchaLibreDto>> CanchasLibresParaSolicitudAsync(Guid solicitudId, CancellationToken ct = default);

    Task<IReadOnlyList<SolicitudHorarioDto>> ListarPendientesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudHorarioDto>> MisAsync(Guid alumnoId, CancellationToken ct = default);
    Task<int> ContarPendientesAsync(CancellationToken ct = default);

    /// <summary>El profe acepta eligiendo una cancha: crea el horario individual.</summary>
    Task AceptarAsync(Guid solicitudId, Guid canchaId, CancellationToken ct = default);

    Task RechazarAsync(Guid solicitudId, CancellationToken ct = default);
}

public class SolicitudHorarioService : ISolicitudHorarioService
{
    private readonly ISolicitudHorarioRepository _solicitudes;
    private readonly IAlumnoRepository _alumnos;
    private readonly ISedeRepository _sedes;
    private readonly IHorarioRepository _horarios;
    private readonly ICargoRepository _cargos;
    private readonly IHorarioService _horarioService;

    public SolicitudHorarioService(
        ISolicitudHorarioRepository solicitudes, IAlumnoRepository alumnos, ISedeRepository sedes,
        IHorarioRepository horarios, ICargoRepository cargos, IHorarioService horarioService)
    {
        _solicitudes = solicitudes;
        _alumnos = alumnos;
        _sedes = sedes;
        _horarios = horarios;
        _cargos = cargos;
        _horarioService = horarioService;
    }

    public async Task<SolicitudHorarioDto> SolicitarAsync(
        Guid alumnoId, Guid sedeId, DayOfWeek dia, TimeOnly hora, int duracionMinutos, CancellationToken ct = default)
    {
        var alumno = await _alumnos.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("El alumno no existe.");
        if (alumno.Estado != EstadoAlumno.Activo)
            throw new ReglaDeNegocioException("Tu cuenta no está activa: hablá con tu profe.");

        var sede = await _sedes.ObtenerAsync(sedeId, ct);
        if (sede is null || !sede.Activo)
            throw new ReglaDeNegocioException("Esa sede no está disponible.");

        // Misma regla que asignar clases nuevas: nadie con la cuota vencida
        var impagos = await _cargos.ListarImpagosAsync([alumnoId], ct);
        if (CuotaService.TieneDeudaVencida(impagos, DateOnly.FromDateTime(DateTime.UtcNow)))
            throw new ReglaDeNegocioException(
                "Tenés la cuota vencida: regularizala antes de pedir clases nuevas.");

        if (await _solicitudes.ExistePendienteAsync(alumnoId, dia, hora, ct))
            throw new ReglaDeNegocioException("Ya pediste ese día y hora; esperá la respuesta del profe.");

        // Tiene que haber al menos UNA cancha libre a esa hora EN ESA SEDE
        var libres = await CanchasLibresAsync(sedeId, dia, hora, duracionMinutos, ct);
        if (libres.Count == 0)
            throw new ReglaDeNegocioException($"No hay canchas libres en {sede.Nombre} a esa hora. Probá otro horario.");

        var solicitud = new SolicitudHorario
        {
            AlumnoId = alumnoId,
            SedeId = sedeId,
            Dia = dia,
            HoraInicio = hora,
            DuracionMinutos = duracionMinutos,
        };
        await _solicitudes.AgregarAsync(solicitud, ct);
        await _solicitudes.GuardarCambiosAsync(ct);

        solicitud.Alumno = alumno; // para el Mapear
        solicitud.Sede = sede;
        return Mapear(solicitud);
    }

    public async Task<IReadOnlyList<CanchaLibreDto>> CanchasLibresAsync(
        Guid sedeId, DayOfWeek dia, TimeOnly hora, int duracionMinutos, CancellationToken ct = default)
    {
        var sede = await _sedes.ObtenerAsync(sedeId, ct);
        if (sede is null || !sede.Activo) return [];

        var libres = new List<CanchaLibreDto>();
        foreach (var cancha in sede.Canchas.Where(c => c.Activo))
        {
            var delDia = await _horarios.ListarPorCanchaYDiaAsync(cancha.Id, dia, ct);
            if (delDia.Any(h => Solapan(hora, duracionMinutos, h.HoraInicio, h.DuracionMinutos)))
                continue;
            libres.Add(new CanchaLibreDto { CanchaId = cancha.Id, Cancha = cancha.Nombre, Sede = sede.Nombre });
        }
        return libres;
    }

    public async Task<IReadOnlyList<CanchaLibreDto>> CanchasLibresParaSolicitudAsync(
        Guid solicitudId, CancellationToken ct = default)
    {
        var sol = await _solicitudes.ObtenerAsync(solicitudId, ct);
        if (sol is null) return [];
        return await CanchasLibresAsync(sol.SedeId, sol.Dia, sol.HoraInicio, sol.DuracionMinutos, ct);
    }

    public async Task<IReadOnlyList<SolicitudHorarioDto>> ListarPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _solicitudes.ListarPorEstadoAsync(EstadoSolicitudHorario.Pendiente, ct);
        return pendientes.Select(Mapear).ToList();
    }

    public async Task<IReadOnlyList<SolicitudHorarioDto>> MisAsync(Guid alumnoId, CancellationToken ct = default)
    {
        var mias = await _solicitudes.ListarPorAlumnoAsync(alumnoId, ct);
        return mias.Select(Mapear).ToList();
    }

    public Task<int> ContarPendientesAsync(CancellationToken ct = default) =>
        _solicitudes.ContarPorEstadoAsync(EstadoSolicitudHorario.Pendiente, ct);

    public async Task AceptarAsync(Guid solicitudId, Guid canchaId, CancellationToken ct = default)
    {
        var solicitud = await _solicitudes.ObtenerAsync(solicitudId, ct)
            ?? throw new ReglaDeNegocioException("La solicitud no existe.");
        if (solicitud.Estado != EstadoSolicitudHorario.Pendiente)
            throw new ReglaDeNegocioException("Esa solicitud ya fue resuelta.");

        // Crea el horario individual con el flujo del profe: valida el
        // solapamiento de ESA cancha y la deuda, y deja los turnos listos para
        // generarse. Si la cancha se ocupó, tira y la solicitud queda Pendiente.
        var horario = await _horarioService.CrearAsync(new CreateHorarioDto
        {
            CanchaId = canchaId,
            AlumnoId = solicitud.AlumnoId,
            Dia = solicitud.Dia,
            HoraInicio = solicitud.HoraInicio,
            DuracionMinutos = solicitud.DuracionMinutos,
        }, ct);

        solicitud.Estado = EstadoSolicitudHorario.Aceptada;
        solicitud.ResueltoEl = DateTime.UtcNow;
        solicitud.CanchaId = canchaId;
        solicitud.HorarioId = horario.Id;
        await _solicitudes.GuardarCambiosAsync(ct);
    }

    public async Task RechazarAsync(Guid solicitudId, CancellationToken ct = default)
    {
        var solicitud = await _solicitudes.ObtenerAsync(solicitudId, ct)
            ?? throw new ReglaDeNegocioException("La solicitud no existe.");
        if (solicitud.Estado != EstadoSolicitudHorario.Pendiente)
            throw new ReglaDeNegocioException("Esa solicitud ya fue resuelta.");

        solicitud.Estado = EstadoSolicitudHorario.Rechazada;
        solicitud.ResueltoEl = DateTime.UtcNow;
        await _solicitudes.GuardarCambiosAsync(ct);
    }

    /// <summary>Dos franjas (inicio+duración) del mismo día se pisan.</summary>
    private static bool Solapan(TimeOnly iniA, int durA, TimeOnly iniB, int durB) =>
        iniA < iniB.AddMinutes(durB) && iniB < iniA.AddMinutes(durA);

    private static SolicitudHorarioDto Mapear(SolicitudHorario s) => new()
    {
        Id = s.Id,
        AlumnoId = s.AlumnoId,
        AlumnoNombre = s.Alumno is null ? string.Empty : $"{s.Alumno.Nombre} {s.Alumno.Apellido}",
        Dia = s.Dia.ToString(),
        HoraInicio = s.HoraInicio,
        DuracionMinutos = s.DuracionMinutos,
        Sede = s.Sede?.Nombre ?? string.Empty,
        Estado = s.Estado.ToString(),
        Cancha = s.Cancha?.Nombre,
        CreadoEl = s.CreadoEl,
        ResueltoEl = s.ResueltoEl,
    };
}
