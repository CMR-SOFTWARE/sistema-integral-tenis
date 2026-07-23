using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface IMembresiaTenantRepository
{
    // ── Scopeadas al tenant actual (el ABM del dueño) ──
    Task<IReadOnlyList<(MembresiaTenant Membresia, Usuario Usuario)>> ListarConUsuarioAsync(CancellationToken ct = default);
    Task<MembresiaTenant?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<MembresiaTenant?> ObtenerPorUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AgregarAsync(MembresiaTenant membresia, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);

    // ── NO scopeadas: corren sin tenant en contexto (login, búsqueda por celular) ──
    Task<Usuario?> BuscarUsuarioPorTelefonoAsync(string telefono, CancellationToken ct = default);
    Task<Usuario?> ObtenerUsuarioAsync(Guid userId, CancellationToken ct = default);
    /// <summary>La membresía staff ACTIVA del usuario (para resolver su tenant al loguearse).</summary>
    Task<MembresiaTenant?> ObtenerActivaPorUserIdAsync(Guid userId, CancellationToken ct = default);
}

public class MembresiaTenantRepository : IMembresiaTenantRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public MembresiaTenantRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<(MembresiaTenant, Usuario)>> ListarConUsuarioAsync(CancellationToken ct = default)
    {
        var query =
            from m in _db.MembresiasTenant.AsNoTracking()
            join u in _db.Users.AsNoTracking() on m.UserId equals u.Id
            where m.TenantId == TenantId
            orderby u.Nombre, u.Apellido
            select new { Membresia = m, Usuario = u };

        var lista = await query.ToListAsync(ct);
        return lista.Select(x => (x.Membresia, x.Usuario)).ToList();
    }

    public Task<MembresiaTenant?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.MembresiasTenant.FirstOrDefaultAsync(m => m.TenantId == TenantId && m.Id == id, ct);

    public Task<MembresiaTenant?> ObtenerPorUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.MembresiasTenant.FirstOrDefaultAsync(m => m.TenantId == TenantId && m.UserId == userId, ct);

    public async Task AgregarAsync(MembresiaTenant membresia, CancellationToken ct = default)
    {
        membresia.TenantId = TenantId;
        _db.MembresiasTenant.Add(membresia);
        await Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);

    public Task<Usuario?> BuscarUsuarioPorTelefonoAsync(string telefono, CancellationToken ct = default)
    {
        // El UserName es el celular (solo dígitos); Identity lo guarda normalizado (dígitos = dígitos)
        var usuario = new string((telefono ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        return _db.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == usuario, ct);
    }

    public Task<Usuario?> ObtenerUsuarioAsync(Guid userId, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

    public Task<MembresiaTenant?> ObtenerActivaPorUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.MembresiasTenant.FirstOrDefaultAsync(m => m.UserId == userId && m.Activo, ct);
}
