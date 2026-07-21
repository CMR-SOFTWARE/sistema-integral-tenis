using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface IAvisoRepository
{
    /// <summary>Los avisos del tenant. soloActivos=true para el portal del alumno.</summary>
    Task<IReadOnlyList<Aviso>> ListarAsync(bool soloActivos, CancellationToken ct = default);
    Task<Aviso?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task AgregarAsync(Aviso aviso, CancellationToken ct = default);
    void Eliminar(Aviso aviso);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class AvisoRepository : IAvisoRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public AvisoRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<Aviso>> ListarAsync(bool soloActivos, CancellationToken ct = default)
    {
        var query = _db.Avisos.Where(a => a.TenantId == TenantId);
        if (soloActivos) query = query.Where(a => a.Activo);
        return await query.OrderByDescending(a => a.CreadoEl).ToListAsync(ct);
    }

    public Task<Aviso?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Avisos.FirstOrDefaultAsync(a => a.TenantId == TenantId && a.Id == id, ct);

    public async Task AgregarAsync(Aviso aviso, CancellationToken ct = default)
    {
        aviso.TenantId = TenantId;
        _db.Avisos.Add(aviso);
        await Task.CompletedTask;
    }

    public void Eliminar(Aviso aviso) =>
        _db.Avisos.Remove(aviso);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
