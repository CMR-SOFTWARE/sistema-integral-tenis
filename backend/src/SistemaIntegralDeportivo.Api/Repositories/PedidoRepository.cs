using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public interface IPedidoRepository
{
    Task AgregarAsync(Pedido pedido, CancellationToken ct = default);
    Task<Pedido?> ObtenerAsync(Guid id, CancellationToken ct = default);
    /// <summary>Los pendientes del profe (con el alumno cargado), el más viejo primero.</summary>
    Task<IReadOnlyList<Pedido>> ListarPorEstadoAsync(EstadoPedido estado, CancellationToken ct = default);
    /// <summary>Mis pedidos (portal del alumno), el más reciente primero.</summary>
    Task<IReadOnlyList<Pedido>> ListarPorAlumnoAsync(Guid alumnoId, CancellationToken ct = default);
    Task<int> ContarPorEstadoAsync(EstadoPedido estado, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class PedidoRepository : IPedidoRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;
    private Guid TenantId => _tenantActual.TenantId;

    public PedidoRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task AgregarAsync(Pedido pedido, CancellationToken ct = default)
    {
        pedido.TenantId = TenantId;
        _db.Pedidos.Add(pedido);
        await Task.CompletedTask;
    }

    public Task<Pedido?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Pedidos.FirstOrDefaultAsync(p => p.TenantId == TenantId && p.Id == id, ct);

    public async Task<IReadOnlyList<Pedido>> ListarPorEstadoAsync(
        EstadoPedido estado, CancellationToken ct = default) =>
        await _db.Pedidos
            .Include(p => p.Alumno)
            .Where(p => p.TenantId == TenantId && p.Estado == estado)
            .OrderBy(p => p.PedidoEl)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Pedido>> ListarPorAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default) =>
        await _db.Pedidos
            .Where(p => p.TenantId == TenantId && p.AlumnoId == alumnoId)
            .OrderByDescending(p => p.PedidoEl)
            .ToListAsync(ct);

    public Task<int> ContarPorEstadoAsync(EstadoPedido estado, CancellationToken ct = default) =>
        _db.Pedidos.CountAsync(p => p.TenantId == TenantId && p.Estado == estado, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
