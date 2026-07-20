using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface IPublicidadRepository
{
    /// <summary>Los banners del tenant. soloActivas=true para el portal del alumno.</summary>
    Task<IReadOnlyList<Publicidad>> ListarAsync(bool soloActivas, CancellationToken ct = default);
    Task<Publicidad?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task AgregarAsync(Publicidad publicidad, CancellationToken ct = default);
    void Eliminar(Publicidad publicidad);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class PublicidadRepository : IPublicidadRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public PublicidadRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<Publicidad>> ListarAsync(bool soloActivas, CancellationToken ct = default)
    {
        var query = _db.Publicidades.Where(p => p.TenantId == TenantId);
        if (soloActivas) query = query.Where(p => p.Activo);
        return await query.OrderByDescending(p => p.CreadoEl).ToListAsync(ct);
    }

    public Task<Publicidad?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Publicidades.FirstOrDefaultAsync(p => p.TenantId == TenantId && p.Id == id, ct);

    public async Task AgregarAsync(Publicidad publicidad, CancellationToken ct = default)
    {
        publicidad.TenantId = TenantId;
        _db.Publicidades.Add(publicidad);
        await Task.CompletedTask;
    }

    public void Eliminar(Publicidad publicidad) =>
        _db.Publicidades.Remove(publicidad);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
