using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Pedidos de servicios (M4): el alumno pide del catálogo, el profe acepta
/// (nace el cargo) o rechaza. La deuda solo existe si el profe acepta.
/// </summary>
public interface IPedidoService
{
    /// <summary>El alumno pide un servicio del catálogo (queda Pendiente, sin deuda todavía).</summary>
    Task<PedidoDto> PedirAsync(Guid alumnoId, Guid servicioId, CancellationToken ct = default);

    /// <summary>Los pedidos pendientes que el profe tiene para resolver.</summary>
    Task<IReadOnlyList<PedidoDto>> ListarPendientesAsync(CancellationToken ct = default);

    /// <summary>Mis pedidos (portal del alumno) con su estado.</summary>
    Task<IReadOnlyList<PedidoDto>> MisPedidosAsync(Guid alumnoId, CancellationToken ct = default);

    /// <summary>Cuántos pedidos pendientes hay (contador del dashboard).</summary>
    Task<int> ContarPendientesAsync(CancellationToken ct = default);

    /// <summary>El profe acepta: nace el cargo (Producto) en la cuenta del alumno.</summary>
    Task AceptarAsync(Guid pedidoId, CancellationToken ct = default);

    /// <summary>El profe rechaza: el pedido se descarta, sin deuda.</summary>
    Task RechazarAsync(Guid pedidoId, CancellationToken ct = default);
}

public class PedidoService : IPedidoService
{
    private readonly IPedidoRepository _pedidos;
    private readonly IServicioRepository _servicios;
    private readonly ICargoRepository _cargos;

    public PedidoService(
        IPedidoRepository pedidos, IServicioRepository servicios, ICargoRepository cargos)
    {
        _pedidos = pedidos;
        _servicios = servicios;
        _cargos = cargos;
    }

    public async Task<PedidoDto> PedirAsync(Guid alumnoId, Guid servicioId, CancellationToken ct = default)
    {
        var servicio = await _servicios.ObtenerAsync(servicioId, ct)
            ?? throw new ReglaDeNegocioException("El servicio no existe.");
        if (!servicio.Activo)
            throw new ReglaDeNegocioException("Ese servicio ya no está disponible.");

        var pedido = new Pedido
        {
            AlumnoId = alumnoId,
            ServicioId = servicio.Id,
            NombreServicio = servicio.Nombre, // snapshot: el precio del pedido no cambia
            Precio = servicio.Precio,          // aunque el profe lo edite después
            Estado = EstadoPedido.Pendiente,
        };
        await _pedidos.AgregarAsync(pedido, ct);
        await _pedidos.GuardarCambiosAsync(ct);
        return Mapear(pedido);
    }

    public async Task<IReadOnlyList<PedidoDto>> ListarPendientesAsync(CancellationToken ct = default)
    {
        var pedidos = await _pedidos.ListarPorEstadoAsync(EstadoPedido.Pendiente, ct);
        return pedidos.Select(Mapear).ToList();
    }

    public async Task<IReadOnlyList<PedidoDto>> MisPedidosAsync(Guid alumnoId, CancellationToken ct = default)
    {
        var pedidos = await _pedidos.ListarPorAlumnoAsync(alumnoId, ct);
        return pedidos.Select(Mapear).ToList();
    }

    public Task<int> ContarPendientesAsync(CancellationToken ct = default) =>
        _pedidos.ContarPorEstadoAsync(EstadoPedido.Pendiente, ct);

    public async Task AceptarAsync(Guid pedidoId, CancellationToken ct = default)
    {
        var pedido = await _pedidos.ObtenerAsync(pedidoId, ct)
            ?? throw new ReglaDeNegocioException("El pedido no existe.");
        if (pedido.Estado != EstadoPedido.Pendiente)
            throw new ReglaDeNegocioException("Ese pedido ya fue resuelto.");

        // Aceptar = nace la deuda: un cargo Producto con el snapshot del pedido.
        // Entra en la cuenta corriente y sigue la maquinaria de cobro (M2).
        var cargo = new Cargo
        {
            AlumnoId = pedido.AlumnoId,
            Tipo = TipoCargo.Producto,
            Concepto = pedido.NombreServicio,
            Monto = pedido.Precio,
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow),
            // TenantId lo asigna el repositorio
        };
        await _cargos.AgregarAsync(cargo, ct);

        pedido.Estado = EstadoPedido.Aceptado;
        pedido.ResueltoEl = DateTime.UtcNow;
        pedido.CargoId = cargo.Id;

        // Mismo DbContext: un solo guardado persiste el cargo Y el pedido
        await _pedidos.GuardarCambiosAsync(ct);
    }

    public async Task RechazarAsync(Guid pedidoId, CancellationToken ct = default)
    {
        var pedido = await _pedidos.ObtenerAsync(pedidoId, ct)
            ?? throw new ReglaDeNegocioException("El pedido no existe.");
        if (pedido.Estado != EstadoPedido.Pendiente)
            throw new ReglaDeNegocioException("Ese pedido ya fue resuelto.");

        pedido.Estado = EstadoPedido.Rechazado;
        pedido.ResueltoEl = DateTime.UtcNow;
        await _pedidos.GuardarCambiosAsync(ct);
    }

    private static PedidoDto Mapear(Pedido p) => new()
    {
        Id = p.Id,
        AlumnoId = p.AlumnoId,
        AlumnoNombre = p.Alumno is null ? string.Empty : $"{p.Alumno.Nombre} {p.Alumno.Apellido}",
        NombreServicio = p.NombreServicio,
        Precio = p.Precio,
        Estado = p.Estado.ToString(),
        PedidoEl = p.PedidoEl,
        ResueltoEl = p.ResueltoEl,
    };
}
