using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Ledger de puntos de dobles — espejo de PuntosMovimientoRepository.</summary>
public interface IPuntosMovimientoDoblesRepository
{
    Task AgregarAsync(PuntosMovimientoDobles movimiento, CancellationToken ct = default);

    /// <summary>Todos los movimientos con fecha >= desde (la ventana de vigencia). El tope de
    /// movimientos más recientes por jugador se aplica en memoria, en el Service.</summary>
    Task<IReadOnlyList<PuntosMovimientoDobles>> ListarVigentesAsync(DateTime desde, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class PuntosMovimientoDoblesRepository : IPuntosMovimientoDoblesRepository
{
    private readonly AppDbContext _db;

    public PuntosMovimientoDoblesRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AgregarAsync(PuntosMovimientoDobles movimiento, CancellationToken ct = default)
    {
        _db.PuntosMovimientosDobles.Add(movimiento);
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<PuntosMovimientoDobles>> ListarVigentesAsync(DateTime desde, CancellationToken ct = default) =>
        await _db.PuntosMovimientosDobles
            .Where(m => m.Fecha >= desde)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync(ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
