using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reglas de la PLATA (ADR-0009) con el modelo MENSUAL: la generación de cargos
/// vive en <see cref="IPoliticaDeCuota"/> (costura aparte); acá se prueba que
/// CuotaService LEE el ledger, edita montos, registra pagos y calcula estados.
/// </summary>
public class CuotaServiceTests
{
    private static readonly Guid Juan = Guid.NewGuid();
    private static readonly Guid Sofia = Guid.NewGuid();

    private readonly Mock<ICargoRepository> _cargos;
    private readonly Mock<IAlumnoRepository> _alumnos;
    private readonly Mock<IPoliticaDeCuota> _politica;
    private readonly CuotaService _service;
    private readonly List<Cargo> _delMes = [];

    public CuotaServiceTests()
    {
        _cargos = new Mock<ICargoRepository>();
        _alumnos = new Mock<IAlumnoRepository>();
        _politica = new Mock<IPoliticaDeCuota>();
        _service = new CuotaService(_cargos.Object, _alumnos.Object, _politica.Object);

        _cargos.Setup(c => c.ListarDelMesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => [.. _delMes]);
        _cargos.Setup(c => c.AgregarAsync(It.IsAny<Cargo>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);
        // Por defecto no hay alumnos activos extra ni con clase (los tests que necesiten los agregan)
        _alumnos.Setup(a => a.ListarAsync(It.IsAny<CategoriaAlumno?>(), It.IsAny<EstadoAlumno?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
        _alumnos.Setup(a => a.ListarConClaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
    }

    // ─────────────────────────────────────────────
    // La costura: la generación se delega en la política
    // ─────────────────────────────────────────────

    [Fact]
    public async Task ObtenerMes_DelegaLaGeneracionEnLaPolitica()
    {
        await _service.ObtenerMesAsync(2026, 7);

        _politica.Verify(p => p.GenerarCargosDelMesAsync(2026, 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObtenerMes_ArmaLaLiquidacionLeyendoElLedger()
    {
        var alumno = new Alumno { Id = Juan, Nombre = "Juan", Apellido = "Pérez", Telefono = "1", Arancel = 30_000m };
        _delMes.Add(new Cargo { AlumnoId = Juan, Alumno = alumno, Tipo = TipoCargo.Cuota, Concepto = "Cuota", Monto = 30_000m, Fecha = new DateOnly(2026, 7, 1) });

        var liq = await _service.ObtenerMesAsync(2026, 7);

        var juan = Assert.Single(liq.Liquidaciones);
        Assert.Equal(30_000m, juan.Total);
        Assert.Equal(30_000m, juan.Saldo);
        Assert.True(juan.CuotaDefinida);
        Assert.Equal(30_000m, liq.TotalFacturado);
        Assert.Equal(30_000m, liq.TotalPendiente);
    }

    // ─────────────────────────────────────────────
    // Editar el monto al cobrar (ajustar la cuota del mes)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task EditarMonto_CargoImpago_CambiaElMonto_YPersiste()
    {
        var cargo = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Cuota, Concepto = "Cuota", Monto = 30_000m, Fecha = new DateOnly(2026, 7, 1) };
        _cargos.Setup(c => c.ObtenerAsync(cargo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cargo);

        var res = await _service.EditarMontoCargoAsync(cargo.Id, 25_000m);

        Assert.Equal(25_000m, cargo.Monto);
        Assert.Equal(25_000m, res.Monto);
        _cargos.Verify(c => c.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EditarMonto_CargoPagado_Lanza()
    {
        var cargo = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Cuota, Concepto = "Cuota", Monto = 30_000m, Fecha = new DateOnly(2026, 7, 1), PagadoEl = DateTime.UtcNow };
        _cargos.Setup(c => c.ObtenerAsync(cargo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cargo);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EditarMontoCargoAsync(cargo.Id, 25_000m));
    }

    [Fact]
    public async Task AgregarCargoManual_Cuota_Lanza()
    {
        // La cuota se genera sola: a mano solo Producto o Ajuste
        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AgregarCargoManualAsync(
            new CreateCargoManualDto { AlumnoId = Juan, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 1_000m }));
    }

    // ─────────────────────────────────────────────
    // Pagos
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PagarMes_SaldaTodosLosImpagosDelAlumno_YSoloEsos()
    {
        var pagadoViejo = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 1), PagadoEl = DateTime.UtcNow.AddDays(-5), MedioPago = MedioPago.Efectivo };
        var impago1 = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 1) };
        var impago2 = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Producto, Concepto = "Encordado", Monto = 12_000m, Fecha = new DateOnly(2026, 7, 15) };
        var deOtroAlumno = new Cargo { AlumnoId = Sofia, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 1) };
        _delMes.AddRange([pagadoViejo, impago1, impago2, deOtroAlumno]);

        await _service.PagarMesAsync(Juan, 2026, 7, MedioPago.Transferencia);

        Assert.NotNull(impago1.PagadoEl);
        Assert.NotNull(impago2.PagadoEl);
        Assert.Equal(MedioPago.Transferencia, impago1.MedioPago);
        Assert.Null(deOtroAlumno.PagadoEl);                       // Sofía no paga lo de Juan
        Assert.Equal(MedioPago.Efectivo, pagadoViejo.MedioPago);  // lo ya pagado no se toca
        _cargos.Verify(c => c.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PagarMes_SinImpagos_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.PagarMesAsync(Juan, 2026, 7, MedioPago.Efectivo));
    }

    [Fact]
    public async Task PagarCargo_MarcaFechaDelServerYMedio()
    {
        var cargo = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 16_000m, Fecha = new DateOnly(2026, 7, 1) };
        _cargos.Setup(c => c.ObtenerAsync(cargo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cargo);

        await _service.PagarCargoAsync(cargo.Id, MedioPago.Efectivo);

        Assert.NotNull(cargo.PagadoEl);
        Assert.Equal(MedioPago.Efectivo, cargo.MedioPago);
        _cargos.Verify(c => c.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PagarCargo_YaPagado_Lanza()
    {
        var cargo = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 16_000m, Fecha = new DateOnly(2026, 7, 1), PagadoEl = DateTime.UtcNow };
        _cargos.Setup(c => c.ObtenerAsync(cargo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cargo);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.PagarCargoAsync(cargo.Id, MedioPago.Efectivo));
    }

    // ─────────────────────────────────────────────
    // Pago informado (portal): el alumno avisa, el profe confirma/rechaza
    // ─────────────────────────────────────────────

    [Fact]
    public async Task InformarMes_MarcaImpagosNoInformados_SinTocarPagadoEl()
    {
        var impago1 = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 1) };
        var impago2 = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Producto, Concepto = "Encordado", Monto = 12_000m, Fecha = new DateOnly(2026, 7, 15) };
        var pagado = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 1), PagadoEl = DateTime.UtcNow };
        var deOtro = new Cargo { AlumnoId = Sofia, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 1) };
        _delMes.AddRange([impago1, impago2, pagado, deOtro]);

        await _service.InformarPagoMesAsync(Juan, 2026, 7);

        Assert.NotNull(impago1.PagoInformadoEl);
        Assert.NotNull(impago2.PagoInformadoEl);
        Assert.Null(impago1.PagadoEl);            // sigue IMPAGO: el profe todavía no confirmó
        Assert.Null(pagado.PagoInformadoEl);      // lo ya pagado no se informa
        Assert.Null(deOtro.PagoInformadoEl);      // lo de Sofía no es de Juan
        _cargos.Verify(c => c.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InformarMes_SinCargosPorInformar_Lanza()
    {
        var yaInformado = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 1), PagoInformadoEl = DateTime.UtcNow };
        _delMes.Add(yaInformado);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.InformarPagoMesAsync(Juan, 2026, 7));
    }

    [Fact]
    public async Task InformarCargo_DeOtroAlumno_Lanza()
    {
        var ajeno = new Cargo { AlumnoId = Sofia, Tipo = TipoCargo.Producto, Concepto = "Encordado", Monto = 12_000m, Fecha = new DateOnly(2026, 7, 15) };
        _cargos.Setup(c => c.ObtenerAsync(ajeno.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ajeno);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.InformarPagoCargoAsync(Juan, ajeno.Id));

        Assert.Null(ajeno.PagoInformadoEl);
    }

    [Fact]
    public async Task Rechazar_VuelveElInformadoAImpago_SinConfirmar()
    {
        var informado = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 1), PagoInformadoEl = DateTime.UtcNow };
        _cargos.Setup(c => c.ObtenerAsync(informado.Id, It.IsAny<CancellationToken>())).ReturnsAsync(informado);

        await _service.RechazarPagoCargoAsync(informado.Id);

        Assert.Null(informado.PagoInformadoEl); // vuelve a "sin informar"
        Assert.Null(informado.PagadoEl);        // nunca se dio por pagado
    }

    [Fact]
    public async Task Estado_TodoElSaldoInformado_EsInformado_YNoCuentaComoVencido()
    {
        var alumno = new Alumno { Id = Juan, Nombre = "Juan", Apellido = "Pérez", Telefono = "1", Arancel = 4_000m };
        var informado = new Cargo { AlumnoId = Juan, Alumno = alumno, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 3), PagoInformadoEl = DateTime.UtcNow };
        _delMes.Add(informado);

        var liq = await _service.ObtenerMesAsync(2026, 7);

        var juan = Assert.Single(liq.Liquidaciones);
        Assert.Equal("Informado", juan.Estado);
        Assert.Equal(0, liq.AlumnosVencidos);
        Assert.True(juan.Cargos[0].PagoInformado);
    }

    // ─────────────────────────────────────────────
    // Morosidad: nadie toma clases NUEVAS con la cuota vencida (estáticos, sin cambio)
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData("2026-06-15", "2026-07-05", true)]  // impago de junio visto en julio: vencido
    [InlineData("2026-07-03", "2026-07-05", false)] // impago del mes en curso antes del 10: todavía no
    [InlineData("2026-07-03", "2026-07-11", true)]  // pasó el día 10 sin pagar: vencido
    public void TieneDeudaVencida_RespetaElDia10DelMesDelCargo(string fechaCargo, string hoyIso, bool esperado)
    {
        var impagos = new[]
        {
            new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 4_000m, Fecha = DateOnly.Parse(fechaCargo) },
        };

        Assert.Equal(esperado, CuotaService.TieneDeudaVencida(impagos, DateOnly.Parse(hoyIso)));
    }

    [Fact]
    public void TieneDeudaVencida_SinImpagos_EsFalse()
    {
        Assert.False(CuotaService.TieneDeudaVencida([], new DateOnly(2026, 7, 20)));
    }

    [Theory]
    [InlineData("2026-07-03", "2026-07-12", false)] // vencido (pasó el 10) pero con días de gracia
    [InlineData("2026-07-03", "2026-07-15", false)] // el 15 exacto todavía no: recién a partir del 16
    [InlineData("2026-07-03", "2026-07-16", true)]  // pasó el 15 sin pagar: sacable del calendario
    [InlineData("2026-06-20", "2026-07-05", true)]  // impago de junio: el 15 de junio quedó atrás
    public void DebeSuspenderse_RespetaElDia15DelMesDelCargo(string fechaCargo, string hoyIso, bool esperado)
    {
        var impagos = new[]
        {
            new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Cuota, Concepto = "x", Monto = 4_000m, Fecha = DateOnly.Parse(fechaCargo) },
        };

        Assert.Equal(esperado, CuotaService.DebeSuspenderse(impagos, DateOnly.Parse(hoyIso)));
    }

    [Fact]
    public void DebeSuspenderse_SinImpagos_EsFalse()
    {
        Assert.False(CuotaService.DebeSuspenderse([], new DateOnly(2026, 7, 30)));
    }

    [Theory]
    [InlineData(0, "2026-07-05", "Pagada")]       // sin saldo → Pagada
    [InlineData(4000, "2026-07-05", "Pendiente")] // debe, y está entre el 1 y el 10
    [InlineData(4000, "2026-07-10", "Pendiente")] // el 10 todavía no venció
    [InlineData(4000, "2026-07-11", "Vencida")]   // el 11 sí
    [InlineData(4000, "2026-08-01", "Vencida")]   // mes siguiente, sigue debiendo julio
    [InlineData(4000, "2026-06-20", "Pendiente")] // mes futuro visto desde junio: aún no vence
    public void CalcularEstado_RespetaElDia10(decimal saldo, string hoyIso, string esperado)
    {
        var estado = CuotaService.CalcularEstado(2026, 7, saldo, DateOnly.Parse(hoyIso));

        Assert.Equal(esperado, estado);
    }
}
