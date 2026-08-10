using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// El alumno pide un lugar en una clase con cupo (M5a). Acá vive la regla que se
/// sacó de HorarioService: <b>el que tiene la cuota vencida no toma clases nuevas
/// por su cuenta</b>. Del lado del profe no aplica — él asigna a quien quiera.
/// </summary>
public class SolicitudCupoServiceTests
{
    private static readonly Guid AlumnoId = Guid.NewGuid();
    private static readonly Guid HorarioId = Guid.NewGuid();

    private static readonly Guid MiClub = Guid.NewGuid();
    private static readonly Guid OtroClub = Guid.NewGuid();

    private readonly Mock<ISolicitudCupoRepository> _solicitudes;
    private readonly Mock<IAlumnoRepository> _alumnos;
    private readonly Mock<IHorarioRepository> _horarios;
    private readonly Mock<ICargoRepository> _cargos;
    private readonly Mock<IHorarioService> _horarioService;
    private readonly SolicitudCupoService _service;

    public SolicitudCupoServiceTests()
    {
        _solicitudes = new Mock<ISolicitudCupoRepository>();
        _alumnos = new Mock<IAlumnoRepository>();
        _horarios = new Mock<IHorarioRepository>();
        _cargos = new Mock<ICargoRepository>();
        _horarioService = new Mock<IHorarioService>();
        _service = new SolicitudCupoService(
            _solicitudes.Object, _alumnos.Object, _horarios.Object,
            _cargos.Object, _horarioService.Object);

        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AlumnoActivo());
        // Por defecto: no debe nada
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _horarios.Setup(h => h.ObtenerConRosterAsync(HorarioId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(ClaseConLugar());
        _solicitudes.Setup(s => s.ExistePendienteAsync(AlumnoId, HorarioId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
        // Por defecto: sin pedidos previos ni clases en la agenda
        _solicitudes.Setup(s => s.ListarPorAlumnoAsync(AlumnoId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync([]);
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);
    }

    private static Alumno AlumnoActivo() => new()
    {
        Id = AlumnoId, Nombre = "Lucas", Apellido = "C", Telefono = "1",
        Estado = EstadoAlumno.Activo, Categoria = CategoriaAlumno.SinCategoria,
        SedeId = MiClub,
    };

    /// <summary>Clase activa, sin categoría (abierta a todos) y con lugar.</summary>
    private static Horario ClaseConLugar() => new()
    {
        Id = HorarioId, CanchaId = Guid.NewGuid(), Activo = true, CupoMaximo = 4,
        Dia = DayOfWeek.Tuesday, HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60,
    };

    /// <summary>Una clase de la agenda, ubicada en un club (la grilla filtra por ahí).</summary>
    private static Horario ClaseEn(Guid sedeId, int cupo = 4, CategoriaAlumno? categoria = null)
    {
        var sede = new Sede { Id = sedeId, Nombre = sedeId == MiClub ? "Mi club" : "Otro club" };
        return new Horario
        {
            Id = Guid.NewGuid(), CanchaId = Guid.NewGuid(), Activo = true,
            CupoMaximo = cupo, Categoria = categoria,
            Dia = DayOfWeek.Tuesday, HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60,
            Cancha = new Cancha { Nombre = "Cancha 1", SedeId = sedeId, Sede = sede },
        };
    }

    /// <summary>Llena la clase hasta el cupo con OTROS alumnos.</summary>
    private static Horario Llenando(Horario h)
    {
        for (var i = 0; i < (h.CupoMaximo ?? 1); i++)
            h.Alumnos.Add(new AlumnoHorario { HorarioId = h.Id, AlumnoId = Guid.NewGuid() });
        return h;
    }

    private void EnLaAgenda(params Horario[] horarios) =>
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(horarios);

    private static Cargo VencidoHaceDosMeses() => new()
    {
        AlumnoId = AlumnoId, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m,
        Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2),
    };

    [Fact]
    public async Task Solicitar_CasoFeliz_DejaLaSolicitudPendiente()
    {
        await _service.SolicitarAsync(AlumnoId, HorarioId);

        _solicitudes.Verify(s => s.AgregarAsync(
            It.Is<SolicitudCupo>(x => x.AlumnoId == AlumnoId && x.HorarioId == HorarioId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// La regla que se mudó desde HorarioService: pedir clases nuevas debiendo, no.
    /// Del otro lado, el profe SÍ puede sumarlo a mano (ver HorarioServiceTests).
    /// </summary>
    [Fact]
    public async Task Solicitar_ConCuotaVencida_Lanza()
    {
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([VencidoHaceDosMeses()]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.SolicitarAsync(AlumnoId, HorarioId));
        _solicitudes.Verify(s => s.AgregarAsync(
            It.IsAny<SolicitudCupo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Aceptar NO revalida la deuda: si se endeudó entre el pedido y la respuesta,
    /// la decisión es del profe (aceptar es su gesto, no del alumno).
    /// </summary>
    [Fact]
    public async Task Aceptar_ConCuotaVencida_LoSumaIgual()
    {
        var solicitud = new SolicitudCupo { AlumnoId = AlumnoId, HorarioId = HorarioId };
        _solicitudes.Setup(s => s.ObtenerAsync(solicitud.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(solicitud);
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([VencidoHaceDosMeses()]);

        await _service.AceptarAsync(solicitud.Id);

        _horarioService.Verify(h => h.AgregarAlumnoAsync(HorarioId, AlumnoId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(EstadoSolicitudGrupo.Aceptada, solicitud.Estado);
    }

    [Fact]
    public async Task Rechazar_LaMarcaRechazadaYNoTocaElRoster()
    {
        var solicitud = new SolicitudCupo { AlumnoId = AlumnoId, HorarioId = HorarioId };
        _solicitudes.Setup(s => s.ObtenerAsync(solicitud.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(solicitud);

        await _service.RechazarAsync(solicitud.Id);

        Assert.Equal(EstadoSolicitudGrupo.Rechazada, solicitud.Estado);
        _horarioService.Verify(h => h.AgregarAlumnoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Una solicitud se resuelve UNA vez. Protege contra el doble clic y contra que
    /// el dueño y el staff la toquen a la vez: sin esto, aceptar dos veces intentaría
    /// sumar al alumno de nuevo (y "ya está en esta clase" sería un error confuso).
    /// </summary>
    [Theory]
    [InlineData(EstadoSolicitudGrupo.Aceptada)]
    [InlineData(EstadoSolicitudGrupo.Rechazada)]
    public async Task Resolver_UnaSolicitudYaResuelta_Lanza(EstadoSolicitudGrupo estado)
    {
        var solicitud = new SolicitudCupo { AlumnoId = AlumnoId, HorarioId = HorarioId, Estado = estado };
        _solicitudes.Setup(s => s.ObtenerAsync(solicitud.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(solicitud);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AceptarAsync(solicitud.Id));
        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.RechazarAsync(solicitud.Id));
    }

    [Fact]
    public async Task Solicitar_ClaseSinLugar_Lanza()
    {
        var llena = ClaseConLugar();
        llena.CupoMaximo = 1;
        llena.Alumnos.Add(new AlumnoHorario { HorarioId = HorarioId, AlumnoId = Guid.NewGuid() });
        _horarios.Setup(h => h.ObtenerConRosterAsync(HorarioId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(llena);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.SolicitarAsync(AlumnoId, HorarioId));
    }

    [Fact]
    public async Task Solicitar_YaPidioEsaClase_Lanza()
    {
        _solicitudes.Setup(s => s.ExistePendienteAsync(AlumnoId, HorarioId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.SolicitarAsync(AlumnoId, HorarioId));
    }

    [Fact]
    public async Task Solicitar_YaVieneAEsaClase_Lanza()
    {
        var conElAdentro = ClaseConLugar();
        conElAdentro.Alumnos.Add(new AlumnoHorario { HorarioId = HorarioId, AlumnoId = AlumnoId });
        _horarios.Setup(h => h.ObtenerConRosterAsync(HorarioId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(conElAdentro);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.SolicitarAsync(AlumnoId, HorarioId));
    }

    // ── La grilla de Reservar: qué ve el alumno y, sobre todo, qué NO ──

    [Fact]
    public async Task Grilla_ClaseDeOtroClub_NoAparece()
    {
        EnLaAgenda(ClaseEn(MiClub), ClaseEn(OtroClub));

        var slot = Assert.Single(await _service.GrillaParaAlumnoAsync(AlumnoId));

        Assert.Equal("Mi club", slot.Sede);
    }

    [Fact]
    public async Task Grilla_AlumnoSinClub_VeTodos()
    {
        // Una pantalla vacía sin explicación es peor que ver de más.
        var sinClub = AlumnoActivo();
        sinClub.SedeId = null;
        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>())).ReturnsAsync(sinClub);
        EnLaAgenda(ClaseEn(MiClub), ClaseEn(OtroClub));

        Assert.Equal(2, (await _service.GrillaParaAlumnoAsync(AlumnoId)).Count);
    }

    [Fact]
    public async Task Grilla_ConLugarYMiCategoria_EsDisponibleConSusLugares()
    {
        var clase = ClaseEn(MiClub, cupo: 4);
        clase.Alumnos.Add(new AlumnoHorario { HorarioId = clase.Id, AlumnoId = Guid.NewGuid() });
        EnLaAgenda(clase);

        var slot = Assert.Single(await _service.GrillaParaAlumnoAsync(AlumnoId));

        Assert.Equal(nameof(EstadoSlot.Disponible), slot.Estado);
        Assert.Equal(clase.Id, slot.HorarioId);
        Assert.Equal(3, slot.LugaresLibres); // 4 - 1, no "1/4"
    }

    [Fact]
    public async Task Grilla_SinCupoMaximo_EsDisponibleSinNumero()
    {
        // Cupo abierto: "hay lugar" sin decir cuántos son.
        var clase = ClaseEn(MiClub);
        clase.CupoMaximo = null;
        clase.Alumnos.Add(new AlumnoHorario { HorarioId = clase.Id, AlumnoId = Guid.NewGuid() });
        EnLaAgenda(clase);

        var slot = Assert.Single(await _service.GrillaParaAlumnoAsync(AlumnoId));

        Assert.Equal(nameof(EstadoSlot.Disponible), slot.Estado);
        Assert.Null(slot.LugaresLibres);
    }

    /// <summary>
    /// El test que fija la regla del PR: lo que el alumno no puede pedir viaja SIN datos.
    /// Ni el id (que lo dejaría pedirlo tocando la API), ni la categoría, ni cuánta gente
    /// hay adentro.
    /// </summary>
    [Fact]
    public async Task Grilla_ClaseLlena_EsOcupadoYNoLlevaNada()
    {
        EnLaAgenda(Llenando(ClaseEn(MiClub, cupo: 2, categoria: CategoriaAlumno.Primera)));

        var slot = Assert.Single(await _service.GrillaParaAlumnoAsync(AlumnoId));

        Assert.Equal(nameof(EstadoSlot.Ocupado), slot.Estado);
        Assert.Null(slot.HorarioId);
        Assert.Null(slot.Categoria);
        Assert.Null(slot.LugaresLibres);
    }

    [Fact]
    public async Task Grilla_ConLugarPeroDeOtraCategoria_EsOcupadoIgual()
    {
        // No se distingue de la llena: el alumno no necesita saber por qué no puede.
        var deOtroNivel = ClaseEn(MiClub, categoria: CategoriaAlumno.Primera);
        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Alumno
                {
                    Id = AlumnoId, Nombre = "L", Apellido = "C", Telefono = "1",
                    Estado = EstadoAlumno.Activo, Categoria = CategoriaAlumno.Cuarta,
                    SedeId = MiClub,
                });
        EnLaAgenda(deOtroNivel);

        var slot = Assert.Single(await _service.GrillaParaAlumnoAsync(AlumnoId));

        Assert.Equal(nameof(EstadoSlot.Ocupado), slot.Estado);
        Assert.Null(slot.HorarioId);
    }

    [Fact]
    public async Task Grilla_ClaseDondeYaVa_EsMia()
    {
        var mia = ClaseEn(MiClub);
        mia.Alumnos.Add(new AlumnoHorario { HorarioId = mia.Id, AlumnoId = AlumnoId });
        EnLaAgenda(mia);

        var slot = Assert.Single(await _service.GrillaParaAlumnoAsync(AlumnoId));

        Assert.Equal(nameof(EstadoSlot.Mia), slot.Estado);
        Assert.Null(slot.HorarioId); // tampoco se puede pedir: ya viene
    }

    [Fact]
    public async Task Grilla_ClaseQueYaPidio_VieneMarcada()
    {
        var clase = ClaseEn(MiClub);
        EnLaAgenda(clase);
        _solicitudes.Setup(s => s.ListarPorAlumnoAsync(AlumnoId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[]
                    {
                        new SolicitudCupo { AlumnoId = AlumnoId, HorarioId = clase.Id },
                    });

        var slot = Assert.Single(await _service.GrillaParaAlumnoAsync(AlumnoId));

        Assert.True(slot.SolicitudPendiente);
    }

    // ── La otra filtración de la misma pantalla: "Mis solicitudes" ──

    [Fact]
    public async Task Mis_IdentificaLaClasePorCuandoEs_NoPorSuNombre()
    {
        // El título de una clase de una persona ES el nombre de esa persona, así que
        // al alumno se le manda día/hora/sede y nada más.
        var clase = ClaseEn(MiClub);
        _solicitudes.Setup(s => s.ListarPorAlumnoAsync(AlumnoId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[]
                    {
                        new SolicitudCupo { AlumnoId = AlumnoId, HorarioId = clase.Id, Horario = clase },
                    });

        var mia = Assert.Single(await _service.MisAsync(AlumnoId));

        Assert.Equal(nameof(DayOfWeek.Tuesday), mia.Dia);
        Assert.Equal(new TimeOnly(18, 0), mia.HoraInicio);
        Assert.Equal("Mi club", mia.Sede);
        Assert.Equal(nameof(EstadoSolicitudGrupo.Pendiente), mia.Estado);
    }
}
