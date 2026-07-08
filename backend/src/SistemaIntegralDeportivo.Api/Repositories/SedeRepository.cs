using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class SedeRepository : ISedeRepository
{
    private readonly AppDbContext _db;

    private static Guid TenantId => AppDbContext.TenantDemoId;

    public SedeRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Sede>> ListarAsync(CancellationToken ct = default) =>
        await _db.Sedes
            .AsNoTracking()
            .Include(s => s.Canchas)
            .Where(s => s.TenantId == TenantId)
            .OrderBy(s => s.Nombre)
            .ToListAsync(ct);

    public Task<Sede?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Sedes
            .Include(s => s.Canchas)
            .FirstOrDefaultAsync(s => s.TenantId == TenantId && s.Id == id, ct);

    public async Task<Sede> AgregarAsync(Sede sede, CancellationToken ct = default)
    {
        sede.TenantId = TenantId;
        _db.Sedes.Add(sede);
        await _db.SaveChangesAsync(ct);
        return sede;
    }

    public async Task<Cancha> AgregarCanchaAsync(Cancha cancha, CancellationToken ct = default)
    {
        _db.Canchas.Add(cancha);
        await _db.SaveChangesAsync(ct);
        return cancha;
    }
}
