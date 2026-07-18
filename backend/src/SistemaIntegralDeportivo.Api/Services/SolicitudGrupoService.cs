using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Reservar horario fijo grupal (M5a): el alumno ve los grupos a los que
/// PODRÍA sumarse (cupo + su categoría) con el precio estimado, y pide entrar.
/// El profe acepta (lo suma al grupo, que reconcilia el calendario) o rechaza.
/// </summary>
public interface ISolicitudGrupoService
{
    /// <summary>Grupos con cupo y categoría compatible para el alumno, con precio estimado.</summary>
    Task<IReadOnlyList<GrupoDisponibleDto>> DisponiblesParaAlumnoAsync(Guid alumnoId, CancellationToken ct = default);

    /// <summary>El alumno pide sumarse a un grupo (queda Pendiente).</summary>
    Task<SolicitudGrupoDto> SolicitarAsync(Guid alumnoId, Guid grupoId, CancellationToken ct = default);

    Task<IReadOnlyList<SolicitudGrupoDto>> ListarPendientesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudGrupoDto>> MisAsync(Guid alumnoId, CancellationToken ct = default);
    Task<int> ContarPendientesAsync(CancellationToken ct = default);

    /// <summary>El profe acepta: suma al alumno al grupo (reconcilia el calendario).</summary>
    Task AceptarAsync(Guid solicitudId, CancellationToken ct = default);

    /// <summary>El profe rechaza la solicitud (sin sumar a nadie).</summary>
    Task RechazarAsync(Guid solicitudId, CancellationToken ct = default);
}

public class SolicitudGrupoService : ISolicitudGrupoService
{
    private readonly ISolicitudGrupoRepository _solicitudes;
    private readonly IGrupoRepository _grupos;
    private readonly IAlumnoRepository _alumnos;
    private readonly IHorarioRepository _horarios;
    private readonly ITenantRepository _tenant;
    private readonly IGrupoService _grupoService;

    public SolicitudGrupoService(
        ISolicitudGrupoRepository solicitudes, IGrupoRepository grupos, IAlumnoRepository alumnos,
        IHorarioRepository horarios, ITenantRepository tenant, IGrupoService grupoService)
    {
        _solicitudes = solicitudes;
        _grupos = grupos;
        _alumnos = alumnos;
        _horarios = horarios;
        _tenant = tenant;
        _grupoService = grupoService;
    }

