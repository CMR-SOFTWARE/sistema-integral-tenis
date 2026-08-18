using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Acceso CROSS-TENANT intencional (ranking global de plataforma).</summary>
public interface IJuegoRevisionRepository
{
    Task AgregarAsync(JuegoRevision revision, CancellationToken ct = default);
    Task<JuegoRevision?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <summary>¿Ya hay una revisión Pendiente para este partido (singles o dobles)? Una por vez.</summary>
    Task<bool> ExistePendienteAsync(Guid? juegoPendienteId, Guid? juegoDoblesPendienteId, CancellationToken ct = default);

    /// <summary>Todas las Pendientes — para el panel de moderación de Plataforma.</summary>
    Task<IReadOnlyList<JuegoRevision>> ListarPendientesAsync(CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class JuegoRevisionRepository : IJuegoRevisionRepository
{
    private readonly AppDbContext _db;

    public JuegoRevisionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AgregarAsync(JuegoRevision revision, CancellationToken ct = default)
    {
        _db.JuegosRevision.Add(revision);
        await Task.CompletedTask;
    }

    public Task<JuegoRevision?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.JuegosRevision.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<bool> ExistePendienteAsync(Guid? juegoPendienteId, Guid? juegoDoblesPendienteId, CancellationToken ct = default) =>
        _db.JuegosRevision.AnyAsync(r =>
            r.Estado == EstadoJuegoRevision.Pendiente &&
            r.JuegoPendienteId == juegoPendienteId &&
            r.JuegoDoblesPendienteId == juegoDoblesPendienteId, ct);

    public async Task<IReadOnlyList<JuegoRevision>> ListarPendientesAsync(CancellationToken ct = default) =>
        await _db.JuegosRevision
            .Where(r => r.Estado == EstadoJuegoRevision.Pendiente)
            .OrderBy(r => r.CreadoEl)
            .ToListAsync(ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
