using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class BloqueoRepository : IBloqueoRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;

    // El tenant sale del token o del override del portal (ADR-0010)
    private Guid TenantId => _tenantActual.TenantId;

    public BloqueoRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<Bloqueo>> ListarAsync(CancellationToken ct = default) =>
        await _db.Bloqueos
            .AsNoTracking()
            .Include(b => b.Cancha)
            .Where(b => b.TenantId == TenantId)
            .OrderBy(b => b.Tipo).ThenBy(b => b.Fecha).ThenBy(b => b.Dia).ThenBy(b => b.HoraInicio)
            .ToListAsync(ct);

    public Task<Bloqueo?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Bloqueos.FirstOrDefaultAsync(b => b.TenantId == TenantId && b.Id == id, ct);

    public Task AgregarAsync(Bloqueo bloqueo, CancellationToken ct = default)
    {
        bloqueo.TenantId = TenantId;
        _db.Bloqueos.Add(bloqueo);
        return Task.CompletedTask; // se persiste con GuardarCambiosAsync
    }

    public void Eliminar(Bloqueo bloqueo) =>
        _db.Bloqueos.Remove(bloqueo);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
