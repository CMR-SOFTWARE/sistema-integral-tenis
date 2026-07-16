using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class SedeRepository : ISedeRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;

    // El tenant sale del token o del override del portal (ADR-0010)
    private Guid TenantId => _tenantActual.TenantId;

    public SedeRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
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

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
