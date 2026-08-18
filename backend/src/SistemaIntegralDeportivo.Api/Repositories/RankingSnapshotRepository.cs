using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Acceso CROSS-TENANT intencional (ranking global de plataforma).</summary>
public interface IRankingSnapshotRepository
{
    /// <summary>¿Ya se generó un cierre oficial hoy (cualquier modalidad/scope)? Evita
    /// duplicar el cierre si el contenedor se reinicia el mismo día.</summary>
    Task<bool> YaCerroHoyAsync(CancellationToken ct = default);

    Task AgregarRangoAsync(IEnumerable<RankingSnapshot> snapshots, CancellationToken ct = default);

    /// <summary>El snapshot oficial VIGENTE (la última FechaCorte) para una modalidad+scope.</summary>
    Task<IReadOnlyList<RankingSnapshot>> ObtenerUltimoOficialAsync(
        ModalidadRanking modalidad, ScopeRanking scope, string? scopeValor, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class RankingSnapshotRepository : IRankingSnapshotRepository
{
    private readonly AppDbContext _db;

    public RankingSnapshotRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> YaCerroHoyAsync(CancellationToken ct = default)
    {
        var hoy = DateTime.UtcNow.Date;
        return _db.RankingSnapshots.AnyAsync(s => s.FechaCorte >= hoy && s.FechaCorte < hoy.AddDays(1), ct);
    }

    public async Task AgregarRangoAsync(IEnumerable<RankingSnapshot> snapshots, CancellationToken ct = default)
    {
        _db.RankingSnapshots.AddRange(snapshots);
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<RankingSnapshot>> ObtenerUltimoOficialAsync(
        ModalidadRanking modalidad, ScopeRanking scope, string? scopeValor, CancellationToken ct = default)
    {
        var query = _db.RankingSnapshots.AsNoTracking()
            .Where(s => s.Modalidad == modalidad && s.Scope == scope && s.ScopeValor == scopeValor);

        var ultimaFecha = await query.MaxAsync(s => (DateTime?)s.FechaCorte, ct);
        if (ultimaFecha is null) return [];

        return await query.Where(s => s.FechaCorte == ultimaFecha).OrderBy(s => s.Posicion).ToListAsync(ct);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
