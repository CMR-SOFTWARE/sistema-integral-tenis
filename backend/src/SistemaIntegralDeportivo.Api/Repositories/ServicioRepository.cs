using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface IServicioRepository
{
    /// <summary>El catálogo del profe. soloActivos=true para el portal del alumno.</summary>
    Task<IReadOnlyList<Servicio>> ListarAsync(bool soloActivos, CancellationToken ct = default);
    Task<Servicio?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task AgregarAsync(Servicio servicio, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class ServicioRepository : IServicioRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public ServicioRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<Servicio>> ListarAsync(bool soloActivos, CancellationToken ct = default)
    {
        var query = _db.Servicios.Where(s => s.TenantId == TenantId);
        if (soloActivos) query = query.Where(s => s.Activo);
        return await query.OrderBy(s => s.Nombre).ToListAsync(ct);
    }

    public Task<Servicio?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Servicios.FirstOrDefaultAsync(s => s.TenantId == TenantId && s.Id == id, ct);

    public async Task AgregarAsync(Servicio servicio, CancellationToken ct = default)
    {
        servicio.TenantId = TenantId;
        _db.Servicios.Add(servicio);
        await Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
