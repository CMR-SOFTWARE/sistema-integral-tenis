using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Pedidos de servicios con carrito (M4 + Bloque 5, TDD): el alumno arma un
/// carrito con varias líneas y lo manda como UN pedido; el profe acepta (nace UN
/// cargo con el total de todas las líneas) o rechaza (sin deuda).
/// </summary>
public class PedidoServiceTests
{
    private static readonly Guid AlumnoId = Guid.NewGuid();

    private readonly Mock<IPedidoRepository> _pedidos;
    private readonly Mock<IServicioRepository> _servicios;
    private readonly Mock<ICargoRepository> _cargos;
    private readonly Mock<IAlumnoRepository> _alumnos;
    private readonly PedidoService _service;
    private readonly List<Cargo> _cargosCreados = [];
    private Pedido? _pedidoCreado;

    public PedidoServiceTests()
    {
        _pedidos = new Mock<IPedidoRepository>();
        _servicios = new Mock<IServicioRepository>();
        _cargos = new Mock<ICargoRepository>();
        _alumnos = new Mock<IAlumnoRepository>();
        _service = new PedidoService(
            _pedidos.Object, _servicios.Object, _cargos.Object, _alumnos.Object);

        _pedidos.Setup(p => p.AgregarAsync(It.IsAny<Pedido>(), It.IsAny<CancellationToken>()))
                .Callback((Pedido p, CancellationToken _) => _pedidoCreado = p)
                .Returns(Task.CompletedTask);
        _cargos.Setup(c => c.AgregarAsync(It.IsAny<Cargo>(), It.IsAny<CancellationToken>()))
               .Callback((Cargo c, CancellationToken _) => _cargosCreados.Add(c))
               .Returns(Task.CompletedTask);
        // El repositorio está scopeado por tenant: que devuelva la ficha significa
        // "es de mi academia". Los alumnos ajenos simplemente no aparecen (null).
        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Alumno { Id = AlumnoId, Nombre = "Mariana", Apellido = "Castro", Telefono = "336474" });
    }

    private Servicio ServicioEnCatalogo(string nombre = "Encordado", bool activo = true, decimal precio = 12_000m)
    {
        var servicio = new Servicio { Nombre = nombre, Precio = precio, Activo = activo };
        _servicios.Setup(s => s.ObtenerAsync(servicio.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(servicio);
        return servicio;
    }

    private Pedido PedidoPendiente(params (string Nombre, decimal Precio, int Cantidad)[] lineas)
    {
        var pedido = new Pedido { AlumnoId = AlumnoId, Estado = EstadoPedido.Pendiente };
        if (lineas.Length == 0) lineas = [("Encordado", 12_000m, 1)];
        foreach (var (nombre, precio, cantidad) in lineas)
        {
            pedido.Lineas.Add(new PedidoLinea
            {
                PedidoId = pedido.Id, Pedido = pedido, ServicioId = Guid.NewGuid(),
                NombreServicio = nombre, PrecioUnitario = precio, Cantidad = cantidad,
            });
        }
        _pedidos.Setup(p => p.ObtenerAsync(pedido.Id, It.IsAny<CancellationToken>())).ReturnsAsync(pedido);
        return pedido;
    }

    // ── Pedir (alumno): el carrito ──

    [Fact]
    public async Task Pedir_ConVariasLineas_CreaUnPedidoConTodasSusLineasYSnapshot()
    {
        var encordado = ServicioEnCatalogo("Encordado", precio: 12_000m);
        var tubo = ServicioEnCatalogo("Tubo de pelotas", precio: 8_000m);

        var dto = await _service.PedirAsync(AlumnoId, [(encordado.Id, 1, null), (tubo.Id, 2, null)]);

        Assert.Equal("Pendiente", dto.Estado);
        Assert.NotNull(_pedidoCreado);
        Assert.Equal(AlumnoId, _pedidoCreado!.AlumnoId);
        Assert.Equal(2, _pedidoCreado.Lineas.Count);
        var l1 = _pedidoCreado.Lineas.Single(l => l.ServicioId == encordado.Id);
        Assert.Equal("Encordado", l1.NombreServicio);
        Assert.Equal(12_000m, l1.PrecioUnitario);
        Assert.Equal(1, l1.Cantidad);
        var l2 = _pedidoCreado.Lineas.Single(l => l.ServicioId == tubo.Id);
        Assert.Equal("Tubo de pelotas", l2.NombreServicio);
        Assert.Equal(8_000m, l2.PrecioUnitario);
        Assert.Equal(2, l2.Cantidad); // cantidad tal cual la pidió
        Assert.Equal(EstadoPedido.Pendiente, _pedidoCreado.Estado);
        // Pedir NO genera deuda todavía
        _cargos.Verify(c => c.AgregarAsync(It.IsAny<Cargo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pedir_CarritoVacio_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.PedirAsync(AlumnoId, []));

        _pedidos.Verify(p => p.AgregarAsync(It.IsAny<Pedido>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pedir_AlgunServicioInactivo_LanzaYNoCreaNada()
    {
        var encordado = ServicioEnCatalogo("Encordado");
        var tubo = ServicioEnCatalogo("Tubo de pelotas", activo: false);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.PedirAsync(AlumnoId, [(encordado.Id, 1, null), (tubo.Id, 1, null)]));

        _pedidos.Verify(p => p.AgregarAsync(It.IsAny<Pedido>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pedir_AlgunServicioInexistente_Lanza()
    {
        var encordado = ServicioEnCatalogo("Encordado");

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.PedirAsync(AlumnoId, [(encordado.Id, 1, null), (Guid.NewGuid(), 1, null)]));

        _pedidos.Verify(p => p.AgregarAsync(It.IsAny<Pedido>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Aceptar (profe): nace UN cargo con el total ──

    [Fact]
    public async Task Aceptar_GeneraUnSoloCargoConElTotalDeTodasLasLineas()
    {
        var pedido = PedidoPendiente(("Encordado", 12_000m, 1), ("Tubo de pelotas", 8_000m, 2));

        await _service.AceptarAsync(pedido.Id);

        var cargo = Assert.Single(_cargosCreados);
        Assert.Equal(TipoCargo.Producto, cargo.Tipo);
        Assert.Equal(28_000m, cargo.Monto); // 12.000 + 8.000×2
        Assert.Equal(AlumnoId, cargo.AlumnoId);
        Assert.Null(cargo.PagadoEl); // nace impago: se cobra con la maquinaria de M2

        Assert.Equal(EstadoPedido.Aceptado, pedido.Estado);
        Assert.NotNull(pedido.ResueltoEl);
        Assert.Equal(cargo.Id, pedido.CargoId);
        _pedidos.Verify(p => p.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Aceptar_ElConceptoJuntaLosNombresDeLasLineas()
    {
        var pedido = PedidoPendiente(("Encordado", 12_000m, 1), ("Tubo de pelotas", 8_000m, 2));

        await _service.AceptarAsync(pedido.Id);

        Assert.Equal("Encordado, Tubo de pelotas x2", Assert.Single(_cargosCreados).Concepto);
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

    // ── Cargar (profe): se lo carga él, sin pasar por la bandeja ──

    [Fact]
    public async Task Cargar_NaceAceptadoYConSuCargo()
    {
        // El caso real: el alumno le pidió el encordado por WhatsApp y el profe se lo
        // carga. Dejarlo Pendiente lo obligaría a aceptar su propio pedido.
        var encordado = ServicioEnCatalogo("Encordado", precio: 12_000m);
        var tubo = ServicioEnCatalogo("Tubo de pelotas", precio: 8_000m);

        var dto = await _service.CargarAlAlumnoAsync(AlumnoId, [(encordado.Id, 1, null), (tubo.Id, 2, null)]);

        Assert.Equal("Aceptado", dto.Estado);
        Assert.Equal(EstadoPedido.Aceptado, _pedidoCreado!.Estado);
        Assert.NotNull(_pedidoCreado.ResueltoEl);

        var cargo = Assert.Single(_cargosCreados);
        Assert.Equal(TipoCargo.Producto, cargo.Tipo);
        Assert.Equal(28_000m, cargo.Monto); // 12.000 + 8.000×2
        Assert.Equal(AlumnoId, cargo.AlumnoId);
        Assert.Equal(cargo.Id, _pedidoCreado.CargoId);
    }

    [Fact]
    public async Task Cargar_GuardaElSnapshotDelPrecio()
    {
        // Mismo contrato que el pedido del alumno: cambiar el precio después no toca
        // lo ya cargado.
        var encordado = ServicioEnCatalogo("Encordado", precio: 12_000m);

        await _service.CargarAlAlumnoAsync(AlumnoId, [(encordado.Id, 1, "Luxilon ALU 1.25")]);

        var linea = Assert.Single(_pedidoCreado!.Lineas);
        Assert.Equal("Encordado", linea.NombreServicio);
        Assert.Equal(12_000m, linea.PrecioUnitario);
        Assert.Equal("Luxilon ALU 1.25", linea.Nota);
    }

    [Fact]
    public async Task Cargar_AlumnoDeOtroTenant_LanzaYNoCreaNada()
    {
        // El alumnoId lo manda el cliente: sin este chequeo, el profe podría cargarle
        // un producto a la ficha de OTRA academia. El repo scopeado devuelve null.
        var encordado = ServicioEnCatalogo();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CargarAlAlumnoAsync(Guid.NewGuid(), [(encordado.Id, 1, null)]));

        _pedidos.Verify(p => p.AgregarAsync(It.IsAny<Pedido>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(_cargosCreados);
    }

    [Fact]
    public async Task Cargar_ServicioInactivo_LanzaYNoCreaNada()
    {
        var encordado = ServicioEnCatalogo("Encordado");
        var tubo = ServicioEnCatalogo("Tubo de pelotas", activo: false);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CargarAlAlumnoAsync(AlumnoId, [(encordado.Id, 1, null), (tubo.Id, 1, null)]));

        _pedidos.Verify(p => p.AgregarAsync(It.IsAny<Pedido>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(_cargosCreados);
    }

    [Fact]
    public async Task Cargar_CarritoVacio_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CargarAlAlumnoAsync(AlumnoId, []));

        _pedidos.Verify(p => p.AgregarAsync(It.IsAny<Pedido>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Rechazar (profe): sin deuda ──

    [Fact]
    public async Task Rechazar_MarcaRechazado_SinGenerarCargo()
    {
        var pedido = PedidoPendiente();

        await _service.RechazarAsync(pedido.Id);

        Assert.Equal(EstadoPedido.Rechazado, pedido.Estado);
        Assert.NotNull(pedido.ResueltoEl);
        Assert.Empty(_cargosCreados); // rechazar nunca cobra
        _pedidos.Verify(p => p.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rechazar_PedidoYaResuelto_Lanza()
    {
        var pedido = PedidoPendiente();
        pedido.Estado = EstadoPedido.Rechazado;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.RechazarAsync(pedido.Id));
    }

    // ── Cancelar (alumno): retira su propio pedido antes de que se resuelva ──

    [Fact]
    public async Task Cancelar_PedidoPropioPendiente_LoElimina()
    {
        var pedido = PedidoPendiente();

        await _service.CancelarAsync(AlumnoId, pedido.Id);

        _pedidos.Verify(p => p.Eliminar(pedido), Times.Once);
        _pedidos.Verify(p => p.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancelar_PedidoDeOtroAlumno_Lanza_YNoLoElimina()
    {
        var pedido = PedidoPendiente();
        var otroAlumno = Guid.NewGuid();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarAsync(otroAlumno, pedido.Id));

        _pedidos.Verify(p => p.Eliminar(It.IsAny<Pedido>()), Times.Never);
    }

    [Fact]
    public async Task Cancelar_PedidoYaResuelto_Lanza()
    {
        var pedido = PedidoPendiente();
        pedido.Estado = EstadoPedido.Aceptado;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarAsync(AlumnoId, pedido.Id));

        _pedidos.Verify(p => p.Eliminar(It.IsAny<Pedido>()), Times.Never);
    }

    [Fact]
    public async Task Cancelar_PedidoInexistente_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarAsync(AlumnoId, Guid.NewGuid()));
    }

    // ── La nota del alumno: va POR PRODUCTO ──

    [Fact]
    public async Task Pedir_ConNotas_CadaUnaQuedaEnSuLinea()
    {
        // El caso del profe: "encordado con Luxilon 1.25" no tiene nada que ver con el
        // tubo de pelotas que pidió en el mismo carrito.
        var encordado = ServicioEnCatalogo("Encordado");
        var tubo = ServicioEnCatalogo("Tubo de pelotas");

        await _service.PedirAsync(AlumnoId, [
            (encordado.Id, 1, "Luxilon ALU 1.25, tensión 24"),
            (tubo.Id, 2, null),
        ]);

        var lEncordado = _pedidoCreado!.Lineas.Single(l => l.ServicioId == encordado.Id);
        var lTubo = _pedidoCreado.Lineas.Single(l => l.ServicioId == tubo.Id);
        Assert.Equal("Luxilon ALU 1.25, tensión 24", lEncordado.Nota);
        Assert.Null(lTubo.Nota); // la nota del otro producto NO se le pega
    }

    [Fact]
    public async Task Pedir_NotaEnBlanco_SeGuardaNull()
    {
        // "" y null significan lo mismo (sin aclaración); tener las dos formas obliga a
        // chequear las dos en cada lugar que la muestre.
        var encordado = ServicioEnCatalogo();

        await _service.PedirAsync(AlumnoId, [(encordado.Id, 1, "   ")]);

        Assert.Null(Assert.Single(_pedidoCreado!.Lineas).Nota);
    }

    [Fact]
    public async Task Pedir_NotaConEspacios_SeGuardaRecortada()
    {
        var encordado = ServicioEnCatalogo();

        await _service.PedirAsync(AlumnoId, [(encordado.Id, 1, "  Wilson NXT  ")]);

        Assert.Equal("Wilson NXT", Assert.Single(_pedidoCreado!.Lineas).Nota);
    }

    [Fact]
    public async Task Aceptar_ElConceptoDelCargo_NoArrastraLasNotas()
    {
        // El concepto es la línea que el alumno ve en su cuenta corriente: si le
        // metiéramos el texto libre de la nota, quedaría un renglón larguísimo y
        // fuera de control. La nota la lee el profe en su bandeja.
        var pedido = PedidoPendiente(("Encordado", 12_000m, 1));
        pedido.Lineas.Single().Nota = "Luxilon ALU 1.25";

        await _service.AceptarAsync(pedido.Id);

        var cargo = Assert.Single(_cargosCreados);
        Assert.Equal("Encordado", cargo.Concepto);
        Assert.DoesNotContain("Luxilon", cargo.Concepto);
    }
}
