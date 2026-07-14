using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class CargoRepository : ICargoRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;

    // El tenant sale del token o del override del portal (ADR-0010)
    private Guid TenantId => _tenantActual.TenantId;

    public CargoRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<Cargo>> ListarDelMesAsync(int anio, int mes, CancellationToken ct = default)
    {
        var primerDia = new DateOnly(anio, mes, 1);
        var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

        return await _db.Cargos
            .Include(c => c.Alumno)
            .Where(c => c.TenantId == TenantId && c.Fecha >= primerDia && c.Fecha <= ultimoDia)
            .OrderBy(c => c.Fecha)
            .ToListAsync(ct);
    }

    public Task<Cargo?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Cargos.FirstOrDefaultAsync(c => c.TenantId == TenantId && c.Id == id, ct);

    public async Task<IReadOnlyList<Cargo>> ListarPorTurnosAsync(
        IReadOnlyCollection<Guid> turnoIds, CancellationToken ct = default) =>
        await _db.Cargos
            .Where(c => c.TenantId == TenantId && c.TurnoId != null && turnoIds.Contains(c.TurnoId.Value))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Cargo>> ListarImpagosAsync(
        IReadOnlyCollection<Guid> alumnoIds, CancellationToken ct = default) =>
        await _db.Cargos
            .Where(c => c.TenantId == TenantId && c.PagadoEl == null && alumnoIds.Contains(c.AlumnoId))
            .ToListAsync(ct);

    public async Task<Dictionary<(int Anio, int Mes), decimal>> SumarPagadosPorMesAsync(
        DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        var porMes = await _db.Cargos
            .Where(c => c.TenantId == TenantId && c.PagadoEl != null &&
                        c.Fecha >= desde && c.Fecha <= hasta)
            .GroupBy(c => new { c.Fecha.Year, c.Fecha.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(c => c.Monto) })
            .ToListAsync(ct);

        return porMes.ToDictionary(x => (x.Year, x.Month), x => x.Total);
    }

    public async Task AgregarAsync(Cargo cargo, CancellationToken ct = default)
    {
        cargo.TenantId = TenantId;
        _db.Cargos.Add(cargo);
        await Task.CompletedTask; // se persiste con GuardarCambiosAsync (misma transacción)
    }

    public void Eliminar(Cargo cargo) =>
        _db.Cargos.Remove(cargo);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
