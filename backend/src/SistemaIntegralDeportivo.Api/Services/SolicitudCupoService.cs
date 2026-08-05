using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// El alumno pide un lugar en una clase fija: ve las que tienen cupo libre y son de
/// su categoría, con el precio estimado, y pide sumarse. El profe acepta (lo suma al
/// roster, que reconcilia el calendario) o rechaza.
///
/// Reemplaza a ISolicitudGrupoService: el flujo es el mismo, pero sobre horarios.
/// </summary>
public interface ISolicitudCupoService
{
    Task<IReadOnlyList<ClaseDisponibleDto>> DisponiblesParaAlumnoAsync(Guid alumnoId, CancellationToken ct = default);
    Task<SolicitudCupoDto> SolicitarAsync(Guid alumnoId, Guid horarioId, CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudCupoDto>> ListarPendientesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudCupoDto>> MisAsync(Guid alumnoId, CancellationToken ct = default);
    Task<int> ContarPendientesAsync(CancellationToken ct = default);
    Task AceptarAsync(Guid solicitudId, CancellationToken ct = default);
    Task RechazarAsync(Guid solicitudId, CancellationToken ct = default);
}

public class SolicitudCupoService : ISolicitudCupoService
{
    private readonly ISolicitudCupoRepository _solicitudes;
    private readonly IAlumnoRepository _alumnos;
    private readonly IHorarioRepository _horarios;
    private readonly ITenantRepository _tenant;
    private readonly IHorarioService _horarioService;

    public SolicitudCupoService(
        ISolicitudCupoRepository solicitudes, IAlumnoRepository alumnos,
        IHorarioRepository horarios, ITenantRepository tenant, IHorarioService horarioService)
    {
        _solicitudes = solicitudes;
        _alumnos = alumnos;
        _horarios = horarios;
        _tenant = tenant;
        _horarioService = horarioService;
    }

    public async Task<IReadOnlyList<ClaseDisponibleDto>> DisponiblesParaAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default)
    {
        var alumno = await _alumnos.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("El alumno no existe.");

        var horarios = await _horarios.ListarActivosAsync(ct);
        var tenant = await _tenant.ObtenerActualAsync(ct);
        var pendientes = (await _solicitudes.ListarPorAlumnoAsync(alumnoId, ct))
            .Where(s => s.Estado == EstadoSolicitudGrupo.Pendiente)
            .Select(s => s.HorarioId)
            .ToHashSet();

        var disponibles = new List<ClaseDisponibleDto>();
        foreach (var h in horarios)
        {
            var activos = h.Alumnos.Count(m => m.FechaBaja is null);
            if (h.Alumnos.Any(m => m.AlumnoId == alumnoId && m.FechaBaja is null)) continue; // ya viene
            if (h.CupoMaximo is not null && activos >= h.CupoMaximo) continue;               // sin lugar
            if (!Categorias.EsCompatible(h.Categoria, alumno.Categoria)) continue;

            var futuros = activos + 1; // contándolo a él para estimar el divisor
            disponibles.Add(new ClaseDisponibleDto
            {
                HorarioId = h.Id,
                Titulo = HorarioService.TituloDe(h.Nombre, h.Alumnos),
                Categoria = h.Categoria?.ToString(),
                MiembrosActivos = activos,
                CupoMaximo = h.CupoMaximo,
                Dia = h.Dia.ToString(),
                HoraInicio = h.HoraInicio,
                DuracionMinutos = h.DuracionMinutos,
                Sede = h.Cancha?.Sede?.Nombre ?? string.Empty,
                Cancha = h.Cancha?.Nombre ?? string.Empty,
                PrecioEstimado = tenant.ValorHoraGrupal is null
                    ? null
                    : Math.Round(tenant.ValorHoraGrupal.Value * h.DuracionMinutos / 60m / futuros, 2),
                SolicitudPendiente = pendientes.Contains(h.Id),
            });
        }

        return [.. disponibles.OrderBy(c => c.Dia).ThenBy(c => c.HoraInicio)];
    }

    public async Task<SolicitudCupoDto> SolicitarAsync(
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

        var activos = horario.Alumnos.Count(m => m.FechaBaja is null);
        if (horario.CupoMaximo is not null && activos >= horario.CupoMaximo)
            throw new ReglaDeNegocioException("Esa clase ya no tiene lugar.");

        if (await _solicitudes.ExistePendienteAsync(alumnoId, horarioId, ct))
            throw new ReglaDeNegocioException("Ya pediste sumarte a esa clase; esperá la respuesta del profe.");

        var solicitud = new SolicitudCupo { AlumnoId = alumnoId, HorarioId = horarioId };
        await _solicitudes.AgregarAsync(solicitud, ct);
        await _solicitudes.GuardarCambiosAsync(ct);

        solicitud.Horario = horario; // para el Mapear
        return Mapear(solicitud);
    }

    public async Task<IReadOnlyList<SolicitudCupoDto>> ListarPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _solicitudes.ListarPorEstadoAsync(EstadoSolicitudGrupo.Pendiente, ct);
        return pendientes.Select(Mapear).ToList();
    }

    public async Task<IReadOnlyList<SolicitudCupoDto>> MisAsync(Guid alumnoId, CancellationToken ct = default)
    {
        var mias = await _solicitudes.ListarPorAlumnoAsync(alumnoId, ct);
        return mias.Select(Mapear).ToList();
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
}
