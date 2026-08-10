using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Pedidos de lugar en una clase (reemplaza a ISolicitudGrupoRepository).</summary>
public interface ISolicitudCupoRepository
{
    Task AgregarAsync(SolicitudCupo solicitud, CancellationToken ct = default);
    Task<SolicitudCupo?> ObtenerAsync(Guid id, CancellationToken ct = default);
    /// <summary>Pendientes del profe (con alumno + clase), el más viejo primero.</summary>
    Task<IReadOnlyList<SolicitudCupo>> ListarPorEstadoAsync(EstadoSolicitudGrupo estado, CancellationToken ct = default);
    /// <summary>Mis solicitudes (portal del alumno), la más reciente primero.</summary>
    Task<IReadOnlyList<SolicitudCupo>> ListarPorAlumnoAsync(Guid alumnoId, CancellationToken ct = default);
    Task<bool> ExistePendienteAsync(Guid alumnoId, Guid horarioId, CancellationToken ct = default);
    Task<int> ContarPorEstadoAsync(EstadoSolicitudGrupo estado, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class SolicitudCupoRepository : ISolicitudCupoRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public SolicitudCupoRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task AgregarAsync(SolicitudCupo solicitud, CancellationToken ct = default)
    {
        solicitud.TenantId = TenantId;
        _db.SolicitudesCupo.Add(solicitud);
        await Task.CompletedTask;
    }

    public Task<SolicitudCupo?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.SolicitudesCupo.FirstOrDefaultAsync(s => s.TenantId == TenantId && s.Id == id, ct);

    public async Task<IReadOnlyList<SolicitudCupo>> ListarPorEstadoAsync(
        EstadoSolicitudGrupo estado, CancellationToken ct = default) =>
        await _db.SolicitudesCupo
            .Include(s => s.Alumno)
            .Include(s => s.Horario).ThenInclude(h => h.Alumnos).ThenInclude(ah => ah.Alumno)
            .Where(s => s.TenantId == TenantId && s.Estado == estado)
            .OrderBy(s => s.CreadoEl)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SolicitudCupo>> ListarPorAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default) =>
        await _db.SolicitudesCupo
            // Cancha+sede, NO el roster: al alumno se le devuelve cuándo y dónde es la
            // clase, nunca su título (que puede ser el nombre de otro alumno). Traer los
            // compañeros era justo lo que alimentaba esa filtración.
            .Include(s => s.Horario).ThenInclude(h => h.Cancha).ThenInclude(c => c.Sede)
            .Where(s => s.TenantId == TenantId && s.AlumnoId == alumnoId)
            .OrderByDescending(s => s.CreadoEl)
            .ToListAsync(ct);

    public Task<bool> ExistePendienteAsync(Guid alumnoId, Guid horarioId, CancellationToken ct = default) =>
        _db.SolicitudesCupo.AnyAsync(
            s => s.TenantId == TenantId && s.AlumnoId == alumnoId && s.HorarioId == horarioId
                 && s.Estado == EstadoSolicitudGrupo.Pendiente, ct);

    public Task<int> ContarPorEstadoAsync(EstadoSolicitudGrupo estado, CancellationToken ct = default) =>
        _db.SolicitudesCupo.CountAsync(s => s.TenantId == TenantId && s.Estado == estado, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
