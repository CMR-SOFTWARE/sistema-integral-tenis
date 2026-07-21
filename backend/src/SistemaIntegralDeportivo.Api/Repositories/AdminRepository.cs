using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>
/// Acceso CROSS-TENANT: el único repo que NO scopea por ITenantActual (consulta
/// todos los clubes). Solo lo usa el AdminController, gateado por policy Admin.
/// </summary>
public interface IAdminRepository
{
    Task<IReadOnlyDictionary<EstadoTenant, int>> ContarClubesPorEstadoAsync(CancellationToken ct = default);
    Task<int> ContarStaffActivosAsync(CancellationToken ct = default);
    Task<int> ContarAlumnosActivosAsync(CancellationToken ct = default);
    Task<decimal> IngresosDelMesAsync(int anio, int mes, CancellationToken ct = default);
    Task<int> ContarClubesNuevosAsync(DateTime desde, CancellationToken ct = default);
    Task<int> ContarAlumnosNuevosAsync(DateTime desde, CancellationToken ct = default);
    Task<IReadOnlyList<ClubAdminDto>> ListarClubesAsync(CancellationToken ct = default);
    Task<Tenant?> ObtenerTenantAsync(Guid id, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _db;

    public AdminRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<EstadoTenant, int>> ContarClubesPorEstadoAsync(CancellationToken ct = default)
    {
        var filas = await _db.Tenants.AsNoTracking()
            .GroupBy(t => t.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync(ct);
        return filas.ToDictionary(x => x.Estado, x => x.Cantidad);
    }

    public Task<int> ContarStaffActivosAsync(CancellationToken ct = default) =>
        _db.MembresiasTenant.CountAsync(m => m.Activo, ct);

    public Task<int> ContarAlumnosActivosAsync(CancellationToken ct = default) =>
        _db.Alumnos.CountAsync(a => a.Estado == EstadoAlumno.Activo, ct);

    public async Task<decimal> IngresosDelMesAsync(int anio, int mes, CancellationToken ct = default)
    {
        var desde = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = desde.AddMonths(1);
        // Pagos CONFIRMADOS (PagadoEl) del mes; ignora ajustes negativos.
        return await _db.Cargos.AsNoTracking()
            .Where(c => c.PagadoEl >= desde && c.PagadoEl < hasta && c.Monto > 0)
            .SumAsync(c => (decimal?)c.Monto, ct) ?? 0m;
    }

    public Task<int> ContarClubesNuevosAsync(DateTime desde, CancellationToken ct = default) =>
        _db.Tenants.CountAsync(t => t.CreadoEl >= desde, ct);

    public Task<int> ContarAlumnosNuevosAsync(DateTime desde, CancellationToken ct = default) =>
        _db.Alumnos.CountAsync(a => a.CreadoEl >= desde, ct);

    public async Task<IReadOnlyList<ClubAdminDto>> ListarClubesAsync(CancellationToken ct = default)
    {
        var query =
            from t in _db.Tenants.AsNoTracking()
            join u in _db.Users.AsNoTracking() on t.OwnerUserId equals u.Id into owners
            from owner in owners.DefaultIfEmpty()
            select new ClubAdminDto
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Subdominio = t.Subdominio,
                Estado = t.Estado.ToString(),
                Profesor = owner == null ? "—" : owner.Nombre + " " + owner.Apellido,
                Alumnos = _db.Alumnos.Count(a => a.TenantId == t.Id && a.Estado == EstadoAlumno.Activo),
                CreadoEl = t.CreadoEl,
            };
        return await query.OrderBy(c => c.Nombre).ToListAsync(ct);
    }

    public Task<Tenant?> ObtenerTenantAsync(Guid id, CancellationToken ct = default) =>
        _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
