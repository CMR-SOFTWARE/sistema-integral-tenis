using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface INotaAlumnoRepository
{
    /// <summary>Todas las notas del alumno (el service filtra las compartidas para el portal).</summary>
    Task<IReadOnlyList<NotaAlumno>> ListarPorAlumnoAsync(Guid alumnoId, CancellationToken ct = default);
    Task<NotaAlumno?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task AgregarAsync(NotaAlumno nota, CancellationToken ct = default);
    void Eliminar(NotaAlumno nota);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class NotaAlumnoRepository : INotaAlumnoRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public NotaAlumnoRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<NotaAlumno>> ListarPorAlumnoAsync(Guid alumnoId, CancellationToken ct = default) =>
        await _db.NotasAlumno
            .Where(n => n.TenantId == TenantId && n.AlumnoId == alumnoId)
            .OrderByDescending(n => n.CreadoEl)
            .ToListAsync(ct);

    public Task<NotaAlumno?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.NotasAlumno.FirstOrDefaultAsync(n => n.TenantId == TenantId && n.Id == id, ct);

    public async Task AgregarAsync(NotaAlumno nota, CancellationToken ct = default)
    {
        nota.TenantId = TenantId;
        _db.NotasAlumno.Add(nota);
        await Task.CompletedTask;
    }

    public void Eliminar(NotaAlumno nota) =>
        _db.NotasAlumno.Remove(nota);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
