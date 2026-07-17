using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface IRaquetaRepository
{
    Task<IReadOnlyList<Raqueta>> ListarPorAlumnoAsync(Guid alumnoId, CancellationToken ct = default);
    Task<Raqueta?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task AgregarAsync(Raqueta raqueta, CancellationToken ct = default);
    void Eliminar(Raqueta raqueta);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class RaquetaRepository : IRaquetaRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public RaquetaRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<Raqueta>> ListarPorAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default) =>
        await _db.Raquetas
            .Where(r => r.TenantId == TenantId && r.AlumnoId == alumnoId)
            .OrderBy(r => r.CreadoEl)
            .ToListAsync(ct);

    public Task<Raqueta?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Raquetas.FirstOrDefaultAsync(r => r.TenantId == TenantId && r.Id == id, ct);

    public async Task AgregarAsync(Raqueta raqueta, CancellationToken ct = default)
    {
        raqueta.TenantId = TenantId;
        _db.Raquetas.Add(raqueta);
        await Task.CompletedTask;
    }

    public void Eliminar(Raqueta raqueta) =>
        _db.Raquetas.Remove(raqueta);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
