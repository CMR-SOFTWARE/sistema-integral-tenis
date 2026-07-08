using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class AlumnoRepository : IAlumnoRepository
{
    private readonly AppDbContext _db;

    // Mientras no haya auth, todo opera sobre el tenant demo (ADR-0004).
    // Cuando exista login, este valor saldrá del contexto del request.
    private static Guid TenantId => AppDbContext.TenantDemoId;

    public AlumnoRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExisteDniAsync(string dni, CancellationToken ct = default) =>
        _db.Alumnos.AnyAsync(a => a.TenantId == TenantId && a.Dni == dni, ct);

    public async Task<Alumno> AgregarAsync(Alumno alumno, CancellationToken ct = default)
    {
        alumno.TenantId = TenantId;

        if (alumno.Tutor is not null)
        {
            // Si el tutor ya existe en el tenant (mismo DNI: caso hermanos),
            // se reutiliza en vez de duplicarlo (índice único TenantId+Dni).
            var existente = await _db.Tutores.FirstOrDefaultAsync(
                t => t.TenantId == TenantId && t.Dni == alumno.Tutor.Dni, ct);

            if (existente is not null)
            {
                alumno.Tutor = null;
                alumno.TutorId = existente.Id;
            }
            else
            {
                alumno.Tutor.TenantId = TenantId;
            }
        }

        _db.Alumnos.Add(alumno);
        await _db.SaveChangesAsync(ct);
        return alumno;
    }

    public async Task<IReadOnlyList<Alumno>> ListarAsync(
        CategoriaAlumno? categoria, EstadoAlumno? estado, CancellationToken ct = default)
    {
        var query = _db.Alumnos.AsNoTracking().Where(a => a.TenantId == TenantId);

        if (categoria is not null) query = query.Where(a => a.Categoria == categoria);
        if (estado is not null) query = query.Where(a => a.Estado == estado);

        return await query
            .OrderBy(a => a.Apellido).ThenBy(a => a.Nombre)
            .ToListAsync(ct);
    }

    public Task<Alumno?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Alumnos.FirstOrDefaultAsync(a => a.TenantId == TenantId && a.Id == id, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);

    public Task<int> ContarPorEstadoAsync(EstadoAlumno estado, CancellationToken ct = default) =>
        _db.Alumnos.CountAsync(a => a.TenantId == TenantId && a.Estado == estado, ct);

    public Task<int> ContarNuevosDesdeAsync(DateTime desde, CancellationToken ct = default) =>
        _db.Alumnos.CountAsync(a => a.TenantId == TenantId && a.CreadoEl >= desde, ct);

    public async Task<decimal> SumarArancelActivosAsync(CancellationToken ct = default) =>
        await _db.Alumnos
            .Where(a => a.TenantId == TenantId && a.Estado == EstadoAlumno.Activo)
            .SumAsync(a => a.Arancel ?? 0, ct);

    public async Task<Dictionary<CategoriaAlumno, int>> ContarPorCategoriaAsync(CancellationToken ct = default) =>
        await _db.Alumnos
            .Where(a => a.TenantId == TenantId && a.Estado != EstadoAlumno.Inactivo)
            .GroupBy(a => a.Categoria)
            .Select(g => new { Categoria = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Categoria, x => x.Cantidad, ct);
}
