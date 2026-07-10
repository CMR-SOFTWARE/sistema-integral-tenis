using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class CargoRepository : ICargoRepository
{
    private readonly AppDbContext _db;

    private static Guid TenantId => AppDbContext.TenantDemoId;

    public CargoRepository(AppDbContext db)
    {
        _db = db;
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
