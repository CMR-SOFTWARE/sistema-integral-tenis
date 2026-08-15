using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>
/// Acceso CROSS-TENANT intencional: las notificaciones son por Usuario (identidad
/// global), no por tenant. No inyecta ITenantActual — mismo criterio que
/// AdminRepository, pero accesible a cualquier usuario autenticado (no solo Admin).
/// </summary>
public interface INotificacionRepository
{
    Task AgregarAsync(Notificacion notificacion, CancellationToken ct = default);
    Task<IReadOnlyList<Notificacion>> MisAsync(Guid userId, int top, CancellationToken ct = default);
    Task<int> ContarNoLeidasAsync(Guid userId, CancellationToken ct = default);
    Task<Notificacion?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task MarcarTodasLeidasAsync(Guid userId, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class NotificacionRepository : INotificacionRepository
{
    private readonly AppDbContext _db;

    public NotificacionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AgregarAsync(Notificacion notificacion, CancellationToken ct = default)
    {
        _db.Notificaciones.Add(notificacion);
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Notificacion>> MisAsync(Guid userId, int top, CancellationToken ct = default) =>
        await _db.Notificaciones
            .Where(n => n.DestinatarioUserId == userId)
            .OrderByDescending(n => n.CreadaEl)
            .Take(top)
            .ToListAsync(ct);

    public Task<int> ContarNoLeidasAsync(Guid userId, CancellationToken ct = default) =>
        _db.Notificaciones.CountAsync(n => n.DestinatarioUserId == userId && !n.Leida, ct);

    public Task<Notificacion?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Notificaciones.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task MarcarTodasLeidasAsync(Guid userId, CancellationToken ct = default) =>
        await _db.Notificaciones
            .Where(n => n.DestinatarioUserId == userId && !n.Leida)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true), ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
