using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface ISolicitudGrupoRepository
{
    Task AgregarAsync(SolicitudGrupo solicitud, CancellationToken ct = default);
    Task<SolicitudGrupo?> ObtenerAsync(Guid id, CancellationToken ct = default);
    /// <summary>Pendientes del profe (con alumno + grupo), el más viejo primero.</summary>
    Task<IReadOnlyList<SolicitudGrupo>> ListarPorEstadoAsync(EstadoSolicitudGrupo estado, CancellationToken ct = default);
    /// <summary>Mis solicitudes (portal del alumno), la más reciente primero.</summary>
    Task<IReadOnlyList<SolicitudGrupo>> ListarPorAlumnoAsync(Guid alumnoId, CancellationToken ct = default);
    Task<bool> ExistePendienteAsync(Guid alumnoId, Guid grupoId, CancellationToken ct = default);
    Task<int> ContarPorEstadoAsync(EstadoSolicitudGrupo estado, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class SolicitudGrupoRepository : ISolicitudGrupoRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public SolicitudGrupoRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task AgregarAsync(SolicitudGrupo solicitud, CancellationToken ct = default)
    {
        solicitud.TenantId = TenantId;
        _db.SolicitudesGrupo.Add(solicitud);
        await Task.CompletedTask;
    }

    public Task<SolicitudGrupo?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.SolicitudesGrupo.FirstOrDefaultAsync(s => s.TenantId == TenantId && s.Id == id, ct);

    public async Task<IReadOnlyList<SolicitudGrupo>> ListarPorEstadoAsync(
        EstadoSolicitudGrupo estado, CancellationToken ct = default) =>
        await _db.SolicitudesGrupo
            .Include(s => s.Alumno)
            .Include(s => s.Grupo)
            .Where(s => s.TenantId == TenantId && s.Estado == estado)
            .OrderBy(s => s.CreadoEl)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SolicitudGrupo>> ListarPorAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default) =>
        await _db.SolicitudesGrupo
            .Include(s => s.Grupo)
            .Where(s => s.TenantId == TenantId && s.AlumnoId == alumnoId)
            .OrderByDescending(s => s.CreadoEl)
            .ToListAsync(ct);

    public Task<bool> ExistePendienteAsync(Guid alumnoId, Guid grupoId, CancellationToken ct = default) =>
        _db.SolicitudesGrupo.AnyAsync(s =>
            s.TenantId == TenantId && s.AlumnoId == alumnoId &&
            s.GrupoId == grupoId && s.Estado == EstadoSolicitudGrupo.Pendiente, ct);

    public Task<int> ContarPorEstadoAsync(EstadoSolicitudGrupo estado, CancellationToken ct = default) =>
        _db.SolicitudesGrupo.CountAsync(s => s.TenantId == TenantId && s.Estado == estado, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
