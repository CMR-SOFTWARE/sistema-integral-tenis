using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Acceso CROSS-TENANT intencional (ranking global): no inyecta ITenantActual.</summary>
public interface IPuntosMovimientoRepository
{
    Task AgregarAsync(PuntosMovimiento movimiento, CancellationToken ct = default);

    /// <summary>Movimientos posteriores a `desde`, más reciente primero — la ventana de
    /// vigencia. El tope de "72 más recientes por jugador" se aplica en memoria
    /// (Service): agrupar+Take por grupo no traduce bien a SQL vía EF.</summary>
    Task<IReadOnlyList<PuntosMovimiento>> ListarVigentesAsync(DateTime desde, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class PuntosMovimientoRepository : IPuntosMovimientoRepository
{
    private readonly AppDbContext _db;

    public PuntosMovimientoRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AgregarAsync(PuntosMovimiento movimiento, CancellationToken ct = default)
    {
        _db.PuntosMovimientos.Add(movimiento);
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<PuntosMovimiento>> ListarVigentesAsync(DateTime desde, CancellationToken ct = default) =>
        await _db.PuntosMovimientos
            .Where(m => m.Fecha >= desde)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync(ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
