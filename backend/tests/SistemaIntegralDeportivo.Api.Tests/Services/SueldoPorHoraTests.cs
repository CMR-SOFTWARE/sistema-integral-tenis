using Moq;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// El cálculo del sueldo (G3): valor hora × horas dadas. El valor sale del
/// override del horario o del valor base del empleado; solo cuentan las clases
/// DADAS (Programado); el dueño no genera sueldo (no es empleado).
/// </summary>
public class SueldoPorHoraTests
{
    private readonly Mock<ITurnoService> _turnoService = new();
    private readonly Mock<ITurnoRepository> _turnos = new();
    private readonly Mock<IMembresiaTenantRepository> _membresias = new();
    private readonly SueldoPorHora _politica;

    private readonly List<TurnoAgenda> _delMes = [];
    private readonly List<(MembresiaTenant, Usuario)> _empleados = [];

    public SueldoPorHoraTests()
    {
        _politica = new SueldoPorHora(_turnoService.Object, _turnos.Object, _membresias.Object);

        _turnoService.Setup(s => s.GenerarTurnosDelMesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => [.. _delMes]);
        _membresias.Setup(m => m.ListarConUsuarioAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(() => [.. _empleados]);
    }

    // ── Helpers ──

    private void Empleado(Guid userId, decimal? valorHora = null, bool activo = true)
    {
        var m = new MembresiaTenant { UserId = userId, ValorHora = valorHora, Activo = activo };
        var u = new Usuario { Id = userId, Nombre = "Profe", Apellido = userId.ToString()[..4], UserName = "u" };
        _empleados.Add((m, u));
    }

    private static Horario HorarioDe(Guid profe, int dur, decimal? valorHoraProfe = null, DayOfWeek dia = DayOfWeek.Monday) =>
        new()
        {
            CanchaId = Guid.NewGuid(),
            ProfesorUserId = profe,
            ValorHoraProfe = valorHoraProfe,
            Dia = dia,
            HoraInicio = new TimeOnly(18, 0),
            DuracionMinutos = dur,
        };

    // El repositorio ya no devuelve la entidad sino el turno proyectado; el Horario
    // se sigue usando acá como fuente de los datos de la clase.
    private void Turno(Horario h, EstadoTurno estado = EstadoTurno.Programado, int diaMes = 6) =>
        _delMes.Add(TurnosDePrueba.Agenda(
            fecha: new DateOnly(2026, 7, diaMes),
            hora: h.HoraInicio,
            duracion: h.DuracionMinutos,
            estado: estado,
            horarioId: h.Id,
            profesorUserId: h.ProfesorUserId,
            valorHoraProfe: h.ValorHoraProfe,
            horarioDia: h.Dia,
            horarioHoraInicio: h.HoraInicio));

    // ─────────────────────────────────────────────

    [Fact]
    public async Task Calcular_MaterializaElMesAntesDeSumar()
    {
        await _politica.CalcularDelMesAsync(2026, 7);

        _turnoService.Verify(s => s.GenerarTurnosDelMesAsync(2026, 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Suma_ValorHoraPorHoras_DeLasClasesDadas()
    {
        var profe = Guid.NewGuid();
        Empleado(profe, valorHora: 8_000m);
        var h = HorarioDe(profe, dur: 60);
        Turno(h, diaMes: 6);
        Turno(h, diaMes: 13); // dos clases del mismo horario en el mes

        var res = await _politica.CalcularDelMesAsync(2026, 7);

        var sueldo = Assert.Single(res);
        Assert.Equal(16_000m, sueldo.Monto);       // 8000 × 2 horas
        Assert.Equal(2m, sueldo.HorasTotales);
        Assert.True(sueldo.TieneValorHora);
        var linea = Assert.Single(sueldo.Detalle);
        Assert.Equal(2, linea.Clases);
    }

    [Fact]
    public async Task Cancelado_NoSuma()
    {
        var profe = Guid.NewGuid();
        Empleado(profe, valorHora: 8_000m);
        var h = HorarioDe(profe, dur: 60);
        Turno(h, EstadoTurno.Programado, diaMes: 6);
        Turno(h, EstadoTurno.Cancelado, diaMes: 13); // esta no se paga

        var res = await _politica.CalcularDelMesAsync(2026, 7);

        var sueldo = Assert.Single(res);
        Assert.Equal(8_000m, sueldo.Monto);   // solo la clase dada
        Assert.Equal(1m, sueldo.HorasTotales);
    }

    [Fact]
    public async Task OverrideDelHorario_PisaLaBaseDelProfe()
    {
        var profe = Guid.NewGuid();
        Empleado(profe, valorHora: 8_000m);
        var menores = HorarioDe(profe, dur: 60, valorHoraProfe: 5_000m); // menores, paga menos
        Turno(menores);

        var res = await _politica.CalcularDelMesAsync(2026, 7);

        var sueldo = Assert.Single(res);
        Assert.Equal(5_000m, sueldo.Monto); // usa el override, no la base
        Assert.True(sueldo.TieneValorHora);
    }

    [Fact]
    public async Task SinTarifa_DaCeroYMarcaSinValorHora()
    {
        var profe = Guid.NewGuid();
        Empleado(profe, valorHora: null); // sin base
        var h = HorarioDe(profe, dur: 60, valorHoraProfe: null); // sin override
        Turno(h);

        var res = await _politica.CalcularDelMesAsync(2026, 7);

        var sueldo = Assert.Single(res);
        Assert.Equal(0m, sueldo.Monto);
        Assert.False(sueldo.TieneValorHora); // el profe lo ve marcado para completar
    }

    [Fact]
    public async Task ElDueño_NoGeneraSueldo()
    {
        // El dueño no está en las membresías; su clase no debe crear un sueldo.
        var dueño = Guid.NewGuid();
        var h = HorarioDe(dueño, dur: 60, valorHoraProfe: 9_000m);
        Turno(h);

        var res = await _politica.CalcularDelMesAsync(2026, 7);

        Assert.Empty(res);
    }

    [Fact]
    public async Task HorasMixtas_60y90_SumanBien()
    {
        var profe = Guid.NewGuid();
        Empleado(profe, valorHora: 8_000m);
        Turno(HorarioDe(profe, dur: 60, dia: DayOfWeek.Monday));
        Turno(HorarioDe(profe, dur: 90, dia: DayOfWeek.Wednesday));

        var res = await _politica.CalcularDelMesAsync(2026, 7);

        var sueldo = Assert.Single(res);
        Assert.Equal(2.5m, sueldo.HorasTotales);   // 1h + 1.5h
        Assert.Equal(20_000m, sueldo.Monto);       // 8000 + 12000
        Assert.Equal(2, sueldo.Detalle.Count);
    }

    [Fact]
    public async Task ExEmpleadoSinClases_NoAparece_ActivoSinClases_ApareceEnCero()
    {
        var exProfe = Guid.NewGuid();
        var activo = Guid.NewGuid();
        Empleado(exProfe, valorHora: 8_000m, activo: false);
        Empleado(activo, valorHora: 8_000m, activo: true);
        // ninguno tiene clases este mes

        var res = await _politica.CalcularDelMesAsync(2026, 7);

        var sueldo = Assert.Single(res);          // solo el activo
        Assert.Equal(activo, sueldo.UserId);
        Assert.Equal(0m, sueldo.Monto);
    }
}
