using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Pedidos de servicios (M4, TDD): el alumno pide del catálogo (snapshot del
/// precio), el profe acepta (nace el cargo Producto) o rechaza (sin deuda).
/// La deuda solo existe si el profe acepta.
/// </summary>
public class PedidoServiceTests
{
    private static readonly Guid AlumnoId = Guid.NewGuid();

    private readonly Mock<IPedidoRepository> _pedidos;
    private readonly Mock<IServicioRepository> _servicios;
    private readonly Mock<ICargoRepository> _cargos;
    private readonly PedidoService _service;
    private readonly List<Cargo> _cargosCreados = [];
    private Pedido? _pedidoCreado;

    public PedidoServiceTests()
    {
        _pedidos = new Mock<IPedidoRepository>();
        _servicios = new Mock<IServicioRepository>();
        _cargos = new Mock<ICargoRepository>();
        _service = new PedidoService(_pedidos.Object, _servicios.Object, _cargos.Object);

        _pedidos.Setup(p => p.AgregarAsync(It.IsAny<Pedido>(), It.IsAny<CancellationToken>()))
                .Callback((Pedido p, CancellationToken _) => _pedidoCreado = p)
                .Returns(Task.CompletedTask);
        _cargos.Setup(c => c.AgregarAsync(It.IsAny<Cargo>(), It.IsAny<CancellationToken>()))
               .Callback((Cargo c, CancellationToken _) => _cargosCreados.Add(c))
               .Returns(Task.CompletedTask);
    }

    private Servicio ServicioEnCatalogo(bool activo = true, decimal precio = 12_000m)
    {
        var servicio = new Servicio { Nombre = "Encordado", Precio = precio, Activo = activo };
        _servicios.Setup(s => s.ObtenerAsync(servicio.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(servicio);
        return servicio;
    }

    private Pedido PedidoPendiente()
    {
        var pedido = new Pedido
        {
            AlumnoId = AlumnoId, ServicioId = Guid.NewGuid(),
            NombreServicio = "Encordado", Precio = 12_000m, Estado = EstadoPedido.Pendiente,
        };
        _pedidos.Setup(p => p.ObtenerAsync(pedido.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pedido);
        return pedido;
    }

    // ── Pedir (alumno) ──

    [Fact]
    public async Task Pedir_ServicioActivo_CreaPedidoPendiente_ConSnapshotDePrecio()
    {
        var servicio = ServicioEnCatalogo(precio: 12_000m);

        var dto = await _service.PedirAsync(AlumnoId, servicio.Id);

        Assert.Equal("Pendiente", dto.Estado);
        Assert.NotNull(_pedidoCreado);
        Assert.Equal(AlumnoId, _pedidoCreado!.AlumnoId);
        Assert.Equal("Encordado", _pedidoCreado.NombreServicio); // snapshot del nombre
        Assert.Equal(12_000m, _pedidoCreado.Precio);              // snapshot del precio
        Assert.Equal(EstadoPedido.Pendiente, _pedidoCreado.Estado);
        // Pedir NO genera deuda todavía
        _cargos.Verify(c => c.AgregarAsync(It.IsAny<Cargo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pedir_ServicioInactivo_Lanza()
    {
        var servicio = ServicioEnCatalogo(activo: false);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.PedirAsync(AlumnoId, servicio.Id));

        _pedidos.Verify(p => p.AgregarAsync(It.IsAny<Pedido>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pedir_ServicioInexistente_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.PedirAsync(AlumnoId, Guid.NewGuid()));
    }

    // ── Aceptar (profe): nace el cargo ──

    [Fact]
    public async Task Aceptar_GeneraCargoProducto_ConSnapshot_YMarcaAceptado()
    {
        var pedido = PedidoPendiente();

        await _service.AceptarAsync(pedido.Id);

        var cargo = Assert.Single(_cargosCreados);
        Assert.Equal(TipoCargo.Producto, cargo.Tipo);
        Assert.Equal("Encordado", cargo.Concepto);   // del snapshot del pedido
        Assert.Equal(12_000m, cargo.Monto);
        Assert.Equal(AlumnoId, cargo.AlumnoId);
        Assert.Null(cargo.PagadoEl);                  // nace impago: se cobra con la maquinaria de M2

        Assert.Equal(EstadoPedido.Aceptado, pedido.Estado);
        Assert.NotNull(pedido.ResueltoEl);
        Assert.Equal(cargo.Id, pedido.CargoId);       // el pedido queda linkeado a su cargo
        _pedidos.Verify(p => p.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Aceptar_PedidoYaResuelto_Lanza_YNoGeneraOtroCargo()
    {
        var pedido = PedidoPendiente();
        pedido.Estado = EstadoPedido.Aceptado; // ya lo aceptó antes

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AceptarAsync(pedido.Id));

        Assert.Empty(_cargosCreados);
    }

    [Fact]
    public async Task Aceptar_PedidoInexistente_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AceptarAsync(Guid.NewGuid()));
    }

    // ── Rechazar (profe): sin deuda ──

    [Fact]
    public async Task Rechazar_MarcaRechazado_SinGenerarCargo()
    {
        var pedido = PedidoPendiente();

        await _service.RechazarAsync(pedido.Id);

        Assert.Equal(EstadoPedido.Rechazado, pedido.Estado);
        Assert.NotNull(pedido.ResueltoEl);
        Assert.Empty(_cargosCreados);                 // rechazar nunca cobra
        _pedidos.Verify(p => p.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rechazar_PedidoYaResuelto_Lanza()
    {
        var pedido = PedidoPendiente();
        pedido.Estado = EstadoPedido.Rechazado;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.RechazarAsync(pedido.Id));
    }
}
