using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Las reglas de la PLATA (modelo-precios.md + ADR-0009), fijadas por TDD:
/// grupal ÷ asignados (el ausente paga igual), individual entera, cancelado
/// sin cargo, idempotencia, pagos por mes y por cargo, estado calculado.
/// </summary>
public class CuotaServiceTests
{
    private const decimal ValorBase = 16_000m; // el precio real del profe

    private static readonly Guid Juan = Guid.NewGuid();
    private static readonly Guid Sofia = Guid.NewGuid();
    private static readonly Guid Mateo = Guid.NewGuid();
    private static readonly Guid Vale = Guid.NewGuid();

    private readonly Mock<ICargoRepository> _cargos;
    private readonly Mock<ITurnoRepository> _turnos;
    private readonly Mock<ITenantRepository> _tenant;
    private readonly Mock<ITurnoService> _turnoService;
    private readonly CuotaService _service;
    private readonly List<Cargo> _agregados = [];

    public CuotaServiceTests()
    {
        _cargos = new Mock<ICargoRepository>();
        _turnos = new Mock<ITurnoRepository>();
        _tenant = new Mock<ITenantRepository>();
        _turnoService = new Mock<ITurnoService>();
        _service = new CuotaService(_cargos.Object, _turnos.Object, _tenant.Object, _turnoService.Object);

        // Tenant con los precios configurados (base real: $16.000)
        _tenant.Setup(t => t.ObtenerActualAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Tenant
               {
                   Subdominio = "demo",
                   Nombre = "Club Demo",
                   ValorHoraGrupal = ValorBase,
                   ValorClaseIndividual = ValorBase,
               });

        // Por defecto: sin turnos ni cargos previos; AgregarAsync captura
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _cargos.Setup(c => c.ListarDelMesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => [.. _agregados]);
        _cargos.Setup(c => c.AgregarAsync(It.IsAny<Cargo>(), It.IsAny<CancellationToken>()))
               .Callback((Cargo c, CancellationToken _) => _agregados.Add(c))
               .Returns(Task.CompletedTask);
    }

    // ── Helpers para armar turnos ──

    private static Turno TurnoGrupal(params (Guid alumnoId, bool presente)[] roster)
    {
        var turno = new Turno
        {
            HorarioId = Guid.NewGuid(),
            CanchaId = Guid.NewGuid(),
            Fecha = new DateOnly(2026, 7, 14),
            HoraInicio = new TimeOnly(18, 0),
            DuracionMinutos = 60,
            Horario = new Horario
            {
                CanchaId = Guid.NewGuid(),
                GrupoId = Guid.NewGuid(),
                Grupo = new Grupo { Nombre = "Intermedios" },
                Dia = DayOfWeek.Tuesday,
                HoraInicio = new TimeOnly(18, 0),
                DuracionMinutos = 60,
            },
        };
        foreach (var (alumnoId, presente) in roster)
            turno.Participantes.Add(new TurnoParticipante { Turno = turno, AlumnoId = alumnoId, Presente = presente });
        return turno;
    }

    private static Turno TurnoIndividual(Guid alumnoId)
    {
        var turno = new Turno
        {
            HorarioId = Guid.NewGuid(),
            CanchaId = Guid.NewGuid(),
            Fecha = new DateOnly(2026, 7, 16),
            HoraInicio = new TimeOnly(10, 0),
            DuracionMinutos = 60,
            Horario = new Horario
            {
                CanchaId = Guid.NewGuid(),
                AlumnoId = alumnoId,
                Dia = DayOfWeek.Thursday,
                HoraInicio = new TimeOnly(10, 0),
                DuracionMinutos = 60,
            },
        };
        turno.Participantes.Add(new TurnoParticipante { Turno = turno, AlumnoId = alumnoId });
        return turno;
    }

    // ─────────────────────────────────────────────
    // Generación de cargos de clase
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Mes_ClaseGrupal_DivideEntreAsignados_YElAusentePagaIgual()
    {
        // Grupo de 4 asignados, Mateo FALTÓ: igual son 4 cargos de $4.000
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([TurnoGrupal((Juan, true), (Sofia, true), (Mateo, false), (Vale, true))]);

        await _service.ObtenerMesAsync(2026, 7);

        Assert.Equal(4, _agregados.Count);
        Assert.All(_agregados, c => Assert.Equal(4_000m, c.Monto)); // 16.000 ÷ 4
        Assert.Contains(_agregados, c => c.AlumnoId == Mateo);      // el ausente TAMBIÉN debe
        Assert.All(_agregados, c => Assert.Equal(TipoCargo.Clase, c.Tipo));
    }

    [Fact]
    public async Task Mes_ClaseIndividual_CargaElValorEntero()
    {
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([TurnoIndividual(Juan)]);

        await _service.ObtenerMesAsync(2026, 7);

        var cargo = Assert.Single(_agregados);
        Assert.Equal(ValorBase, cargo.Monto); // $16.000 enteros
        Assert.Equal(Juan, cargo.AlumnoId);
    }

