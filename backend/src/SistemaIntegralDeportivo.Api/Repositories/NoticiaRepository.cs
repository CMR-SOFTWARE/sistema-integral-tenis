using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface INoticiaRepository
{
    /// <summary>Las noticias del tenant. soloActivas=true para el portal del alumno.</summary>
    Task<IReadOnlyList<Noticia>> ListarAsync(bool soloActivas, CancellationToken ct = default);
    Task<Noticia?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task AgregarAsync(Noticia noticia, CancellationToken ct = default);
    void Eliminar(Noticia noticia);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class NoticiaRepository : INoticiaRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public NoticiaRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<Noticia>> ListarAsync(bool soloActivas, CancellationToken ct = default)
    {
        var query = _db.Noticias.Where(n => n.TenantId == TenantId);
        if (soloActivas) query = query.Where(n => n.Activo);
        // Las destacadas primero: es el orden con el que el portal las muestra, y también
        // el que le sirve al profe para ver qué tiene arriba de todo en el Inicio.
        return await query
            .OrderByDescending(n => n.Importante)
            .ThenByDescending(n => n.CreadoEl)
            .ToListAsync(ct);
    }

    public Task<Noticia?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Noticias.FirstOrDefaultAsync(n => n.TenantId == TenantId && n.Id == id, ct);

    public async Task AgregarAsync(Noticia noticia, CancellationToken ct = default)
    {
        noticia.TenantId = TenantId;
        _db.Noticias.Add(noticia);
        await Task.CompletedTask;
    }

    public void Eliminar(Noticia noticia) =>
        _db.Noticias.Remove(noticia);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
