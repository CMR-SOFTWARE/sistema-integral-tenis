using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// SueldoService cruza lo CALCULADO (política) con lo PAGADO (ledger): arma la
/// pantalla del mes, registra el pago (uno por mes, monto ajustable) y lo revierte.
/// </summary>
public class SueldoServiceTests
{
    private static readonly Guid Profe = Guid.NewGuid();

    private readonly Mock<IPoliticaDeSueldo> _politica = new();
    private readonly Mock<IPagoEmpleadoRepository> _pagos = new();
    private readonly Mock<IMembresiaTenantRepository> _membresias = new();
    private readonly SueldoService _service;

    private readonly List<SueldoCalculadoDto> _calculados = [];
    private readonly List<PagoEmpleado> _pagosDelMes = [];

    public SueldoServiceTests()
    {
        _service = new SueldoService(_politica.Object, _pagos.Object, _membresias.Object);

        _politica.Setup(p => p.CalcularDelMesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(() => [.. _calculados]);
        _pagos.Setup(p => p.ListarDelMesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(() => [.. _pagosDelMes]);
        _pagos.Setup(p => p.ObtenerDelMesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((PagoEmpleado?)null);
        _pagos.Setup(p => p.AgregarAsync(It.IsAny<PagoEmpleado>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
        _pagos.Setup(p => p.GuardarCambiosAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        // Por defecto el profe ES del equipo (los tests que prueben lo contrario lo pisan)
        _membresias.Setup(m => m.ObtenerPorUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new MembresiaTenant { UserId = Profe });
    }

    private void Calculado(Guid userId, decimal monto) =>
        _calculados.Add(new SueldoCalculadoDto
        {
            UserId = userId, Nombre = "Profe", Apellido = "Test", Activo = true,
            Monto = monto, HorasTotales = 2m, TieneValorHora = true,
        });

    // ── Armado de la pantalla ──

    [Fact]
    public async Task ObtenerMes_SinPago_QuedaPendienteConTodoElSaldo()
    {
        Calculado(Profe, 16_000m);

        var liq = await _service.ObtenerMesAsync(2026, 7);

        var fila = Assert.Single(liq.Empleados);
        Assert.Equal(16_000m, fila.Calculado);
        Assert.Equal(0m, fila.Pagado);
        Assert.Equal(16_000m, fila.Saldo);
        Assert.Equal("Pendiente", fila.Estado);
        Assert.Equal(16_000m, liq.TotalAPagar);
        Assert.Equal(16_000m, liq.TotalPendiente);
    }

    [Fact]
    public async Task ObtenerMes_ConPago_QuedaPagadoConSaldoCero()
    {
        Calculado(Profe, 16_000m);
        _pagosDelMes.Add(new PagoEmpleado { UserId = Profe, Anio = 2026, Mes = 7, Monto = 16_000m, MedioPago = MedioPago.Transferencia, PagadoEl = DateTime.UtcNow });

        var liq = await _service.ObtenerMesAsync(2026, 7);

        var fila = Assert.Single(liq.Empleados);
        Assert.Equal(16_000m, fila.Pagado);
        Assert.Equal(0m, fila.Saldo);
        Assert.Equal("Pagado", fila.Estado);
        Assert.Equal("Transferencia", fila.MedioPago);
        Assert.Equal(16_000m, liq.TotalPagado);
    }

    // ── Pagar ──

    [Fact]
    public async Task Pagar_CreaElPagoConElMontoYMedio()
    {
        PagoEmpleado? creado = null;
        _pagos.Setup(p => p.AgregarAsync(It.IsAny<PagoEmpleado>(), It.IsAny<CancellationToken>()))
              .Callback<PagoEmpleado, CancellationToken>((p, _) => creado = p)
              .Returns(Task.CompletedTask);

        await _service.PagarAsync(Profe, 2026, 7, 15_000m, MedioPago.Efectivo);

        Assert.NotNull(creado);
        Assert.Equal(Profe, creado!.UserId);
        Assert.Equal(2026, creado.Anio);
        Assert.Equal(7, creado.Mes);
        Assert.Equal(15_000m, creado.Monto);
        Assert.Equal(MedioPago.Efectivo, creado.MedioPago);
        _pagos.Verify(p => p.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pagar_MesYaPagado_Lanza()
    {
        _pagos.Setup(p => p.ObtenerDelMesAsync(Profe, 2026, 7, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new PagoEmpleado { UserId = Profe, Anio = 2026, Mes = 7, Monto = 10_000m });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.PagarAsync(Profe, 2026, 7, 15_000m, MedioPago.Efectivo));

        _pagos.Verify(p => p.AgregarAsync(It.IsAny<PagoEmpleado>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pagar_ProfeAjeno_Lanza()
    {
        _membresias.Setup(m => m.ObtenerPorUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((MembresiaTenant?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.PagarAsync(Guid.NewGuid(), 2026, 7, 15_000m, MedioPago.Efectivo));
    }

    [Fact]
    public async Task Pagar_MontoNegativo_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.PagarAsync(Profe, 2026, 7, -1m, MedioPago.Efectivo));
    }

    // ── Revertir ──

    [Fact]
    public async Task Revertir_BorraElPagoYPersiste()
    {
        var pago = new PagoEmpleado { UserId = Profe, Anio = 2026, Mes = 7, Monto = 16_000m };
        _pagos.Setup(p => p.ObtenerDelMesAsync(Profe, 2026, 7, It.IsAny<CancellationToken>())).ReturnsAsync(pago);

        await _service.RevertirPagoAsync(Profe, 2026, 7);

        _pagos.Verify(p => p.Eliminar(pago), Times.Once);
        _pagos.Verify(p => p.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Revertir_SinPago_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.RevertirPagoAsync(Profe, 2026, 7));
    }
}