    [Fact]
    public async Task Mes_ClaseIndividual_De30Min_ProrrateaPorDuracion()
    {
        // El precio configurado es POR HORA: 30' = la mitad
        var turno = TurnoIndividual(Juan);
        turno.DuracionMinutos = 30;
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([turno]);

        await _service.ObtenerMesAsync(2026, 7);

        var cargo = Assert.Single(_agregados);
        Assert.Equal(8_000m, cargo.Monto); // 16.000 × (30 ÷ 60)
    }

    [Fact]
    public async Task Mes_ClaseGrupal_De90Min_ProrrateaPorDuracion_YDivideEntreAsignados()
    {
        // Hora y media de grupo de 4: 16.000 × 1,5 ÷ 4 = $6.000 cada uno
        var turno = TurnoGrupal((Juan, true), (Sofia, true), (Mateo, true), (Vale, true));
        turno.DuracionMinutos = 90;
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([turno]);

        await _service.ObtenerMesAsync(2026, 7);

        Assert.Equal(4, _agregados.Count);
        Assert.All(_agregados, c => Assert.Equal(6_000m, c.Monto));
    }

    [Fact]
    public async Task Mes_MaterializaLosTurnosDelMes_AntesDeLiquidar()
    {
        // Cuotas no depende de que hayas visitado el Calendario: genera lo que falte
        await _service.ObtenerMesAsync(2026, 7);

        _turnoService.Verify(
            s => s.GenerarTurnosDelMesAsync(2026, 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Mes_TurnoCancelado_NoGeneraCargo()
    {
        var cancelado = TurnoGrupal((Juan, true), (Sofia, true));
        cancelado.Estado = EstadoTurno.Cancelado;
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([cancelado]);

        await _service.ObtenerMesAsync(2026, 7);

        Assert.Empty(_agregados); // la clase no ocurrió → nadie paga
    }

    [Fact]
    public async Task Mes_EsIdempotente_NoDuplicaCargosExistentes()
    {
        var turno = TurnoGrupal((Juan, true), (Sofia, true));
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([turno]);
        // El cargo de Juan por ese turno YA existe
        _agregados.Add(new Cargo
        {
            AlumnoId = Juan,
            TurnoId = turno.Id,
            Tipo = TipoCargo.Clase,
            Concepto = "Clase grupal",
            Monto = 8_000m,
            Fecha = turno.Fecha,
        });

        await _service.ObtenerMesAsync(2026, 7);

        // Solo se agregó el de Sofía (el de Juan no se duplica)
        Assert.Equal(2, _agregados.Count);
        Assert.Single(_agregados, c => c.AlumnoId == Juan);
    }

    [Fact]
    public async Task Mes_SinPreciosConfigurados_Lanza()
    {
        _tenant.Setup(t => t.ObtenerActualAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Tenant { Subdominio = "demo", Nombre = "Club Demo" }); // sin precios
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([TurnoGrupal((Juan, true))]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.ObtenerMesAsync(2026, 7));
    }

    // ─────────────────────────────────────────────
    // Pagos
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PagarMes_SaldaTodosLosImpagosDelAlumno_YSoloEsos()
    {
        var pagadoViejo = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 7), PagadoEl = DateTime.UtcNow.AddDays(-5), MedioPago = MedioPago.Efectivo };
        var impago1 = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 14) };
        var impago2 = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Producto, Concepto = "Encordado", Monto = 12_000m, Fecha = new DateOnly(2026, 7, 15) };
        var deOtroAlumno = new Cargo { AlumnoId = Sofia, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 14) };
        _agregados.AddRange([pagadoViejo, impago1, impago2, deOtroAlumno]);

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
        var cargo = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 16_000m, Fecha = new DateOnly(2026, 7, 16) };
        _cargos.Setup(c => c.ObtenerAsync(cargo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cargo);

        await _service.PagarCargoAsync(cargo.Id, MedioPago.Efectivo);

        Assert.NotNull(cargo.PagadoEl);
        Assert.Equal(MedioPago.Efectivo, cargo.MedioPago);
        _cargos.Verify(c => c.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PagarCargo_YaPagado_Lanza()
    {
        var cargo = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 16_000m, Fecha = new DateOnly(2026, 7, 16), PagadoEl = DateTime.UtcNow };
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
        var impago1 = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 14) };
        var impago2 = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Producto, Concepto = "Encordado", Monto = 12_000m, Fecha = new DateOnly(2026, 7, 15) };
        var pagado = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 7), PagadoEl = DateTime.UtcNow };
        var deOtro = new Cargo { AlumnoId = Sofia, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 14) };
        _agregados.AddRange([impago1, impago2, pagado, deOtro]);

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
        // Todo ya está informado: no hay nada nuevo que avisar
        var yaInformado = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 14), PagoInformadoEl = DateTime.UtcNow };
        _agregados.Add(yaInformado);

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
    public async Task InformarCargo_Propio_LoMarca()
    {
        var mio = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Producto, Concepto = "Encordado", Monto = 12_000m, Fecha = new DateOnly(2026, 7, 15) };
        _cargos.Setup(c => c.ObtenerAsync(mio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mio);

        await _service.InformarPagoCargoAsync(Juan, mio.Id);

        Assert.NotNull(mio.PagoInformadoEl);
        Assert.Null(mio.PagadoEl); // informado ≠ pagado
    }

    [Fact]
    public async Task Rechazar_VuelveElInformadoAImpago_SinConfirmar()
    {
        var informado = new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 14), PagoInformadoEl = DateTime.UtcNow };
        _cargos.Setup(c => c.ObtenerAsync(informado.Id, It.IsAny<CancellationToken>())).ReturnsAsync(informado);

        await _service.RechazarPagoCargoAsync(informado.Id);

        Assert.Null(informado.PagoInformadoEl); // vuelve a "sin informar"
        Assert.Null(informado.PagadoEl);        // nunca se dio por pagado
    }

    [Fact]
    public async Task Estado_TodoElSaldoInformado_EsInformado_YNoCuentaComoVencido()
    {
        // Impago de junio (vencido) pero el alumno ya avisó que transfirió:
        // el estado tapa a "Vencida" con "Informado" (hay acción del profe pendiente)
        var informado = new Cargo { AlumnoId = Juan, Alumno = new Alumno { Id = Juan, Nombre = "Juan", Apellido = "Pérez", Dni = "1", Telefono = "1", FechaNacimiento = DateTime.UtcNow.AddYears(-30) }, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m, Fecha = new DateOnly(2026, 7, 3), PagoInformadoEl = DateTime.UtcNow };
        _cargos.Setup(c => c.ListarDelMesAsync(2026, 7, It.IsAny<CancellationToken>()))
               .ReturnsAsync([informado]);

        var liq = await _service.ObtenerMesAsync(2026, 7);

        var juan = Assert.Single(liq.Liquidaciones);
        Assert.Equal("Informado", juan.Estado);
        Assert.Equal(0, liq.AlumnosVencidos);
        Assert.True(juan.Cargos[0].PagoInformado);
    }

    // ─────────────────────────────────────────────
    // Morosidad: nadie toma clases NUEVAS con la cuota vencida
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData("2026-06-15", "2026-07-05", true)]  // impago de junio visto en julio: vencido
    [InlineData("2026-07-03", "2026-07-05", false)] // impago del mes en curso antes del 10: todavía no
    [InlineData("2026-07-03", "2026-07-11", true)]  // pasó el día 10 sin pagar: vencido
    public void TieneDeudaVencida_RespetaElDia10DelMesDelCargo(string fechaCargo, string hoyIso, bool esperado)
    {
        var impagos = new[]
        {
            new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m, Fecha = DateOnly.Parse(fechaCargo) },
        };

        Assert.Equal(esperado, CuotaService.TieneDeudaVencida(impagos, DateOnly.Parse(hoyIso)));
    }

    [Fact]
    public void TieneDeudaVencida_SinImpagos_EsFalse()
    {
        Assert.False(CuotaService.TieneDeudaVencida([], new DateOnly(2026, 7, 20)));
    }

    // ─────────────────────────────────────────────
    // Morosidad DURA (día 15): el profe puede sacarlo del calendario
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData("2026-07-03", "2026-07-12", false)] // vencido (pasó el 10) pero con días de gracia
    [InlineData("2026-07-03", "2026-07-15", false)] // el 15 exacto todavía no: recién a partir del 16
    [InlineData("2026-07-03", "2026-07-16", true)]  // pasó el 15 sin pagar: sacable del calendario
    [InlineData("2026-06-20", "2026-07-05", true)]  // impago de junio: el 15 de junio quedó atrás
    public void DebeSuspenderse_RespetaElDia15DelMesDelCargo(string fechaCargo, string hoyIso, bool esperado)
    {
        var impagos = new[]
        {
            new Cargo { AlumnoId = Juan, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m, Fecha = DateOnly.Parse(fechaCargo) },
        };

        Assert.Equal(esperado, CuotaService.DebeSuspenderse(impagos, DateOnly.Parse(hoyIso)));
    }

    [Fact]
    public void DebeSuspenderse_SinImpagos_EsFalse()
    {
        Assert.False(CuotaService.DebeSuspenderse([], new DateOnly(2026, 7, 30)));
    }

    // ─────────────────────────────────────────────
    // Estado calculado (contra el día 10 — nunca almacenado)
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData(0, "2026-07-05", "Pagada")]    // sin saldo → Pagada
    [InlineData(4000, "2026-07-05", "Pendiente")] // debe, y está entre el 1 y el 10
    [InlineData(4000, "2026-07-10", "Pendiente")] // el 10 todavía no venció
    [InlineData(4000, "2026-07-11", "Vencida")]   // el 11 sí
    [InlineData(4000, "2026-08-01", "Vencida")]   // mes siguiente, sigue debiendo julio
    [InlineData(4000, "2026-06-20", "Pendiente")] // mes futuro visto desde junio: aún no vence
    public void CalcularEstado_RespetaElDia10(decimal saldo, string hoyIso, string esperado)
    {
        var hoy = DateOnly.Parse(hoyIso);

        var estado = CuotaService.CalcularEstado(2026, 7, saldo, hoy);

        Assert.Equal(esperado, estado);
    }
}
