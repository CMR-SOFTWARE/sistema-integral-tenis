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
}
