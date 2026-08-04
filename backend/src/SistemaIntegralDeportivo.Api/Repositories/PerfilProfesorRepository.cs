using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface IPerfilProfesorRepository
{
    // ── Scopeadas al tenant actual (el profe editando lo suyo) ──

    /// <summary>El perfil del usuario en el tenant actual, con fotos e hitos.</summary>
    Task<PerfilProfesor?> ObtenerDeUsuarioAsync(Guid userId, CancellationToken ct = default);
    Task AgregarAsync(PerfilProfesor perfil, CancellationToken ct = default);

    /// <summary>
    /// Las hijas se agregan por acá y no solo sumándolas a la colección del perfil:
    /// como su Id se asigna en C# (no lo genera la base), EF ve una PK con valor y
    /// da por hecho que la fila ya existe → generaría un UPDATE de cero filas.
    /// </summary>
    void AgregarFoto(FotoPerfil foto);
    void AgregarHito(HitoTrayectoria hito);

    void Eliminar(PerfilProfesor perfil);
    void EliminarFoto(FotoPerfil foto);
    void EliminarHito(HitoTrayectoria hito);
    Task GuardarCambiosAsync(CancellationToken ct = default);

    // ── NO scopeadas: leen el club que pide el alumno, que NO es el tenant de su
    //    token (un jugador sin ficha no tiene ninguno). Reciben el tenantId por
    //    parámetro y nunca tocan ITenantActual, así el fail-fast sigue intacto. ──

    Task<Tenant?> ObtenerTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Los profes del club: el dueño y sus empleados activos, con perfil si tienen.</summary>
    Task<IReadOnlyList<(Usuario Usuario, bool EsDueño, PerfilProfesor? Perfil)>> ListarProfesoresDelClubAsync(
        Guid tenantId, CancellationToken ct = default);

    /// <summary>El perfil completo de un profe de ese club (con fotos e hitos) y su usuario.</summary>
    Task<(Usuario Usuario, PerfilProfesor Perfil)?> ObtenerDeClubAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default);

    /// <summary>¿Sigue dando clases en ese club? (es el dueño o tiene membresía activa)</summary>
    Task<bool> TrabajaEnElClubAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
}

public class PerfilProfesorRepository : IPerfilProfesorRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public PerfilProfesorRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    // ── Scopeadas ──

    public Task<PerfilProfesor?> ObtenerDeUsuarioAsync(Guid userId, CancellationToken ct = default) =>
        _db.PerfilesProfesor
            .Include(p => p.Fotos)
            .Include(p => p.Hitos)
            .FirstOrDefaultAsync(p => p.TenantId == TenantId && p.UserId == userId, ct);

    public async Task AgregarAsync(PerfilProfesor perfil, CancellationToken ct = default)
    {
        perfil.TenantId = TenantId;
        _db.PerfilesProfesor.Add(perfil);
        await Task.CompletedTask;
    }

    public void AgregarFoto(FotoPerfil foto) => _db.FotosPerfil.Add(foto);

    public void AgregarHito(HitoTrayectoria hito) => _db.HitosTrayectoria.Add(hito);

    public void Eliminar(PerfilProfesor perfil) => _db.PerfilesProfesor.Remove(perfil);

    public void EliminarFoto(FotoPerfil foto) => _db.FotosPerfil.Remove(foto);

    public void EliminarHito(HitoTrayectoria hito) => _db.HitosTrayectoria.Remove(hito);

    public Task GuardarCambiosAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    // ── NO scopeadas (lectura del club que pide el alumno) ──

    public Task<Tenant?> ObtenerTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);

    public async Task<IReadOnlyList<(Usuario, bool, PerfilProfesor?)>> ListarProfesoresDelClubAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null) return [];

        // El dueño no tiene membresía (se resuelve por OwnerUserId); los empleados sí,
        // y solo cuentan los activos.
        var idsStaff = await _db.MembresiasTenant.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.Activo)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        var ids = new List<Guid>();
        if (tenant.OwnerUserId is Guid dueño) ids.Add(dueño);
        ids.AddRange(idsStaff.Where(id => !ids.Contains(id)));

        var usuarios = await _db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(ct);

        var perfiles = await _db.PerfilesProfesor.AsNoTracking()
            .Where(p => p.TenantId == tenantId && ids.Contains(p.UserId))
            .ToListAsync(ct);

        // Se respeta el orden de "ids": el dueño primero, después los empleados
        return ids
            .Select(id => (Usuario: usuarios.FirstOrDefault(u => u.Id == id), Id: id))
            .Where(x => x.Usuario is not null)
            .Select(x => (
                x.Usuario!,
                x.Id == tenant.OwnerUserId,
                perfiles.FirstOrDefault(p => p.UserId == x.Id)))
            .ToList();
    }

    public async Task<(Usuario, PerfilProfesor)?> ObtenerDeClubAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var perfil = await _db.PerfilesProfesor.AsNoTracking()
            .Include(p => p.Fotos)
            .Include(p => p.Hitos)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId, ct);
        if (perfil is null) return null;

        var usuario = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return usuario is null ? null : (usuario, perfil);
    }

    public async Task<bool> TrabajaEnElClubAsync(Guid tenantId, Guid userId, CancellationToken ct = default) =>
        await _db.Tenants.AsNoTracking().AnyAsync(t => t.Id == tenantId && t.OwnerUserId == userId, ct)
        || await _db.MembresiasTenant.AsNoTracking()
            .AnyAsync(m => m.TenantId == tenantId && m.UserId == userId && m.Activo, ct);
}
