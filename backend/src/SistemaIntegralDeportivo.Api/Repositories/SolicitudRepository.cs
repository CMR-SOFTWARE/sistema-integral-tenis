using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class SolicitudRepository : ISolicitudRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;

    private Guid TenantId => _tenantActual.TenantId;

    public SolicitudRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<Solicitud>> ListarPorUsuarioAsync(
        Guid userId, CancellationToken ct = default) =>
        await _db.Solicitudes
            .AsNoTracking()
            .Include(s => s.Tenant)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreadoEl)
            .ToListAsync(ct);

    public Task<bool> ExistePendienteAsync(Guid userId, Guid tenantId, CancellationToken ct = default) =>
        _db.Solicitudes.AnyAsync(
            s => s.UserId == userId && s.TenantId == tenantId && s.Estado == EstadoSolicitud.Pendiente, ct);

    public async Task<IReadOnlyList<(Solicitud, Usuario)>> ListarPendientesConUsuarioAsync(
        CancellationToken ct = default)
    {
        var lista = await (
            from s in _db.Solicitudes.AsNoTracking()
            join u in _db.Users.AsNoTracking() on s.UserId equals u.Id
            where s.TenantId == TenantId && s.Estado == EstadoSolicitud.Pendiente
            orderby s.CreadoEl
            select new { s, u }).ToListAsync(ct);

        return lista.Select(x => (x.s, x.u)).ToList();
    }

    public async Task<(Solicitud, Usuario)?> ObtenerPendienteConUsuarioAsync(
        Guid id, CancellationToken ct = default)
    {
        // OJO: sin AsNoTracking en NINGUNA fuente — un solo AsNoTracking apaga
        // el tracking de TODA la query y aprobar/rechazar mutan la solicitud
        // (bug real: la aprobación no se persistía)
        var fila = await (
            from s in _db.Solicitudes
            join u in _db.Users on s.UserId equals u.Id
            where s.Id == id && s.TenantId == TenantId && s.Estado == EstadoSolicitud.Pendiente
            select new { s, u }).FirstOrDefaultAsync(ct);

        return fila is null ? null : (fila.s, fila.u);
    }

    public Task<int> ContarPendientesAsync(CancellationToken ct = default) =>
        _db.Solicitudes.CountAsync(
            s => s.TenantId == TenantId && s.Estado == EstadoSolicitud.Pendiente, ct);

    public Task AgregarAsync(Solicitud solicitud, CancellationToken ct = default)
    {
        _db.Solicitudes.Add(solicitud);
        return Task.CompletedTask; // se persiste con GuardarCambiosAsync
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
