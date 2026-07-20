using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface IClaseSueltaRepository
{
    Task AgregarAsync(ClaseSuelta clase, CancellationToken ct = default);
    Task<ClaseSuelta?> ObtenerAsync(Guid id, CancellationToken ct = default);
    /// <summary>Pendientes del profe (con alumno, sede y cargo para ver el pago).</summary>
    Task<IReadOnlyList<ClaseSuelta>> ListarPorEstadoAsync(EstadoClaseSuelta estado, CancellationToken ct = default);
    /// <summary>Mis clases sueltas (portal), la más reciente primero.</summary>
    Task<IReadOnlyList<ClaseSuelta>> ListarPorAlumnoAsync(Guid alumnoId, CancellationToken ct = default);
    Task<int> ContarPorEstadoAsync(EstadoClaseSuelta estado, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class ClaseSueltaRepository : IClaseSueltaRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public ClaseSueltaRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task AgregarAsync(ClaseSuelta clase, CancellationToken ct = default)
    {
        clase.TenantId = TenantId;
        _db.ClasesSueltas.Add(clase);
        await Task.CompletedTask;
    }

    public Task<ClaseSuelta?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.ClasesSueltas
            .Include(c => c.Cargo)
            .FirstOrDefaultAsync(c => c.TenantId == TenantId && c.Id == id, ct);

    public async Task<IReadOnlyList<ClaseSuelta>> ListarPorEstadoAsync(
        EstadoClaseSuelta estado, CancellationToken ct = default) =>
        await _db.ClasesSueltas
            .Include(c => c.Alumno)
            .Include(c => c.Sede)
            .Include(c => c.Cargo)
            .Where(c => c.TenantId == TenantId && c.Estado == estado)
            .OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ClaseSuelta>> ListarPorAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default) =>
        await _db.ClasesSueltas
            .Include(c => c.Sede)
            .Include(c => c.Cancha)
            .Include(c => c.Cargo)
            .Where(c => c.TenantId == TenantId && c.AlumnoId == alumnoId)
            .OrderByDescending(c => c.CreadoEl)
            .ToListAsync(ct);

    public Task<int> ContarPorEstadoAsync(EstadoClaseSuelta estado, CancellationToken ct = default) =>
        _db.ClasesSueltas.CountAsync(c => c.TenantId == TenantId && c.Estado == estado, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
