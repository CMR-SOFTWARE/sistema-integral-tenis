using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class AlumnoRepository : IAlumnoRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;

    // El tenant sale del token o del override del portal (ADR-0010)
    private Guid TenantId => _tenantActual.TenantId;

    public AlumnoRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
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

    // ── Auth / portal: estos dos son GLOBALES a propósito (no scopean por
    //    tenant): la identidad cruza negocios y el reclamo busca la ficha de
    //    la persona en TODOS los tenants (ADR-0007) ──

    public async Task<IReadOnlyList<Alumno>> BuscarReclamablesAsync(
        string? dni, string? telefono, CancellationToken ct = default) =>
        await _db.Alumnos
            .Include(a => a.Tenant)
            .Where(a => a.UserId == null &&
                ((dni != null && dni != "" && a.Dni == dni) ||
                 (telefono != null && telefono != "" && a.Telefono == telefono)))
            .ToListAsync(ct);

    public Task<Alumno?> ObtenerPorUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.Alumnos
            .Include(a => a.Tenant)
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

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
