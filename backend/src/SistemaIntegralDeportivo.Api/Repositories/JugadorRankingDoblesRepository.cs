using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Una fila del leaderboard de dobles, con el nombre resuelto vía JugadorRanking → Usuario.</summary>
public record FilaRankingDobles(JugadorRankingDobles Jugador, Guid JugadorRankingId, string Nombre, string Apellido);

/// <summary>
/// Acceso CROSS-TENANT intencional (ranking global) — mismo criterio que
/// JugadorRankingRepository. El nombre se resuelve con un doble join:
/// JugadorRankingDobles → JugadorRanking → Usuario.
/// </summary>
public interface IJugadorRankingDoblesRepository
{
    Task<JugadorRankingDobles?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<JugadorRankingDobles?> ObtenerPorJugadorRankingIdAsync(Guid jugadorRankingId, CancellationToken ct = default);
    Task<FilaRankingDobles?> ObtenerConNombreAsync(Guid id, CancellationToken ct = default);
    Task AgregarAsync(JugadorRankingDobles jugador, CancellationToken ct = default);

    /// <summary>Activos, ordenados por PosicionProvisional (la verdad ya recalculada) — nulls al final.</summary>
    Task<IReadOnlyList<FilaRankingDobles>> ListarActivosOrdenadosAsync(CancellationToken ct = default);

    /// <summary>Todos los activos, entidades TRACKEADAS — para el recálculo global.</summary>
    Task<IReadOnlyList<JugadorRankingDobles>> ListarTodosActivosAsync(CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class JugadorRankingDoblesRepository : IJugadorRankingDoblesRepository
{
    private readonly AppDbContext _db;

    public JugadorRankingDoblesRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<JugadorRankingDobles?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.JugadoresRankingDobles.FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<JugadorRankingDobles?> ObtenerPorJugadorRankingIdAsync(Guid jugadorRankingId, CancellationToken ct = default) =>
        _db.JugadoresRankingDobles.FirstOrDefaultAsync(j => j.JugadorRankingId == jugadorRankingId, ct);

    public async Task<FilaRankingDobles?> ObtenerConNombreAsync(Guid id, CancellationToken ct = default)
    {
        var query =
            from d in _db.JugadoresRankingDobles.AsNoTracking()
            join j in _db.JugadoresRanking.AsNoTracking() on d.JugadorRankingId equals j.Id
            join u in _db.Users.AsNoTracking() on j.UsuarioId equals u.Id
            where d.Id == id
            select new { Dobles = d, JugadorRankingId = j.Id, u.Nombre, u.Apellido };
        var fila = await query.FirstOrDefaultAsync(ct);
        return fila is null ? null : new FilaRankingDobles(fila.Dobles, fila.JugadorRankingId, fila.Nombre, fila.Apellido);
    }

    public async Task AgregarAsync(JugadorRankingDobles jugador, CancellationToken ct = default)
    {
        _db.JugadoresRankingDobles.Add(jugador);
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<FilaRankingDobles>> ListarActivosOrdenadosAsync(CancellationToken ct = default)
    {
        var query =
            from d in _db.JugadoresRankingDobles.AsNoTracking()
            join j in _db.JugadoresRanking.AsNoTracking() on d.JugadorRankingId equals j.Id
            join u in _db.Users.AsNoTracking() on j.UsuarioId equals u.Id
            where d.Activo
            orderby d.PosicionProvisional == null, d.PosicionProvisional ascending,
                    d.PuntosProvisionales descending, d.OrdenInscripcion ascending
            select new { Dobles = d, JugadorRankingId = j.Id, u.Nombre, u.Apellido };

        var filas = await query.ToListAsync(ct);
        return filas.Select(f => new FilaRankingDobles(f.Dobles, f.JugadorRankingId, f.Nombre, f.Apellido)).ToList();
    }

    public async Task<IReadOnlyList<JugadorRankingDobles>> ListarTodosActivosAsync(CancellationToken ct = default) =>
        await _db.JugadoresRankingDobles.Where(j => j.Activo).ToListAsync(ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
