using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reglas de las clases fijas (TDD): solapamiento POR CANCHA, el cupo y quién puede
/// sumarse al roster, y la desactivación que limpia el futuro sin tocar historia ni
/// plata cobrada. Las reglas de cupo/estado/deuda vienen del viejo GrupoService: el
/// grupo desapareció, las reglas no.
/// </summary>
public class HorarioServiceTests
{
    private static readonly Guid Cancha1 = Guid.NewGuid();
    private static readonly Guid Cancha2 = Guid.NewGuid();
    private static readonly Guid HorarioId = Guid.NewGuid();
    private static readonly Guid AlumnoId = Guid.NewGuid();

    private readonly Mock<IHorarioRepository> _repo;
    private readonly Mock<ITurnoRepository> _turnos;
    private readonly Mock<ICargoRepository> _cargos;
    private readonly Mock<IBloqueoRepository> _bloqueos;
    private readonly Mock<IStaffService> _staff;
    private readonly Mock<IAlumnoRepository> _alumnos;
    private readonly Mock<ISedeRepository> _sedes;
    private readonly Mock<IUsuarioActual> _usuario;
    private readonly Mock<IAlumnoService> _alumnoService;
    private readonly HorarioService _service;

    public HorarioServiceTests()
    {
        _repo = new Mock<IHorarioRepository>();
        _turnos = new Mock<ITurnoRepository>();
        _cargos = new Mock<ICargoRepository>();
        _bloqueos = new Mock<IBloqueoRepository>();
        _staff = new Mock<IStaffService>();
        _alumnos = new Mock<IAlumnoRepository>();
        _sedes = new Mock<ISedeRepository>();
        _usuario = new Mock<IUsuarioActual>(); // por defecto: no es staff → sin límite de club
        _alumnoService = new Mock<IAlumnoService>();
        _service = new HorarioService(
            _repo.Object, _turnos.Object, _cargos.Object, _bloqueos.Object,
            _staff.Object, _alumnos.Object, _sedes.Object, _usuario.Object, _alumnoService.Object);

        // Por defecto: cualquier profe asignado es válido (los tests que prueban la
        // regla lo pisan con false)
        _staff.Setup(s => s.EsAsignableAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        // Por defecto: nadie debe nada
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

        // Por defecto: no hay bloqueos
        _bloqueos.Setup(b => b.ListarAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Por defecto: el alumno existe y está activo
        _alumnos.Setup(r => r.ObtenerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) => new Alumno
                {
                    Id = id, Nombre = "Juan", Apellido = "Pérez", Telefono = "1",
                    Estado = EstadoAlumno.Activo,
                });

        // Ya existe: martes 18:00-19:00 en Cancha 1
        var existente = new Horario
        {
            CanchaId = Cancha1,
            Dia = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(18, 0),
            DuracionMinutos = 60,
        };
        _repo.Setup(r => r.ListarPorCanchaYDiaAsync(Cancha1, DayOfWeek.Tuesday, It.IsAny<CancellationToken>()))
             .ReturnsAsync([existente]);
        _repo.Setup(r => r.ListarPorCanchaYDiaAsync(Cancha2, It.IsAny<DayOfWeek>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Horario h, CancellationToken _) => h);
    }

    private static CreateHorarioDto Dto(Guid cancha, TimeOnly hora, int duracion = 60) => new()
    {
        CanchaId = cancha,
        Dia = DayOfWeek.Tuesday,
        HoraInicio = hora,
        DuracionMinutos = duracion,
    };

    /// <summary>Una clase ya existente, con su roster, para los tests del cupo.</summary>
    private Horario ClaseCon(int? cupo, int ocupados)
    {
        var horario = new Horario
        {
            Id = HorarioId, CanchaId = Cancha1, CupoMaximo = cupo,
            Dia = DayOfWeek.Tuesday, HoraInicio = new TimeOnly(18, 0),
        };
        _repo.Setup(r => r.ObtenerAsync(HorarioId, It.IsAny<CancellationToken>())).ReturnsAsync(horario);
        _repo.Setup(r => r.ContarActivosAsync(HorarioId, It.IsAny<CancellationToken>())).ReturnsAsync(ocupados);
        return horario;
    }

    private static UpdateHorarioDto Update(
        Guid cancha, DayOfWeek dia, TimeOnly hora, int duracion = 60,
        Guid? profe = null, decimal? valor = null) => new()
    {
        CanchaId = cancha,
        Dia = dia,
        HoraInicio = hora,
        DuracionMinutos = duracion,
        ProfesorUserId = profe,
        ValorHoraProfe = valor,
    };

    [Fact]
    public async Task Crear_SolapaEnLaMismaCancha_Lanza()
    {
        // 18:30-19:30 pisa al de 18:00-19:00 en la misma cancha
        var dto = Dto(Cancha1, new TimeOnly(18, 30));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Crear_MismaHoraEnOtraCancha_Crea()
    {
        // El profe tiene staff: dos clases a la vez en canchas distintas, OK
        var dto = Dto(Cancha2, new TimeOnly(18, 0));

        await _service.CrearAsync(dto);

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_ConProfeAjenoAlClub_Lanza()
    {
        _staff.Setup(s => s.EsAsignableAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var dto = Dto(Cancha2, new TimeOnly(10, 0));
        dto.ProfesorUserId = Guid.NewGuid();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Crear_ConProfeDelClub_GuardaLaAsignacion()
    {
        var profe = Guid.NewGuid();
        var dto = Dto(Cancha2, new TimeOnly(10, 0));
        dto.ProfesorUserId = profe;
        Horario? creado = null;
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Horario h, CancellationToken _) => { creado = h; return h; });

        await _service.CrearAsync(dto);

        Assert.Equal(profe, creado!.ProfesorUserId);
    }

    [Fact]
    public async Task Crear_PisaUnBloqueoFijo_Lanza()
    {
        // Bloqueo fijo martes 18:00-20:00 en TODAS las canchas → no se puede poner ahí
        _bloqueos.Setup(b => b.ListarAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new Bloqueo
        {
            Tipo = TipoBloqueo.Fijo,
            Dia = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(18, 0),
            HoraFin = new TimeOnly(20, 0),
            CanchaId = null, // todas
        }]);
        var dto = Dto(Cancha2, new TimeOnly(18, 30)); // cancha libre de horarios, pero bloqueada

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Crear_BloqueoFijoDeOtraCancha_NoFrena()
    {
        // El bloqueo es solo en Cancha1 → un horario en Cancha2 se puede crear igual
        _bloqueos.Setup(b => b.ListarAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new Bloqueo
        {
            Tipo = TipoBloqueo.Fijo,
            Dia = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(18, 0),
            HoraFin = new TimeOnly(20, 0),
            CanchaId = Cancha1,
        }]);
        var dto = Dto(Cancha2, new TimeOnly(18, 30));

        await _service.CrearAsync(dto);

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_ContiguoEnLaMismaCancha_NoEsSolapamiento()
    {
        // 19:00-20:00 arranca justo cuando termina el de 18:00 → válido
        var dto = Dto(Cancha1, new TimeOnly(19, 0));

        await _service.CrearAsync(dto);

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_SinAlumnos_EsValido()
    {
        // Armar la clase primero y sumar la gente después es un caso normal
        // (antes era imposible: el horario exigía un grupo o un alumno).
        var dto = Dto(Cancha2, new TimeOnly(10, 0));

        await _service.CrearAsync(dto);

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_ConMasAlumnosQueElCupo_Lanza()
    {
        var dto = Dto(Cancha2, new TimeOnly(10, 0));
        dto.CupoMaximo = 2;
        dto.AlumnoIds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Crear_ConAlumnos_LosSumaAlRosterYLosPromueveDeEspera()
    {
        var dto = Dto(Cancha2, new TimeOnly(10, 0));
        dto.AlumnoIds = [AlumnoId];

        await _service.CrearAsync(dto);

        _repo.Verify(r => r.AgregarMembresiaAsync(
            It.Is<AlumnoHorario>(m => m.AlumnoId == AlumnoId), It.IsAny<CancellationToken>()), Times.Once);
        _alumnos.Verify(r => r.PromoverDeEsperaAsync(AlumnoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// La membresía que se registra en el repositorio tiene que ser LA MISMA instancia
    /// que queda colgada del horario. Con dos objetos distintos —misma PK compuesta
    /// (AlumnoId, HorarioId)— EF los cuenta como dos filas y el alta explota contra la
    /// base real; con los repos mockeados no se nota, así que se chequea acá.
    /// </summary>
    [Fact]
    public async Task Crear_ConAlumnos_RegistraUnaSolaMembresiaPorAlumno()
    {
        var dto = Dto(Cancha2, new TimeOnly(10, 0));
        dto.AlumnoIds = [AlumnoId];

        Horario? guardado = null;
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()))
             .Callback<Horario, CancellationToken>((h, _) => guardado = h)
             .ReturnsAsync((Horario h, CancellationToken _) => h);

        var registradas = new List<AlumnoHorario>();
        _repo.Setup(r => r.AgregarMembresiaAsync(It.IsAny<AlumnoHorario>(), It.IsAny<CancellationToken>()))
             .Callback<AlumnoHorario, CancellationToken>((m, _) => registradas.Add(m))
             .Returns(Task.CompletedTask);

        await _service.CrearAsync(dto);

        var registrada = Assert.Single(registradas);
        var enElHorario = Assert.Single(guardado!.Alumnos);
        Assert.Same(registrada, enElHorario);
    }

    [Fact]
    public async Task Crear_ConUnAlumnoConCuotaVencida_NoCreaNada()
    {
        var dto = Dto(Cancha2, new TimeOnly(10, 0));
        dto.AlumnoIds = [AlumnoId];
        // Debe una clase de hace 2 meses: liquidación vencida hace rato
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([new Cargo
               {
                   AlumnoId = AlumnoId, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m,
                   Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2),
               }]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
        // La validación va ANTES de crear: no queda una clase a medio armar
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────
    // El roster: las reglas que antes vivían en GrupoService
    // ─────────────────────────────────────────────

    [Fact]
    public async Task AgregarAlumno_ClaseCompleta_Lanza()
    {
        ClaseCon(cupo: 4, ocupados: 4);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AgregarAlumnoAsync(HorarioId, AlumnoId));
        _repo.Verify(r => r.AgregarMembresiaAsync(It.IsAny<AlumnoHorario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AgregarAlumno_SinLimiteDeCupo_LoSuma()
    {
        ClaseCon(cupo: null, ocupados: 99);

        await _service.AgregarAlumnoAsync(HorarioId, AlumnoId);

        _repo.Verify(r => r.AgregarMembresiaAsync(It.IsAny<AlumnoHorario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AgregarAlumno_YaEstaEnLaClase_Lanza()
    {
        ClaseCon(cupo: 4, ocupados: 1);
        _repo.Setup(r => r.ObtenerMembresiaAsync(HorarioId, AlumnoId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new AlumnoHorario { HorarioId = HorarioId, AlumnoId = AlumnoId });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AgregarAlumnoAsync(HorarioId, AlumnoId));
    }

    [Fact]
    public async Task AgregarAlumno_QueSeHabiaIdo_ReactivaSuLugar()
    {
        ClaseCon(cupo: 4, ocupados: 1);
        var vieja = new AlumnoHorario
        {
            HorarioId = HorarioId, AlumnoId = AlumnoId, FechaBaja = DateTime.UtcNow.AddMonths(-1),
        };
        _repo.Setup(r => r.ObtenerMembresiaAsync(HorarioId, AlumnoId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(vieja);

        await _service.AgregarAlumnoAsync(HorarioId, AlumnoId);

        Assert.Null(vieja.FechaBaja); // se reutiliza la fila, no se duplica
        _repo.Verify(r => r.AgregarMembresiaAsync(It.IsAny<AlumnoHorario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AgregarAlumno_NoActivo_Lanza()
    {
        ClaseCon(cupo: 4, ocupados: 0);
        _alumnos.Setup(r => r.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Alumno
                {
                    Id = AlumnoId, Nombre = "Juan", Apellido = "Pérez", Telefono = "1",
                    Estado = EstadoAlumno.Inactivo,
                });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AgregarAlumnoAsync(HorarioId, AlumnoId));
    }

    [Fact]
    public async Task AgregarAlumno_ConCuotaVencida_Lanza()
    {
        ClaseCon(cupo: 4, ocupados: 0);
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([new Cargo
               {
                   AlumnoId = AlumnoId, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m,
                   Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2),
               }]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AgregarAlumnoAsync(HorarioId, AlumnoId));
    }

    [Fact]
    public async Task AgregarAlumno_CasoFeliz_ReconciliaElCalendario()
    {
        ClaseCon(cupo: 4, ocupados: 1);

        await _service.AgregarAlumnoAsync(HorarioId, AlumnoId);

        // Sin esto, el que se suma no aparece en los turnos ya generados
        _alumnoService.Verify(s => s.SincronizarCalendarioAsync(AlumnoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QuitarAlumno_LeDaDeBajaYReconcilia()
    {
        ClaseCon(cupo: 4, ocupados: 2);
        var membresia = new AlumnoHorario { HorarioId = HorarioId, AlumnoId = AlumnoId };
        _repo.Setup(r => r.ObtenerMembresiaAsync(HorarioId, AlumnoId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(membresia);

        await _service.QuitarAlumnoAsync(HorarioId, AlumnoId);

        Assert.NotNull(membresia.FechaBaja); // baja lógica: se conserva la historia
        _alumnoService.Verify(s => s.SincronizarCalendarioAsync(AlumnoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QuitarAlumno_QueNoEstaEnLaClase_Lanza()
    {
        ClaseCon(cupo: 4, ocupados: 2);
        _repo.Setup(r => r.ObtenerMembresiaAsync(HorarioId, AlumnoId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((AlumnoHorario?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.QuitarAlumnoAsync(HorarioId, AlumnoId));
    }

    [Fact]
    public void TituloAutomatico_SegunElRoster()
    {
        var uno = new List<AlumnoHorario>
        {
            new() { AlumnoId = AlumnoId, Alumno = new Alumno { Nombre = "Ana", Apellido = "Gómez", Telefono = "1" } },
        };
        var varios = new List<AlumnoHorario>
        {
            new() { Alumno = new Alumno { Nombre = "Ana", Apellido = "Gómez", Telefono = "1" } },
            new() { Alumno = new Alumno { Nombre = "Luis", Apellido = "Ruiz", Telefono = "2" } },
        };

        Assert.Equal("Intermedios", HorarioService.TituloDe("Intermedios", varios)); // el nombre manda
        Assert.Equal("Ana Gómez", HorarioService.TituloDe(null, uno));               // la particular
        Assert.Equal("Grupo de 2", HorarioService.TituloDe(null, varios));
        Assert.Equal("Clase sin alumnos", HorarioService.TituloDe(null, []));
    }

    // ─────────────────────────────────────────────
    // Desactivar: apaga la plantilla y limpia el futuro
    // ─────────────────────────────────────────────

    private Horario HorarioActivo()
    {
        var horario = new Horario
        {
            CanchaId = Cancha1,
            AlumnoId = AlumnoId,
            Dia = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(10, 0),
            DuracionMinutos = 30,
            Activo = true,
        };
        _repo.Setup(r => r.ObtenerAsync(horario.Id, It.IsAny<CancellationToken>()))
             .ReturnsAsync(horario);
        return horario;
    }

    private Turno TurnoDe(Horario horario, DateOnly fecha) => new()
    {
        HorarioId = horario.Id,
        CanchaId = horario.CanchaId,
        Fecha = fecha,
        HoraInicio = horario.HoraInicio,
        DuracionMinutos = horario.DuracionMinutos,
    };

    [Fact]
    public async Task Desactivar_ApagaLaPlantilla_YBorraTurnosFuturosConSusCargosImpagos()
    {
        var horario = HorarioActivo();
        var futuro = TurnoDe(horario, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3));
        var cargoImpago = new Cargo
        {
            AlumnoId = AlumnoId, TurnoId = futuro.Id, Tipo = TipoCargo.Clase,
            Concepto = "Clase individual (30')", Monto = 8_000m, Fecha = futuro.Fecha,
        };
        _turnos.Setup(t => t.ListarPorHorarioDesdeAsync(horario.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([futuro]);
        _cargos.Setup(c => c.ListarPorTurnosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([cargoImpago]);

        await _service.DesactivarAsync(horario.Id);

        Assert.False(horario.Activo);
        _cargos.Verify(c => c.Eliminar(cargoImpago), Times.Once);
        _turnos.Verify(t => t.Eliminar(futuro), Times.Once);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Desactivar_ConservaElTurnoFuturoSiTieneUnCargoPagado()
    {
        var horario = HorarioActivo();
        var futuro = TurnoDe(horario, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3));
        var cargoPagado = new Cargo
        {
            AlumnoId = AlumnoId, TurnoId = futuro.Id, Tipo = TipoCargo.Clase,
            Concepto = "Clase individual (30')", Monto = 8_000m, Fecha = futuro.Fecha,
            PagadoEl = DateTime.UtcNow, MedioPago = MedioPago.Efectivo,
        };
        _turnos.Setup(t => t.ListarPorHorarioDesdeAsync(horario.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([futuro]);
        _cargos.Setup(c => c.ListarPorTurnosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([cargoPagado]);

        await _service.DesactivarAsync(horario.Id);

        // Plata cobrada no se toca: ni el cargo ni su turno se borran
        Assert.False(horario.Activo);
        _cargos.Verify(c => c.Eliminar(It.IsAny<Cargo>()), Times.Never);
        _turnos.Verify(t => t.Eliminar(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task Desactivar_SoloMiraDesdeHoy_LoPasadoEsHistoria()
    {
        var horario = HorarioActivo();
        _turnos.Setup(t => t.ListarPorHorarioDesdeAsync(horario.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

        await _service.DesactivarAsync(horario.Id);

        // Pide los turnos DESDE HOY: los anteriores ni se consultan
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        _turnos.Verify(t => t.ListarPorHorarioDesdeAsync(horario.Id, hoy, It.IsAny<CancellationToken>()), Times.Once);
        _turnos.Verify(t => t.Eliminar(It.IsAny<Turno>()), Times.Never);
    }

    // ─────────────────────────────────────────────
    // Editar: cambia el horario; si movés la agenda, reprograma turnos futuros
    // ─────────────────────────────────────────────

    /// <summary>Horario editable en Cancha2/miércoles 10:00 (Cancha2 no tiene otros → sin solape).</summary>
    private Horario HorarioEditable()
    {
        var horario = new Horario
        {
            CanchaId = Cancha2,

            Dia = DayOfWeek.Wednesday,
            HoraInicio = new TimeOnly(10, 0),
            DuracionMinutos = 60,
            Activo = true,
        };
        _repo.Setup(r => r.ObtenerAsync(horario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horario);
        return horario;
    }

    [Fact]
    public async Task Editar_SoloProfeYValor_SeteaLosCampos_SinTocarTurnos()
    {
        var horario = HorarioEditable();
        var profe = Guid.NewGuid();
        // Mismo día/hora/cancha/duración: solo cambia el profe y su valor hora
        var dto = Update(Cancha2, DayOfWeek.Wednesday, new TimeOnly(10, 0), 60, profe, 5_000m);

        await _service.EditarAsync(horario.Id, dto);

        Assert.Equal(profe, horario.ProfesorUserId);
        Assert.Equal(5_000m, horario.ValorHoraProfe);
        _turnos.Verify(t => t.Eliminar(It.IsAny<Turno>()), Times.Never); // no se movió la agenda
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Editar_CambiaLaHora_ReprogramaLosTurnosFuturos()
    {
        var horario = HorarioEditable();
        var futuro = TurnoDe(horario, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3));
        var cargoImpago = new Cargo
        {
            AlumnoId = AlumnoId, TurnoId = futuro.Id, Tipo = TipoCargo.Clase,
            Concepto = "Clase", Monto = 8_000m, Fecha = futuro.Fecha,
        };
        _turnos.Setup(t => t.ListarPorHorarioDesdeAsync(horario.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([futuro]);
        _cargos.Setup(c => c.ListarPorTurnosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([cargoImpago]);
        var dto = Update(Cancha2, DayOfWeek.Wednesday, new TimeOnly(11, 0), 60); // movió la hora

        await _service.EditarAsync(horario.Id, dto);

        Assert.Equal(new TimeOnly(11, 0), horario.HoraInicio);
        _turnos.Verify(t => t.Eliminar(futuro), Times.Once);
        _cargos.Verify(c => c.Eliminar(cargoImpago), Times.Once);
    }

    [Fact]
    public async Task Editar_SeSuperponeConOtroHorario_Lanza()
    {
        var horario = HorarioEditable();
        // Lo muevo a Cancha1/martes 18:30 → pisa al `existente` (18:00-19:00, otro id)
        var dto = Update(Cancha1, DayOfWeek.Tuesday, new TimeOnly(18, 30), 60);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EditarAsync(horario.Id, dto));
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Editar_MismoSlot_NoSePisaASiMismo()
    {
        var horario = new Horario
        {
            CanchaId = Cancha1, Dia = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60, Activo = true,
        };
        _repo.Setup(r => r.ObtenerAsync(horario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horario);
        // El propio horario aparece en la lista de su cancha/día: no debe contarse como solape
        _repo.Setup(r => r.ListarPorCanchaYDiaAsync(Cancha1, DayOfWeek.Tuesday, It.IsAny<CancellationToken>()))
             .ReturnsAsync([horario]);
        var dto = Update(Cancha1, DayOfWeek.Tuesday, new TimeOnly(18, 0), 60, Guid.NewGuid(), 7_000m);

        await _service.EditarAsync(horario.Id, dto);

        Assert.Equal(7_000m, horario.ValorHoraProfe);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Editar_PisaUnBloqueoFijo_Lanza()
    {
        var horario = HorarioEditable();
        _bloqueos.Setup(b => b.ListarAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new Bloqueo
        {
            Tipo = TipoBloqueo.Fijo, Dia = DayOfWeek.Wednesday,
            HoraInicio = new TimeOnly(9, 0), HoraFin = new TimeOnly(12, 0), CanchaId = null,
        }]);
        var dto = Update(Cancha2, DayOfWeek.Wednesday, new TimeOnly(10, 30), 60); // dentro del bloqueo

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EditarAsync(horario.Id, dto));
    }

    [Fact]
    public async Task Editar_HorarioInexistente_Lanza()
    {
        _repo.Setup(r => r.ObtenerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Horario?)null);
        var dto = Update(Cancha2, DayOfWeek.Wednesday, new TimeOnly(10, 0), 60);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EditarAsync(Guid.NewGuid(), dto));
    }

    // ─────────────────────────────────────────────
    // El profe EMPLEADO solo gestiona canchas de SU club (sede)
    // ─────────────────────────────────────────────

    /// <summary>Marca al usuario del request como staff de la sede dada.</summary>
    private Guid ComoStaffDe(Guid sede)
    {
        var yo = Guid.NewGuid();
        _usuario.Setup(u => u.EsStaff).Returns(true);
        _usuario.Setup(u => u.UserId).Returns(yo);
        _staff.Setup(s => s.SedeDelProfeAsync(yo, It.IsAny<CancellationToken>())).ReturnsAsync(sede);
        return yo;
    }

    [Fact]
    public async Task Crear_Staff_EnCanchaDeSuClub_Crea()
    {
        var miSede = Guid.NewGuid();
        ComoStaffDe(miSede);
        _sedes.Setup(s => s.SedeDeCanchaAsync(Cancha2, It.IsAny<CancellationToken>())).ReturnsAsync(miSede);

        await _service.CrearAsync(Dto(Cancha2, new TimeOnly(10, 0)));

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_Staff_QuedaAsuNombre()
    {
        // El horario que crea un empleado queda a SU nombre (así aparece en su agenda,
        // que muestra las clases propias), aunque no elija profe.
        var miSede = Guid.NewGuid();
        var yo = ComoStaffDe(miSede);
        _sedes.Setup(s => s.SedeDeCanchaAsync(Cancha2, It.IsAny<CancellationToken>())).ReturnsAsync(miSede);
        Horario? creado = null;
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Horario h, CancellationToken _) => { creado = h; return h; });

        await _service.CrearAsync(Dto(Cancha2, new TimeOnly(10, 0)));

        Assert.Equal(yo, creado!.ProfesorUserId);
    }

    [Fact]
    public async Task Crear_Staff_EnCanchaDeOtroClub_Lanza()
    {
        ComoStaffDe(Guid.NewGuid());
        _sedes.Setup(s => s.SedeDeCanchaAsync(Cancha2, It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(Dto(Cancha2, new TimeOnly(10, 0))));
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Editar_Staff_MoverACanchaDeOtroClub_Lanza()
    {
        var horario = HorarioEditable(); // Cancha2
        var miSede = Guid.NewGuid();
        ComoStaffDe(miSede);
        _sedes.Setup(s => s.SedeDeCanchaAsync(Cancha2, It.IsAny<CancellationToken>())).ReturnsAsync(miSede);        // origen: su club
        _sedes.Setup(s => s.SedeDeCanchaAsync(Cancha1, It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid()); // destino: otro
        var dto = Update(Cancha1, DayOfWeek.Tuesday, new TimeOnly(8, 0), 60);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EditarAsync(horario.Id, dto));
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Listar_Staff_SoloVeLosHorariosDeSuClub()
    {
        var miSede = Guid.NewGuid();
        ComoStaffDe(miSede);
        var mio = new Horario
        {
            Dia = DayOfWeek.Monday, HoraInicio = new TimeOnly(9, 0),
            DuracionMinutos = 60, Cancha = new Cancha { Nombre = "Cancha 1", SedeId = miSede },
        };
        var ajeno = new Horario
        {
            Dia = DayOfWeek.Monday, HoraInicio = new TimeOnly(9, 0),
            DuracionMinutos = 60, Cancha = new Cancha { Nombre = "Cancha 2", SedeId = Guid.NewGuid() },
        };
        _repo.Setup(r => r.ListarActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([mio, ajeno]);

        var res = await _service.ListarAsync();

        Assert.Single(res);
    }
}
