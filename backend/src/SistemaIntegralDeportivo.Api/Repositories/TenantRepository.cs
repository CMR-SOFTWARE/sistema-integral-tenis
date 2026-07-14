using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;

    public TenantRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<Tenant> ObtenerActualAsync(CancellationToken ct = default) =>
        await _db.Tenants.FirstAsync(t => t.Id == _tenantActual.TenantId, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);

    public Task<Tenant?> ObtenerPorOwnerAsync(Guid userId, CancellationToken ct = default) =>
        _db.Tenants.FirstOrDefaultAsync(t => t.OwnerUserId == userId, ct);

    public Task<bool> ExisteSubdominioAsync(string subdominio, CancellationToken ct = default) =>
        _db.Tenants.AnyAsync(t => t.Subdominio == subdominio, ct);

    public Task AgregarAsync(Tenant tenant, CancellationToken ct = default)
    {
        _db.Tenants.Add(tenant);
        return Task.CompletedTask; // se persiste con GuardarCambiosAsync
    }

    public async Task<IReadOnlyList<(Tenant Tenant, string Profesor)>> ListarActivosAsync(
        string? buscar, CancellationToken ct = default)
    {
        // Join con Identity: el nombre del profe sale del usuario dueño
        var query =
            from t in _db.Tenants.AsNoTracking()
            join u in _db.Users.AsNoTracking() on t.OwnerUserId equals u.Id
            where t.Tipo == TipoTenant.Profesor && t.Estado == EstadoTenant.Activo
            select new { Tenant = t, Profesor = u.Nombre + " " + u.Apellido };

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var b = buscar.Trim().ToLower();
            query = query.Where(x =>
                x.Tenant.Nombre.ToLower().Contains(b) || x.Profesor.ToLower().Contains(b));
        }

        var lista = await query.OrderBy(x => x.Tenant.Nombre).ToListAsync(ct);
        return lista.Select(x => (x.Tenant, x.Profesor)).ToList();
    }
}
