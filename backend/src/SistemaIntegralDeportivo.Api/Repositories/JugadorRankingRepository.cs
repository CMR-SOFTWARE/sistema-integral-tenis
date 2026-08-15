using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Una fila del leaderboard, ya con el nombre resuelto (join contra Identity).</summary>
public record FilaRanking(JugadorRanking Jugador, string Nombre, string Apellido);

/// <summary>
/// Acceso CROSS-TENANT intencional (ranking global de plataforma, ADR pendiente):
/// no inyecta ITenantActual ni filtra por tenant — a diferencia de AdminRepository
/// (gateado por policy Admin), acá cualquier jugador autenticado puede leer/operar,
/// mismo criterio que PortalController.
/// </summary>
public interface IJugadorRankingRepository
{
    Task<JugadorRanking?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    Task<JugadorRanking?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<FilaRanking?> ObtenerConNombreAsync(Guid id, CancellationToken ct = default);
    Task AgregarAsync(JugadorRanking jugador, CancellationToken ct = default);

    /// <summary>Activos, ordenados por PosicionProvisional (la verdad ya recalculada) — nulls al final.</summary>
    Task<IReadOnlyList<FilaRanking>> ListarActivosOrdenadosAsync(CancellationToken ct = default);

    /// <summary>Todos los activos, entidades TRACKEADAS — para el recálculo global (los muta y guarda).</summary>
    Task<IReadOnlyList<JugadorRanking>> ListarTodosActivosAsync(CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class JugadorRankingRepository : IJugadorRankingRepository
{
    private readonly AppDbContext _db;

    public JugadorRankingRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<JugadorRanking?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        _db.JugadoresRanking.FirstOrDefaultAsync(j => j.UsuarioId == usuarioId, ct);

    public Task<JugadorRanking?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.JugadoresRanking.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task<FilaRanking?> ObtenerConNombreAsync(Guid id, CancellationToken ct = default)
    {
        var query =
            from j in _db.JugadoresRanking.AsNoTracking()
            join u in _db.Users.AsNoTracking() on j.UsuarioId equals u.Id
            where j.Id == id
            select new { Jugador = j, u.Nombre, u.Apellido };
        var fila = await query.FirstOrDefaultAsync(ct);
        return fila is null ? null : new FilaRanking(fila.Jugador, fila.Nombre, fila.Apellido);
    }

    public async Task AgregarAsync(JugadorRanking jugador, CancellationToken ct = default)
    {
        _db.JugadoresRanking.Add(jugador);
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<FilaRanking>> ListarActivosOrdenadosAsync(CancellationToken ct = default)
    {
        var query =
            from j in _db.JugadoresRanking.AsNoTracking()
            join u in _db.Users.AsNoTracking() on j.UsuarioId equals u.Id
            where j.Activo
            orderby j.PosicionProvisional == null, j.PosicionProvisional ascending,
                    j.PuntosProvisionales descending, j.OrdenInscripcion ascending
            select new { Jugador = j, u.Nombre, u.Apellido };

        var filas = await query.ToListAsync(ct);
        return filas.Select(f => new FilaRanking(f.Jugador, f.Nombre, f.Apellido)).ToList();
    }

    public async Task<IReadOnlyList<JugadorRanking>> ListarTodosActivosAsync(CancellationToken ct = default) =>
        await _db.JugadoresRanking.Where(j => j.Activo).ToListAsync(ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
