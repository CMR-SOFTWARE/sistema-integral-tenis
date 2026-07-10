using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reglas de turnos (TDD): generación perezosa e idempotente con roster
/// congelado, asistencia default-presente y cancelación con motivo.
/// </summary>
public class TurnoServiceTests
{
    private static readonly DateOnly Lunes = new(2026, 7, 13); // lunes 13/07/2026
    private static readonly Guid HorarioId = Guid.NewGuid();
    private static readonly Guid GrupoId = Guid.NewGuid();
    private static readonly Guid AlumnoJuan = Guid.NewGuid();
    private static readonly Guid AlumnaSofia = Guid.NewGuid();
    private static readonly Guid AlumnoDeBaja = Guid.NewGuid();

    private readonly Mock<ITurnoRepository> _turnos;
    private readonly Mock<IHorarioRepository> _horarios;
    private readonly Mock<IGrupoRepository> _grupos;
    private readonly Mock<ICargoRepository> _cargos;
    private readonly TurnoService _service;

    public TurnoServiceTests()
    {
        _turnos = new Mock<ITurnoRepository>();
        _horarios = new Mock<IHorarioRepository>();
        _grupos = new Mock<IGrupoRepository>();
        _cargos = new Mock<ICargoRepository>();
        _service = new TurnoService(_turnos.Object, _horarios.Object, _grupos.Object, _cargos.Object);

        // Por defecto: nadie debe nada
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

        // Horario grupal: martes 18:00, 60'
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([HorarioGrupal()]);

        // Sin turnos generados todavía
        _turnos.Setup(t => t.FechasGeneradasAsync(HorarioId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

        // El grupo tiene 2 miembros activos y 1 dado de baja
        _grupos.Setup(g => g.ObtenerAsync(GrupoId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(GrupoConMiembros());
    }

    private static Horario HorarioGrupal() => new()
    {
        Id = HorarioId,
        CanchaId = Guid.NewGuid(),
        GrupoId = GrupoId,
        Dia = DayOfWeek.Tuesday,
        HoraInicio = new TimeOnly(18, 0),
        DuracionMinutos = 60,
    };

    private static Grupo GrupoConMiembros() => new()
    {
        Id = GrupoId,
        Nombre = "Intermedios",
        Alumnos =
        [
            new AlumnoGrupo { GrupoId = GrupoId, AlumnoId = AlumnoJuan, FechaBaja = null },
            new AlumnoGrupo { GrupoId = GrupoId, AlumnoId = AlumnaSofia, FechaBaja = null },
            new AlumnoGrupo { GrupoId = GrupoId, AlumnoId = AlumnoDeBaja, FechaBaja = DateTime.UtcNow.AddMonths(-1) },
        ],
    };

    // ─────────────────────────────────────────────
    // Generación perezosa: roster congelado e idempotencia
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Semana_GeneraElTurnoDelMartesConRosterDeActivos()
    {
        Turno? generado = null;
        _turnos.Setup(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
               .Callback((Turno t, CancellationToken _) => generado = t)
               .Returns(Task.CompletedTask);

        await _service.ObtenerSemanaAsync(Lunes);

        Assert.NotNull(generado);
        Assert.Equal(Lunes.AddDays(1), generado!.Fecha); // martes 14/07
        Assert.Equal(new TimeOnly(18, 0), generado.HoraInicio);
        // Roster: SOLO los miembros activos (el de baja no juega)
        Assert.Equal(2, generado.Participantes.Count);
        Assert.All(generado.Participantes, p => Assert.True(p.Presente)); // default presente
        Assert.DoesNotContain(generado.Participantes, p => p.AlumnoId == AlumnoDeBaja);
    }

    [Fact]
    public async Task Semana_EsIdempotente_NoRegeneraLoQueYaExiste()
    {
        // El turno del martes ya fue generado antes
        _turnos.Setup(t => t.FechasGeneradasAsync(HorarioId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([Lunes.AddDays(1)]);

        await _service.ObtenerSemanaAsync(Lunes);

        _turnos.Verify(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Semana_HorarioIndividual_ElRosterEsElAlumno()
    {
        var horario = HorarioGrupal();
        horario.GrupoId = null;
        horario.AlumnoId = AlumnoJuan; // clase individual
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([horario]);

        Turno? generado = null;
        _turnos.Setup(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
               .Callback((Turno t, CancellationToken _) => generado = t)
               .Returns(Task.CompletedTask);

        await _service.ObtenerSemanaAsync(Lunes);

        Assert.NotNull(generado);
        Assert.Single(generado!.Participantes);
        Assert.Equal(AlumnoJuan, generado.Participantes.First().AlumnoId);
    }

    [Fact]
    public async Task Semana_MarcaALosDeudoresEnElRoster()
    {
        // Turno con Juan y Sofía; Juan debe una clase de hace 2 meses (vencida)
        var turno = new Turno
        {
            HorarioId = HorarioId,
            Fecha = Lunes.AddDays(1),
            HoraInicio = new TimeOnly(18, 0),
            DuracionMinutos = 60,
        };
        turno.Participantes.Add(new TurnoParticipante { Turno = turno, AlumnoId = AlumnoJuan });
        turno.Participantes.Add(new TurnoParticipante { Turno = turno, AlumnoId = AlumnaSofia });
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([turno]);
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([new Cargo
               {
                   AlumnoId = AlumnoJuan, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m,
                   Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2),
               }]);

        var semana = await _service.ObtenerSemanaAsync(Lunes);

        var dto = Assert.Single(semana, t => t.Participantes.Count == 2);
        Assert.True(dto.Participantes.Single(p => p.AlumnoId == AlumnoJuan).DeudaVencida);
        Assert.False(dto.Participantes.Single(p => p.AlumnoId == AlumnaSofia).DeudaVencida);
    }

    // ─────────────────────────────────────────────
    // Generación por MES (la usa Cuotas para no depender del Calendario)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Mes_GeneraTodosLosTurnosDelMes_YSoloDelMes()
    {
        var generados = new List<Turno>();
        _turnos.Setup(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
               .Callback((Turno t, CancellationToken _) => generados.Add(t))
               .Returns(Task.CompletedTask);

        await _service.GenerarTurnosDelMesAsync(2026, 7);

        // Martes de julio 2026: 7, 14, 21 y 28 — nada de junio ni agosto
        Assert.Equal(4, generados.Count);
        Assert.All(generados, t => Assert.Equal(7, t.Fecha.Month));
        Assert.All(generados, t => Assert.Equal(DayOfWeek.Tuesday, t.Fecha.DayOfWeek));
        Assert.All(generados, t => Assert.Equal(2, t.Participantes.Count)); // roster congelado
        _turnos.Verify(t => t.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Mes_EsIdempotente_SoloGeneraLasFechasQueFaltan()
    {
        // El martes 14 ya tiene turno generado (lo materializó el Calendario)
        _turnos.Setup(t => t.FechasGeneradasAsync(HorarioId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([new DateOnly(2026, 7, 14)]);
        var generados = new List<Turno>();
        _turnos.Setup(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
               .Callback((Turno t, CancellationToken _) => generados.Add(t))
               .Returns(Task.CompletedTask);

        await _service.GenerarTurnosDelMesAsync(2026, 7);

        Assert.Equal(3, generados.Count); // 7, 21 y 28: el 14 no se duplica
        Assert.DoesNotContain(generados, t => t.Fecha == new DateOnly(2026, 7, 14));
    }

    // ─────────────────────────────────────────────
    // Asistencia (no mueve la plata) y cancelación
    // ─────────────────────────────────────────────

    private Turno TurnoConJuan()
    {
        var turno = new Turno
        {
            HorarioId = HorarioId,
            Fecha = Lunes.AddDays(1),
            HoraInicio = new TimeOnly(18, 0),
            DuracionMinutos = 60,
        };
        turno.Participantes.Add(new TurnoParticipante { Turno = turno, AlumnoId = AlumnoJuan, Presente = true });
        _turnos.Setup(t => t.ObtenerAsync(turno.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(turno);
        return turno;
    }

    [Fact]
    public async Task Asistencia_MarcaAusenteAlQueFalto()
    {
        var turno = TurnoConJuan();

        await _service.MarcarAsistenciaAsync(turno.Id, AlumnoJuan, presente: false);

        Assert.False(turno.Participantes.First().Presente);
        _turnos.Verify(t => t.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Asistencia_DeAlguienQueNoParticipa_Lanza()
    {
        var turno = TurnoConJuan();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.MarcarAsistenciaAsync(turno.Id, AlumnaSofia, presente: false));
    }

    [Fact]
    public async Task Cancelar_GuardaMotivoYFecha_SinBorrar()
    {
        var turno = TurnoConJuan();

        await _service.CancelarAsync(turno.Id, "Lluvia");

        Assert.Equal(EstadoTurno.Cancelado, turno.Estado);
        Assert.Equal("Lluvia", turno.CanceladoMotivo);
        Assert.NotNull(turno.CanceladoEl);
        _turnos.Verify(t => t.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancelar_UnTurnoYaCancelado_Lanza()
    {
        var turno = TurnoConJuan();
        turno.Estado = EstadoTurno.Cancelado;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarAsync(turno.Id, "otra vez"));
    }
}
