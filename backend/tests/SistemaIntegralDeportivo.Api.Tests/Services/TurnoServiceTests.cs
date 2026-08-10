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
    private readonly Mock<ICargoRepository> _cargos;
    private readonly Mock<IBloqueoRepository> _bloqueos;
    private readonly Mock<IUsuarioActual> _usuario;
    private readonly TurnoService _service;

    public TurnoServiceTests()
    {
        _turnos = new Mock<ITurnoRepository>();
        _horarios = new Mock<IHorarioRepository>();
        _cargos = new Mock<ICargoRepository>();
        _bloqueos = new Mock<IBloqueoRepository>();
        _usuario = new Mock<IUsuarioActual>(); // por defecto: no es staff → no filtra
        _service = new TurnoService(
            _turnos.Object, _horarios.Object, _cargos.Object, _bloqueos.Object, _usuario.Object);

        // Por defecto: nadie debe nada, sin cargos generados y sin bloqueos
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _cargos.Setup(c => c.ListarPorTurnosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _bloqueos.Setup(b => b.ListarAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);

        // Clase de martes 18:00, 60', con 2 alumnos activos y 1 dado de baja
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([HorarioGrupal()]);

        // Sin turnos generados todavía
        YaGeneradas();
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
    }

    /// <summary>Las fechas que la clase de prueba ya tiene generadas (vacío = ninguna).</summary>
    private void YaGeneradas(params DateOnly[] fechas) =>
        _turnos.Setup(t => t.FechasGeneradasAsync(
                   It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateOnly>(),
                   It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(fechas.ToLookup(_ => HorarioId, f => f));

    /// <summary>
    /// Una clase con su roster cargado. El repo real lo trae con el horario
    /// (Include(h => h.Alumnos).ThenInclude(ah => ah.Alumno)): el fixture hace lo mismo.
    /// </summary>
    private static Horario HorarioGrupal()
    {
        var horario = new Horario
        {
            Id = HorarioId,
            CanchaId = Guid.NewGuid(),
            Nombre = "Intermedios",
            Dia = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(18, 0),
            DuracionMinutos = 60,
            // Alta vieja: los tests de generación cubren el mes completo
            CreadoEl = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        horario.Alumnos = ConRoster(horario.Id);
        return horario;
    }

    /// <summary>2 alumnos activos y 1 que se fue hace un mes (no debe entrar al turno).</summary>
    private static List<AlumnoHorario> ConRoster(Guid horarioId) =>
    [
        new()
        {
            HorarioId = horarioId, AlumnoId = AlumnoJuan, FechaBaja = null,
            Alumno = AlumnoDePrueba(AlumnoJuan, "Juan"),
        },
        new()
        {
            HorarioId = horarioId, AlumnoId = AlumnaSofia, FechaBaja = null,
            Alumno = AlumnoDePrueba(AlumnaSofia, "Sofía"),
        },
        new()
        {
            HorarioId = horarioId, AlumnoId = AlumnoDeBaja, FechaBaja = DateTime.UtcNow.AddMonths(-1),
            Alumno = AlumnoDePrueba(AlumnoDeBaja, "Baja"),
        },
    ];

    /// <summary>
    /// Alumno de fixture. El roster mira `Alumno.Estado`, y el repo real
    /// carga esa navegación (`ObtenerAsync` hace ThenInclude(m => m.Alumno)):
    /// los fixtures tienen que traerla igual.
    /// </summary>
    private static Alumno AlumnoDePrueba(Guid id, string nombre, EstadoAlumno estado = EstadoAlumno.Activo) => new()
    {
        Id = id,
        Nombre = nombre,
        Apellido = "Prueba",
        Dni = id.ToString()[..8],
        Telefono = "+5491155550000",
        Estado = estado,
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

    /// <summary>
    /// La generación pregunta las fechas ya materializadas UNA sola vez, para todas las
    /// clases juntas. Preguntar de a una era N+1: con 46 clases eran 46 idas y vueltas,
    /// y a ~115 ms de red contra Supabase eso ponía la agenda en 5,5 segundos. Es un test
    /// de forma, no de resultado: contra la base local el bug no se nota.
    /// </summary>
    [Fact]
    public async Task Semana_PideLasFechasGeneradas_UnaSolaVezParaTodasLasClases()
    {
        var otra = HorarioGrupal();
        otra.Id = Guid.NewGuid();
        otra.Dia = DayOfWeek.Thursday;
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([HorarioGrupal(), otra]);

        await _service.ObtenerSemanaAsync(Lunes);

        _turnos.Verify(t => t.FechasGeneradasAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Semana_EsIdempotente_NoRegeneraLoQueYaExiste()
    {
        // El turno del martes ya fue generado antes
        YaGeneradas(Lunes.AddDays(1));

        await _service.ObtenerSemanaAsync(Lunes);

        _turnos.Verify(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Semana_ClaseDeUnSoloAlumno_ElRosterEsEl()
    {
        var horario = HorarioGrupal();
        horario.Alumnos =
        [
            new()
            {
                HorarioId = horario.Id, AlumnoId = AlumnoJuan,
                Alumno = AlumnoDePrueba(AlumnoJuan, "Juan"),
            },
        ];
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
        YaGeneradas(new DateOnly(2026, 7, 14));
        var generados = new List<Turno>();
        _turnos.Setup(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
               .Callback((Turno t, CancellationToken _) => generados.Add(t))
               .Returns(Task.CompletedTask);

        await _service.GenerarTurnosDelMesAsync(2026, 7);

        Assert.Equal(3, generados.Count); // 7, 21 y 28: el 14 no se duplica
        Assert.DoesNotContain(generados, t => t.Fecha == new DateOnly(2026, 7, 14));
    }

    [Fact]
    public async Task Semana_NoIncluyeAlumnosPausadosNiDeBaja_EnElRoster()
    {
        // El pausado no ocupa lugar (y no infla el divisor abaratando al resto)
        var horario = HorarioGrupal();
        horario.Alumnos.First(a => a.AlumnoId == AlumnaSofia).Alumno =
            AlumnoDePrueba(AlumnaSofia, "Sofía", EstadoAlumno.Suspendido);
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([horario]);

        Turno? generado = null;
        _turnos.Setup(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
               .Callback((Turno t, CancellationToken _) => generado = t)
               .Returns(Task.CompletedTask);

        await _service.ObtenerSemanaAsync(Lunes);

        Assert.NotNull(generado);
        Assert.Single(generado!.Participantes); // solo Juan
        Assert.DoesNotContain(generado.Participantes, p => p.AlumnoId == AlumnaSofia);
    }

    [Fact]
    public async Task Semana_ClaseDeUnSoloAlumnoPausado_NoGeneraTurno()
    {
        // Sin nadie que juegue no hay turno: la clase de a uno es el caso extremo
        // del roster vacío (antes era una rama aparte, la del horario individual).
        var horario = HorarioGrupal();
        horario.Alumnos =
        [
            new()
            {
                HorarioId = horario.Id, AlumnoId = AlumnoJuan,
                Alumno = AlumnoDePrueba(AlumnoJuan, "Juan", EstadoAlumno.Suspendido),
            },
        ];
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([horario]);

        await _service.ObtenerSemanaAsync(Lunes);

        _turnos.Verify(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Mes_NoGeneraTurnosAnterioresAlAltaDelHorario()
    {
        // Horario dado de alta el lunes 13/07: las clases del 7/07 no
        // existieron — un horario nuevo no genera (ni cobra) el pasado
        var horario = HorarioGrupal();
        horario.CreadoEl = new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([horario]);
        var generados = new List<Turno>();
        _turnos.Setup(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
               .Callback((Turno t, CancellationToken _) => generados.Add(t))
               .Returns(Task.CompletedTask);

        await _service.GenerarTurnosDelMesAsync(2026, 7);

        // Martes de julio: 7, 14, 21, 28 → solo desde el alta (14, 21, 28)
        Assert.Equal(3, generados.Count);
        Assert.DoesNotContain(generados, t => t.Fecha < new DateOnly(2026, 7, 13));
    }

    [Fact]
    public async Task Mes_ElMismoDiaDelAlta_SiSeGenera()
    {
        // Alta un martes: la clase de ESE martes sí va (fecha == alta)
        var horario = HorarioGrupal();
        horario.CreadoEl = new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc);
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([horario]);
        var generados = new List<Turno>();
        _turnos.Setup(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
               .Callback((Turno t, CancellationToken _) => generados.Add(t))
               .Returns(Task.CompletedTask);

        await _service.GenerarTurnosDelMesAsync(2026, 7);

        Assert.Contains(generados, t => t.Fecha == new DateOnly(2026, 7, 14));
    }

    // ─────────────────────────────────────────────
    // Bloqueos: la generación perezosa SALTEA los slots cubiertos
    // (no genera cancelados: borrar el bloqueo los hace reaparecer)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Mes_SalteaElSlotConBloqueoDeRango_YGeneraLosDemas()
    {
        // Bloqueo puntual el martes 14/07 que pisa la franja 18-19
        _bloqueos.Setup(b => b.ListarAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new Bloqueo
                 {
                     Tipo = TipoBloqueo.Rango,
                     Fecha = new DateOnly(2026, 7, 14),
                     HoraInicio = new TimeOnly(17, 0),
                     HoraFin = new TimeOnly(20, 0),
                     Motivo = MotivoBloqueo.MalClima,
                 }]);
        var generados = new List<Turno>();
        _turnos.Setup(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
               .Callback((Turno t, CancellationToken _) => generados.Add(t))
               .Returns(Task.CompletedTask);

        await _service.GenerarTurnosDelMesAsync(2026, 7);

        Assert.Equal(3, generados.Count); // 7, 21 y 28: el 14 está bloqueado
        Assert.DoesNotContain(generados, t => t.Fecha == new DateOnly(2026, 7, 14));
    }

    [Fact]
    public async Task Mes_BloqueoFijoDelDia_NoGeneraNingunTurnoDeEseHorario()
    {
        // Fijo todos los martes 17:30-18:30: solapa parcialmente el turno de 18
        _bloqueos.Setup(b => b.ListarAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new Bloqueo
                 {
                     Tipo = TipoBloqueo.Fijo,
                     Dia = DayOfWeek.Tuesday,
                     HoraInicio = new TimeOnly(17, 30),
                     HoraFin = new TimeOnly(18, 30),
                 }]);

        await _service.GenerarTurnosDelMesAsync(2026, 7);

        _turnos.Verify(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Mes_BloqueoDeOtraCancha_NoAfectaLaGeneracion()
    {
        _bloqueos.Setup(b => b.ListarAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new Bloqueo
                 {
                     Tipo = TipoBloqueo.Fijo,
                     Dia = DayOfWeek.Tuesday,
                     HoraInicio = new TimeOnly(17, 0),
                     HoraFin = new TimeOnly(20, 0),
                     CanchaId = Guid.NewGuid(), // otra cancha
                 }]);
        var generados = new List<Turno>();
        _turnos.Setup(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
               .Callback((Turno t, CancellationToken _) => generados.Add(t))
               .Returns(Task.CompletedTask);

        await _service.GenerarTurnosDelMesAsync(2026, 7);

        Assert.Equal(4, generados.Count); // los 4 martes de julio, sin salteos
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
    public async Task Cancelar_GuardaMotivoFechaYQuien_SinBorrar()
    {
        var turno = TurnoConJuan();

        await _service.CancelarAsync(turno.Id, "Lluvia");

        Assert.Equal(EstadoTurno.Cancelado, turno.Estado);
        Assert.Equal("Lluvia", turno.CanceladoMotivo);
        Assert.NotNull(turno.CanceladoEl);
        Assert.Equal(CanceladoPor.Profesor, turno.CanceladoPor);
        _turnos.Verify(t => t.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancelar_EliminaLosCargosImpagosDelTurno_YRespetaPagados()
    {
        // Turno cancelado por el profe = la clase no ocurre → nadie paga.
        // El cargo YA PAGADO no se toca (plata cobrada es intocable).
        var turno = TurnoConJuan();
        var impago = new Cargo
        {
            AlumnoId = AlumnoJuan, TurnoId = turno.Id, Tipo = TipoCargo.Clase,
            Concepto = "x", Monto = 4_000m, Fecha = turno.Fecha,
        };
        var pagado = new Cargo
        {
            AlumnoId = AlumnaSofia, TurnoId = turno.Id, Tipo = TipoCargo.Clase,
            Concepto = "x", Monto = 4_000m, Fecha = turno.Fecha,
            PagadoEl = DateTime.UtcNow, MedioPago = MedioPago.Efectivo,
        };
        _cargos.Setup(c => c.ListarPorTurnosAsync(
                   It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(turno.Id)), It.IsAny<CancellationToken>()))
               .ReturnsAsync([impago, pagado]);

        await _service.CancelarAsync(turno.Id, "Lluvia");

        _cargos.Verify(c => c.Eliminar(impago), Times.Once);
        _cargos.Verify(c => c.Eliminar(pagado), Times.Never);
    }

    [Fact]
    public async Task Cancelar_UnTurnoYaCancelado_Lanza()
    {
        var turno = TurnoConJuan();
        turno.Estado = EstadoTurno.Cancelado;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarAsync(turno.Id, "otra vez"));
    }

    // ─────────────────────────────────────────────
    // Vista mensual: el staff ve solo lo suyo (igual que la semana)
    // ─────────────────────────────────────────────

    private static Turno TurnoConHorarioDe(Guid profeId) => new()
    {
        HorarioId = Guid.NewGuid(),
        Fecha = new DateOnly(2026, 7, 14),
        HoraInicio = new TimeOnly(18, 0),
        DuracionMinutos = 60,
        Horario = new Horario { ProfesorUserId = profeId, CanchaId = Guid.NewGuid() },
    };

    [Fact]
    public async Task MesVista_Staff_SoloDevuelveSusTurnos()
    {
        var staffId = Guid.NewGuid();
        _usuario.Setup(u => u.EsStaff).Returns(true);
        _usuario.Setup(u => u.UserId).Returns(staffId);
        // Sin horarios activos → la generación no agrega nada (aislamos la vista)
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var mio = TurnoConHorarioDe(staffId);
        var ajeno = TurnoConHorarioDe(Guid.NewGuid());
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([mio, ajeno]);

        var mesVista = await _service.ObtenerMesAsync(2026, 7);

        var t = Assert.Single(mesVista);
        Assert.Equal(mio.Id, t.Id);
        Assert.Equal(staffId, t.ProfesorUserId);
    }

    [Fact]
    public async Task MesVista_Dueño_VeTodosLosTurnos()
    {
        // El dueño no es staff → sin filtro
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([TurnoConHorarioDe(Guid.NewGuid()), TurnoConHorarioDe(Guid.NewGuid())]);

        var mesVista = await _service.ObtenerMesAsync(2026, 7);

        Assert.Equal(2, mesVista.Count);
    }

    // ─────────────────────────────────────────────
    // Cómo se llama un turno en pantalla
    //
    // No había ni un test acá, y por eso pasó desapercibido que al desaparecer los
    // grupos una clase de cuatro se mostraba como "Fulano (suelta)" en el calendario
    // y como "Clase individual" en el portal del alumno (09/08/2026).
    // ─────────────────────────────────────────────

    private static Alumno Alumno(string nombre, string apellido) =>
        new() { Id = Guid.NewGuid(), Nombre = nombre, Apellido = apellido, Telefono = "1" };

    /// <summary>Un turno colgado de una clase con ese nombre y ese roster.</summary>
    private static Turno TurnoDeClase(string? nombre, params Alumno[] roster)
    {
        var horario = new Horario { Id = HorarioId, CanchaId = Guid.NewGuid(), Nombre = nombre };
        foreach (var a in roster)
            horario.Alumnos.Add(new AlumnoHorario { HorarioId = horario.Id, AlumnoId = a.Id, Alumno = a });
        return new Turno { Fecha = Lunes, HorarioId = horario.Id, Horario = horario };
    }

    [Fact]
    public void Titulo_ClaseConNombre_UsaEseNombre()
    {
        var t = TurnoDeClase("Intermedios", Alumno("Juan", "Pérez"), Alumno("Sofía", "Gómez"));

        Assert.Equal("Intermedios", TurnoService.TituloDe(t));
    }

    /// <summary>El caso que estaba roto: una clase de varios NO es de nadie en particular.</summary>
    [Fact]
    public void Titulo_ClaseSinNombreConVarios_DiceCuantosSon()
    {
        var t = TurnoDeClase(null, Alumno("Juan", "Pérez"), Alumno("Sofía", "Gómez"), Alumno("Ana", "Díaz"));

        var titulo = TurnoService.TituloDe(t);

        Assert.Equal("Grupo de 3", titulo);
        Assert.DoesNotContain("suelta", titulo);
        Assert.DoesNotContain("individual", titulo);
    }

    [Fact]
    public void Titulo_ClaseSinNombreConUnoSolo_EsElNombreDelAlumno()
    {
        var t = TurnoDeClase(null, Alumno("Juan", "Pérez"));

        Assert.Equal("Juan Pérez", TurnoService.TituloDe(t));
    }

    [Fact]
    public void Titulo_TurnoSuelto_SeNombraPorQuienLoPidio()
    {
        var suelto = Alumno("Mateo", "Ruiz");
        var t = new Turno { Fecha = Lunes, HorarioId = null };
        t.Participantes.Add(new TurnoParticipante { AlumnoId = suelto.Id, Alumno = suelto });

        Assert.Equal("Mateo Ruiz (suelta)", TurnoService.TituloDe(t));
    }

    [Fact]
    public void Titulo_TurnoSueltoSinParticipantes_NoRompe()
    {
        Assert.Equal("Clase suelta", TurnoService.TituloDe(new Turno { Fecha = Lunes, HorarioId = null }));
    }
}
