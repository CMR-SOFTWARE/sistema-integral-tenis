using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface IServicioRepository
{
    /// <summary>El catálogo del profe. soloActivos=true para el portal del alumno.</summary>
    Task<IReadOnlyList<Servicio>> ListarAsync(bool soloActivos, CancellationToken ct = default);
    Task<Servicio?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task AgregarAsync(Servicio servicio, CancellationToken ct = default);

    /// <summary>
    /// Las fotos se agregan y se borran por acá, y no solo tocando la colección del
    /// producto: como su Id se asigna en C# (no lo genera la base), EF ve una PK con
    /// valor y da por hecho que la fila ya existe → manda un UPDATE de cero filas y
    /// revienta con <c>DbUpdateConcurrencyException</c>. Mismo motivo que en
    /// <see cref="IPerfilProfesorRepository.AgregarFoto"/>.
    /// </summary>
    void AgregarFoto(FotoServicio foto);
    void EliminarFoto(FotoServicio foto);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class ServicioRepository : IServicioRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public ServicioRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<Servicio>> ListarAsync(bool soloActivos, CancellationToken ct = default)
    {
        // Include de las fotos: el catálogo se muestra CON ellas, así que traerlas aparte
        // sería una consulta por producto.
        var query = _db.Servicios.Include(s => s.Fotos).Where(s => s.TenantId == TenantId);
        if (soloActivos) query = query.Where(s => s.Activo);
        return await query.OrderBy(s => s.Nombre).ToListAsync(ct);
    }

    public Task<Servicio?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Servicios.Include(s => s.Fotos)
            .FirstOrDefaultAsync(s => s.TenantId == TenantId && s.Id == id, ct);

    public async Task AgregarAsync(Servicio servicio, CancellationToken ct = default)
    {
        servicio.TenantId = TenantId;
        _db.Servicios.Add(servicio);
        await Task.CompletedTask;
    }

    public void AgregarFoto(FotoServicio foto) => _db.FotosServicio.Add(foto);

    public void EliminarFoto(FotoServicio foto) => _db.FotosServicio.Remove(foto);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
