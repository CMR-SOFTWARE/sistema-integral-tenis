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
    /// <summary>
    /// El alumno pide su carrito (una o varias líneas) en un solo pedido — queda
    /// Pendiente, sin deuda todavía. Todo o nada: si algún servicio no existe o
    /// no está activo, no se crea nada.
    /// </summary>
    Task<PedidoDto> PedirAsync(
        Guid alumnoId, IReadOnlyList<(Guid ServicioId, int Cantidad, string? Nota)> lineas, CancellationToken ct = default);

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

    /// <summary>
    /// El alumno cancela su propio pedido mientras siga Pendiente (todavía no generó
    /// deuda, así que se borra directo — no hay nada que "revertir").
    /// </summary>
    Task CancelarAsync(Guid alumnoId, Guid pedidoId, CancellationToken ct = default);
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

    public async Task<PedidoDto> PedirAsync(
        Guid alumnoId, IReadOnlyList<(Guid ServicioId, int Cantidad, string? Nota)> lineas,
        CancellationToken ct = default)
    {
        if (lineas.Count == 0)
            throw new ReglaDeNegocioException("El carrito está vacío.");

        // Todo o nada: se resuelven y validan todas las líneas ANTES de tocar el
        // repositorio (mismo criterio que ClaseSueltaService.AsignarAsync).
        var servicios = new List<(Servicio Servicio, int Cantidad, string? Nota)>();
        foreach (var (servicioId, cantidad, nota) in lineas)
        {
            var servicio = await _servicios.ObtenerAsync(servicioId, ct)
                ?? throw new ReglaDeNegocioException("Uno de los servicios pedidos no existe.");
            if (!servicio.Activo)
                throw new ReglaDeNegocioException($"\"{servicio.Nombre}\" ya no está disponible.");
            servicios.Add((servicio, cantidad, nota));
        }

        var pedido = new Pedido { AlumnoId = alumnoId, Estado = EstadoPedido.Pendiente };
        foreach (var (servicio, cantidad, nota) in servicios)
        {
            pedido.Lineas.Add(new PedidoLinea
            {
                PedidoId = pedido.Id,
                Pedido = pedido,
                ServicioId = servicio.Id,
                NombreServicio = servicio.Nombre, // snapshot: el precio del pedido no cambia
                PrecioUnitario = servicio.Precio,  // aunque el profe lo edite después
                Cantidad = cantidad,
                // Vacío o solo espacios se guarda como null: "" y null significan lo mismo
                // (sin aclaración) y tener las dos formas obliga a chequear las dos siempre.
                Nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim(),
            });
        }
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

        // Aceptar = nace la deuda: UN cargo Producto con el total de todas las
        // líneas. Entra en la cuenta corriente y sigue la maquinaria de cobro (M2).
        var cargo = new Cargo
        {
            AlumnoId = pedido.AlumnoId,
            Tipo = TipoCargo.Producto,
            Concepto = ConceptoDe(pedido.Lineas),
            Monto = pedido.Lineas.Sum(l => l.PrecioUnitario * l.Cantidad),
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

    public async Task CancelarAsync(Guid alumnoId, Guid pedidoId, CancellationToken ct = default)
    {
        var pedido = await _pedidos.ObtenerAsync(pedidoId, ct)
            ?? throw new ReglaDeNegocioException("El pedido no existe.");
        if (pedido.AlumnoId != alumnoId)
            throw new ReglaDeNegocioException("Ese pedido no es tuyo.");
        if (pedido.Estado != EstadoPedido.Pendiente)
            throw new ReglaDeNegocioException("Ese pedido ya fue resuelto.");

        _pedidos.Eliminar(pedido);
        await _pedidos.GuardarCambiosAsync(ct);
    }

    /// <summary>"Encordado, Tubo de pelotas x2" — junta los nombres de las líneas del
    /// pedido para el Concepto de un cargo que en realidad tiene varios ítems.</summary>
    private static string ConceptoDe(IEnumerable<PedidoLinea> lineas) =>
        string.Join(", ", lineas.Select(l => l.Cantidad > 1 ? $"{l.NombreServicio} x{l.Cantidad}" : l.NombreServicio));

    private static PedidoDto Mapear(Pedido p) => new()
    {
        Id = p.Id,
        AlumnoId = p.AlumnoId,
        AlumnoNombre = p.Alumno is null ? string.Empty : $"{p.Alumno.Nombre} {p.Alumno.Apellido}",
        Lineas = p.Lineas.Select(l => new PedidoLineaDto
        {
            ServicioId = l.ServicioId,
            NombreServicio = l.NombreServicio,
            PrecioUnitario = l.PrecioUnitario,
            Cantidad = l.Cantidad,
            Subtotal = l.PrecioUnitario * l.Cantidad,
            Nota = l.Nota,
        }).ToList(),
        Total = p.Lineas.Sum(l => l.PrecioUnitario * l.Cantidad),
        Estado = p.Estado.ToString(),
        PedidoEl = p.PedidoEl,
        ResueltoEl = p.ResueltoEl,
    };
}
