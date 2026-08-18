using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>
/// Acceso CROSS-TENANT: el único repo que NO scopea por ITenantActual (consulta
/// todos los clubes). Solo lo usa el AdminController, gateado por policy Admin.
/// </summary>
public interface IAdminRepository
{
    Task<IReadOnlyDictionary<EstadoTenant, int>> ContarClubesPorEstadoAsync(CancellationToken ct = default);
    Task<int> ContarStaffActivosAsync(CancellationToken ct = default);

    /// <summary>Todas las PERSONAS de la plataforma (dueños + staff + alumnos + admins),
    /// sin duplicar a quien cumple varios roles.</summary>
    Task<int> ContarUsuariosAsync(CancellationToken ct = default);
    Task<decimal> IngresosDelMesAsync(int anio, int mes, CancellationToken ct = default);
    Task<int> ContarClubesNuevosAsync(DateTime desde, CancellationToken ct = default);
    Task<int> ContarAlumnosNuevosAsync(DateTime desde, CancellationToken ct = default);
    Task<IReadOnlyList<ClubAdminDto>> ListarClubesAsync(CancellationToken ct = default);
    Task<Tenant?> ObtenerTenantAsync(Guid id, CancellationToken ct = default);

    /// <summary>Los UsuarioId de los admins de plataforma — para notificarles pedidos de revisión.</summary>
    Task<IReadOnlyList<Guid>> ListarUsuarioIdsAdminsAsync(CancellationToken ct = default);

    /// <summary>El padrón de personas (Bloque 6, pedido 11): AspNetUsers con sus roles
    /// por tenant (dueño/staff/alumno), NO la tabla Alumnos.</summary>
    Task<IReadOnlyList<PersonaAdminDto>> ListarPersonasAsync(CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _db;

    public AdminRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<EstadoTenant, int>> ContarClubesPorEstadoAsync(CancellationToken ct = default)
    {
        var filas = await _db.Tenants.AsNoTracking()
            .GroupBy(t => t.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync(ct);
        return filas.ToDictionary(x => x.Estado, x => x.Cantidad);
    }

    public Task<int> ContarStaffActivosAsync(CancellationToken ct = default) =>
        _db.MembresiasTenant.CountAsync(m => m.Activo, ct);

    public async Task<int> ContarUsuariosAsync(CancellationToken ct = default) =>
        (await ListarPersonaIdsAsync(ct)).Count;

    /// <summary>Los userId con algún rol en la plataforma (dueño, staff, alumno o admin),
    /// sin duplicados. Compartido por el conteo y por <see cref="ListarPersonasAsync"/>.</summary>
    private async Task<List<Guid>> ListarPersonaIdsAsync(CancellationToken ct)
    {
        var duenos = await _db.Tenants.AsNoTracking()
            .Where(t => t.OwnerUserId != null).Select(t => t.OwnerUserId!.Value).ToListAsync(ct);
        var staff = await _db.MembresiasTenant.AsNoTracking().Select(m => m.UserId).ToListAsync(ct);
        var alumnos = await _db.Alumnos.AsNoTracking()
            .Where(a => a.UserId != null).Select(a => a.UserId!.Value).ToListAsync(ct);
        var admins = await _db.Users.AsNoTracking()
            .Where(u => u.EsAdminPlataforma).Select(u => u.Id).ToListAsync(ct);

        return duenos.Concat(staff).Concat(alumnos).Concat(admins).Distinct().ToList();
    }

    public async Task<decimal> IngresosDelMesAsync(int anio, int mes, CancellationToken ct = default)
    {
        var desde = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = desde.AddMonths(1);
        // Pagos CONFIRMADOS (PagadoEl) del mes; ignora ajustes negativos.
        return await _db.Cargos.AsNoTracking()
            .Where(c => c.PagadoEl >= desde && c.PagadoEl < hasta && c.Monto > 0)
            .SumAsync(c => (decimal?)c.Monto, ct) ?? 0m;
    }

    public Task<int> ContarClubesNuevosAsync(DateTime desde, CancellationToken ct = default) =>
        _db.Tenants.CountAsync(t => t.CreadoEl >= desde, ct);

    public Task<int> ContarAlumnosNuevosAsync(DateTime desde, CancellationToken ct = default) =>
        _db.Alumnos.CountAsync(a => a.CreadoEl >= desde, ct);

    public async Task<IReadOnlyList<ClubAdminDto>> ListarClubesAsync(CancellationToken ct = default)
    {
        var query =
            from t in _db.Tenants.AsNoTracking()
            join u in _db.Users.AsNoTracking() on t.OwnerUserId equals u.Id into owners
            from owner in owners.DefaultIfEmpty()
            select new ClubAdminDto
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Subdominio = t.Subdominio,
                Estado = t.Estado.ToString(),
                Profesor = owner == null ? "—" : owner.Nombre + " " + owner.Apellido,
                Alumnos = _db.Alumnos.Count(a => a.TenantId == t.Id && a.Estado == EstadoAlumno.Activo),
                CreadoEl = t.CreadoEl,
            };
        return await query.OrderBy(c => c.Nombre).ToListAsync(ct);
    }

    public Task<Tenant?> ObtenerTenantAsync(Guid id, CancellationToken ct = default) =>
        _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Guid>> ListarUsuarioIdsAdminsAsync(CancellationToken ct = default) =>
        await _db.Users.Where(u => u.EsAdminPlataforma).Select(u => u.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<PersonaAdminDto>> ListarPersonasAsync(CancellationToken ct = default)
    {
        var duenos = await (
            from t in _db.Tenants.AsNoTracking()
            where t.OwnerUserId != null
            select new { UserId = t.OwnerUserId!.Value, Tipo = "Dueño", Club = t.Nombre }
        ).ToListAsync(ct);

        var staff = await (
            from m in _db.MembresiasTenant.AsNoTracking()
            join t in _db.Tenants.AsNoTracking() on m.TenantId equals t.Id
            select new { UserId = m.UserId, Tipo = "Staff", Club = t.Nombre }
        ).ToListAsync(ct);

        var alumnos = await (
            from a in _db.Alumnos.AsNoTracking()
            where a.UserId != null
            join t in _db.Tenants.AsNoTracking() on a.TenantId equals t.Id
            select new { UserId = a.UserId!.Value, Tipo = "Alumno", Club = t.Nombre }
        ).Distinct().ToListAsync(ct);

        var roles = duenos.Concat(staff).Concat(alumnos).ToList();

        var admins = await _db.Users.AsNoTracking()
            .Where(u => u.EsAdminPlataforma).Select(u => u.Id).ToListAsync(ct);
        var userIds = roles.Select(r => r.UserId).Concat(admins).Distinct().ToList();

        var usuarios = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(ct);

        return usuarios
            .OrderBy(u => u.Nombre).ThenBy(u => u.Apellido)
            .Select(u => new PersonaAdminDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                Telefono = u.PhoneNumber,
                EsAdminPlataforma = u.EsAdminPlataforma,
                Roles = roles.Where(r => r.UserId == u.Id)
                    .Select(r => new RolPersonaDto { Tipo = r.Tipo, Club = r.Club })
                    .ToList(),
            })
            .ToList();
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
