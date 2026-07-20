using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface ISolicitudHorarioRepository
{
    Task AgregarAsync(SolicitudHorario solicitud, CancellationToken ct = default);
    Task<SolicitudHorario?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudHorario>> ListarPorEstadoAsync(EstadoSolicitudHorario estado, CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudHorario>> ListarPorAlumnoAsync(Guid alumnoId, CancellationToken ct = default);
    Task<bool> ExistePendienteAsync(Guid alumnoId, DayOfWeek dia, TimeOnly hora, CancellationToken ct = default);
    Task<int> ContarPorEstadoAsync(EstadoSolicitudHorario estado, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class SolicitudHorarioRepository : ISolicitudHorarioRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public SolicitudHorarioRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task AgregarAsync(SolicitudHorario solicitud, CancellationToken ct = default)
    {
        solicitud.TenantId = TenantId;
        _db.SolicitudesHorario.Add(solicitud);
        await Task.CompletedTask;
    }

    public Task<SolicitudHorario?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.SolicitudesHorario.FirstOrDefaultAsync(s => s.TenantId == TenantId && s.Id == id, ct);

    public async Task<IReadOnlyList<SolicitudHorario>> ListarPorEstadoAsync(
        EstadoSolicitudHorario estado, CancellationToken ct = default) =>
        await _db.SolicitudesHorario
            .Include(s => s.Alumno)
            .Include(s => s.Sede)
            .Where(s => s.TenantId == TenantId && s.Estado == estado)
            .OrderBy(s => s.CreadoEl)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SolicitudHorario>> ListarPorAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default) =>
        await _db.SolicitudesHorario
            .Include(s => s.Sede)
            .Include(s => s.Cancha)
            .Where(s => s.TenantId == TenantId && s.AlumnoId == alumnoId)
            .OrderByDescending(s => s.CreadoEl)
            .ToListAsync(ct);

    public Task<bool> ExistePendienteAsync(Guid alumnoId, DayOfWeek dia, TimeOnly hora, CancellationToken ct = default) =>
        _db.SolicitudesHorario.AnyAsync(s =>
            s.TenantId == TenantId && s.AlumnoId == alumnoId && s.Dia == dia &&
            s.HoraInicio == hora && s.Estado == EstadoSolicitudHorario.Pendiente, ct);

    public Task<int> ContarPorEstadoAsync(EstadoSolicitudHorario estado, CancellationToken ct = default) =>
        _db.SolicitudesHorario.CountAsync(s => s.TenantId == TenantId && s.Estado == estado, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
