using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface IRaquetaRepository
{
    /// <summary>Las raquetas del alumno CON su historial de encordados.</summary>
    Task<IReadOnlyList<Raqueta>> ListarPorAlumnoAsync(Guid alumnoId, CancellationToken ct = default);
    Task<Raqueta?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task AgregarAsync(Raqueta raqueta, CancellationToken ct = default);
    void Eliminar(Raqueta raqueta);

    /// <summary>Un encordado del historial (para editarlo o borrarlo).</summary>
    Task<Encordado?> ObtenerEncordadoAsync(Guid id, CancellationToken ct = default);
    Task AgregarEncordadoAsync(Encordado encordado, CancellationToken ct = default);
    void EliminarEncordado(Encordado encordado);

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
            .Include(r => r.Encordados)
            .Where(r => r.TenantId == TenantId && r.AlumnoId == alumnoId)
            .OrderBy(r => r.CreadoEl)
            .ToListAsync(ct);

    public Task<Raqueta?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Raquetas
            .Include(r => r.Encordados)
            .FirstOrDefaultAsync(r => r.TenantId == TenantId && r.Id == id, ct);

    public async Task AgregarAsync(Raqueta raqueta, CancellationToken ct = default)
    {
        raqueta.TenantId = TenantId;
        _db.Raquetas.Add(raqueta);
        await Task.CompletedTask;
    }

    public void Eliminar(Raqueta raqueta) =>
        _db.Raquetas.Remove(raqueta);

    public Task<Encordado?> ObtenerEncordadoAsync(Guid id, CancellationToken ct = default) =>
        _db.Encordados.FirstOrDefaultAsync(e => e.TenantId == TenantId && e.Id == id, ct);

    public async Task AgregarEncordadoAsync(Encordado encordado, CancellationToken ct = default)
    {
        encordado.TenantId = TenantId;
        _db.Encordados.Add(encordado);
        await Task.CompletedTask;
    }

    public void EliminarEncordado(Encordado encordado) =>
        _db.Encordados.Remove(encordado);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
