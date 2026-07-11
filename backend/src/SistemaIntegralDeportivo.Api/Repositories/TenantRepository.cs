using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _db;

    public TenantRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Tenant> ObtenerActualAsync(CancellationToken ct = default) =>
        await _db.Tenants.FirstAsync(t => t.Id == AppDbContext.TenantDemoId, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);

    public Task<bool> EsDuenioAsync(Guid userId, CancellationToken ct = default) =>
        _db.Tenants.AnyAsync(t => t.OwnerUserId == userId, ct);
}