    public async Task<IReadOnlyList<GrupoDisponibleDto>> DisponiblesParaAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default)
    {
        var alumno = await _alumnos.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("El alumno no existe.");

        var grupos = await _grupos.ListarAsync(ct);
        var horarios = await _horarios.ListarActivosAsync(ct);
        var tenant = await _tenant.ObtenerActualAsync(ct);
        var pendientes = (await _solicitudes.ListarPorAlumnoAsync(alumnoId, ct))
            .Where(s => s.Estado == EstadoSolicitudGrupo.Pendiente)
            .Select(s => s.GrupoId)
            .ToHashSet();

        var horariosPorGrupo = horarios
            .Where(h => h.GrupoId is not null)
            .ToLookup(h => h.GrupoId!.Value);

        var disponibles = new List<GrupoDisponibleDto>();
        foreach (var g in grupos.Where(g => g.Activo))
        {
            var activos = g.Alumnos.Count(m => m.FechaBaja is null);
            var yaEsMiembro = g.Alumnos.Any(m => m.AlumnoId == alumnoId && m.FechaBaja is null);
            if (yaEsMiembro) continue;
            if (g.CupoMaximo is not null && activos >= g.CupoMaximo) continue;
            if (!CategoriaCompatible(g.Categoria, alumno.Categoria)) continue;

            var futuros = activos + 1; // contándolo a él para estimar el divisor
            var hs = horariosPorGrupo[g.Id].Select(h => new HorarioDisponibleDto
            {
                Dia = h.Dia.ToString(),
                HoraInicio = h.HoraInicio,
                DuracionMinutos = h.DuracionMinutos,
                Sede = h.Cancha?.Sede?.Nombre ?? string.Empty,
                Cancha = h.Cancha?.Nombre ?? string.Empty,
                PrecioEstimado = tenant.ValorHoraGrupal is null
                    ? null
                    : Math.Round(tenant.ValorHoraGrupal.Value * h.DuracionMinutos / 60m / futuros, 2),
            }).ToList();

            disponibles.Add(new GrupoDisponibleDto
            {
                GrupoId = g.Id,
                Nombre = g.Nombre,
                Categoria = g.Categoria?.ToString(),
                MiembrosActivos = activos,
                CupoMaximo = g.CupoMaximo,
                Horarios = hs,
                SolicitudPendiente = pendientes.Contains(g.Id),
            });
        }

        return disponibles.OrderBy(g => g.Nombre).ToList();
    }

    public async Task<SolicitudGrupoDto> SolicitarAsync(
        Guid alumnoId, Guid grupoId, CancellationToken ct = default)
    {
        var alumno = await _alumnos.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("El alumno no existe.");
        var grupo = await _grupos.ObtenerAsync(grupoId, ct)
            ?? throw new ReglaDeNegocioException("El grupo no existe.");

        if (!grupo.Activo)
            throw new ReglaDeNegocioException("Ese grupo ya no está disponible.");
        if (grupo.Alumnos.Any(m => m.AlumnoId == alumnoId && m.FechaBaja is null))
            throw new ReglaDeNegocioException("Ya sos parte de ese grupo.");
        if (!CategoriaCompatible(grupo.Categoria, alumno.Categoria))
            throw new ReglaDeNegocioException("Ese grupo es de otra categoría.");

        var activos = grupo.Alumnos.Count(m => m.FechaBaja is null);
        if (grupo.CupoMaximo is not null && activos >= grupo.CupoMaximo)
            throw new ReglaDeNegocioException("Ese grupo ya no tiene lugar.");

        if (await _solicitudes.ExistePendienteAsync(alumnoId, grupoId, ct))
            throw new ReglaDeNegocioException("Ya pediste sumarte a ese grupo; esperá la respuesta del profe.");

        var solicitud = new SolicitudGrupo { AlumnoId = alumnoId, GrupoId = grupoId };
        await _solicitudes.AgregarAsync(solicitud, ct);
        await _solicitudes.GuardarCambiosAsync(ct);

        solicitud.Grupo = grupo; // para el Mapear
        return Mapear(solicitud);
    }

    public async Task<IReadOnlyList<SolicitudGrupoDto>> ListarPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _solicitudes.ListarPorEstadoAsync(EstadoSolicitudGrupo.Pendiente, ct);
        return pendientes.Select(Mapear).ToList();
    }

    public async Task<IReadOnlyList<SolicitudGrupoDto>> MisAsync(Guid alumnoId, CancellationToken ct = default)
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

        // Suma al alumno al grupo: revalida cupo/estado/deuda y RECONCILIA los
        // turnos futuros (lo repone en el calendario). Si algo falla —p.ej. el
        // cupo se llenó— tira y la solicitud queda Pendiente para reintentar.
        await _grupoService.AsignarAlumnoAsync(solicitud.GrupoId, solicitud.AlumnoId, ct);

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

    /// <summary>
    /// El grupo sin categoría asignada (null o SinCategoria) es ABIERTO a
    /// todos; si tiene una categoría, la del alumno tiene que coincidir.
    /// </summary>
    private static bool CategoriaCompatible(CategoriaAlumno? grupo, CategoriaAlumno alumno) =>
        grupo is null || grupo == CategoriaAlumno.SinCategoria || grupo == alumno;

    private static SolicitudGrupoDto Mapear(SolicitudGrupo s) => new()
    {
        Id = s.Id,
        AlumnoId = s.AlumnoId,
        AlumnoNombre = s.Alumno is null ? string.Empty : $"{s.Alumno.Nombre} {s.Alumno.Apellido}",
        GrupoId = s.GrupoId,
        GrupoNombre = s.Grupo?.Nombre ?? string.Empty,
        Estado = s.Estado.ToString(),
        CreadoEl = s.CreadoEl,
        ResueltoEl = s.ResueltoEl,
    };
}
