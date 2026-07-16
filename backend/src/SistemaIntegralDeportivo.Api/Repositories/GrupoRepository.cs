using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class GrupoRepository : IGrupoRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;

    // El tenant sale del token o del override del portal (ADR-0010)
    private Guid TenantId => _tenantActual.TenantId;

    public GrupoRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public Task<Grupo?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Grupos
            .Include(g => g.Alumnos)          // membresías
                .ThenInclude(m => m.Alumno)   // con su alumno (para los chips)
            .FirstOrDefaultAsync(g => g.TenantId == TenantId && g.Id == id, ct);

    public async Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken ct = default) =>
        await _db.Grupos
            .AsNoTracking()
            .Include(g => g.Alumnos)
                .ThenInclude(m => m.Alumno)
            .Where(g => g.TenantId == TenantId)
            .OrderByDescending(g => g.Activo)
            .ThenBy(g => g.Nombre)
            .ToListAsync(ct);

    public async Task<Grupo> AgregarAsync(Grupo grupo, CancellationToken ct = default)
    {
        grupo.TenantId = TenantId;
        _db.Grupos.Add(grupo);
        await _db.SaveChangesAsync(ct);
        return grupo;
    }

    public Task<AlumnoGrupo?> ObtenerMembresiaAsync(Guid grupoId, Guid alumnoId, CancellationToken ct = default) =>
        _db.AlumnoGrupos
            .FirstOrDefaultAsync(m => m.GrupoId == grupoId && m.AlumnoId == alumnoId, ct);

    public Task<int> ContarMiembrosActivosAsync(Guid grupoId, CancellationToken ct = default) =>
        _db.AlumnoGrupos.CountAsync(m => m.GrupoId == grupoId && m.FechaBaja == null, ct);

    public async Task<IReadOnlyList<AlumnoGrupo>> ListarMembresiasActivasDeAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default) =>
        // TRACKEADO: la baja del alumno les pone FechaBaja
        await _db.AlumnoGrupos
            .Where(m => m.AlumnoId == alumnoId && m.FechaBaja == null)
            .ToListAsync(ct);

    public async Task AgregarMembresiaAsync(AlumnoGrupo membresia, CancellationToken ct = default)
    {
        _db.AlumnoGrupos.Add(membresia);
        await Task.CompletedTask; // se persiste con GuardarCambiosAsync (misma transacción)
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
