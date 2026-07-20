using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Clase individual fija (M5b, TDD): el alumno propone SEDE + día/hora/duración;
/// se valida que haya cancha libre EN ESA SEDE; el profe acepta eligiendo una
/// cancha (se crea el horario individual) o rechaza.
/// </summary>
public class SolicitudHorarioServiceTests
{
    private static readonly Guid AlumnoId = Guid.NewGuid();
    private static readonly Guid SedeId = Guid.NewGuid();
    private static readonly Guid CanchaId = Guid.NewGuid();

    private readonly Mock<ISolicitudHorarioRepository> _solicitudes;
    private readonly Mock<IAlumnoRepository> _alumnos;
    private readonly Mock<ISedeRepository> _sedes;
    private readonly Mock<IHorarioRepository> _horarios;
    private readonly Mock<ICargoRepository> _cargos;
    private readonly Mock<IHorarioService> _horarioService;
    private readonly SolicitudHorarioService _service;

    public SolicitudHorarioServiceTests()
    {
        _solicitudes = new Mock<ISolicitudHorarioRepository>();
        _alumnos = new Mock<IAlumnoRepository>();
        _sedes = new Mock<ISedeRepository>();
        _horarios = new Mock<IHorarioRepository>();
        _cargos = new Mock<ICargoRepository>();
        _horarioService = new Mock<IHorarioService>();
        _service = new SolicitudHorarioService(
            _solicitudes.Object, _alumnos.Object, _sedes.Object,
            _horarios.Object, _cargos.Object, _horarioService.Object);

        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AlumnoActivo());
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        // La sede existe, activa, con una cancha; sin horarios ese día (= libre)
        _sedes.Setup(s => s.ObtenerAsync(SedeId, It.IsAny<CancellationToken>())).ReturnsAsync(SedeConCancha());
        _horarios.Setup(h => h.ListarPorCanchaYDiaAsync(It.IsAny<Guid>(), It.IsAny<DayOfWeek>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);
    }

    private static Alumno AlumnoActivo() => new()
    {
        Id = AlumnoId, Nombre = "Lucas", Apellido = "C", Dni = "1", Telefono = "1",
        FechaNacimiento = DateTime.UtcNow.AddYears(-25), Estado = EstadoAlumno.Activo,
    };

    private static Sede SedeConCancha(params (Guid id, bool activa)[] extra)
    {
        var sede = new Sede { Id = SedeId, Nombre = "Central", Activo = true };
        sede.Canchas.Add(new Cancha { Id = CanchaId, Nombre = "Cancha 1", Activo = true });
        foreach (var (id, activa) in extra)
            sede.Canchas.Add(new Cancha { Id = id, Nombre = "Cancha X", Activo = activa });
        return sede;
    }

    // ── Solicitar ──

    [Fact]
    public async Task Solicitar_HayCanchaLibreEnLaSede_CreaPendiente()
    {
        SolicitudHorario? creada = null;
        _solicitudes.Setup(s => s.AgregarAsync(It.IsAny<SolicitudHorario>(), It.IsAny<CancellationToken>()))
                    .Callback((SolicitudHorario s, CancellationToken _) => creada = s)
                    .Returns(Task.CompletedTask);

        var dto = await _service.SolicitarAsync(AlumnoId, SedeId, DayOfWeek.Tuesday, new TimeOnly(18, 0), 60);

        Assert.Equal("Pendiente", dto.Estado);
        Assert.NotNull(creada);
        Assert.Equal(SedeId, creada!.SedeId);
        Assert.Equal(DayOfWeek.Tuesday, creada.Dia);
        Assert.Equal(new TimeOnly(18, 0), creada.HoraInicio);
    }

    [Fact]
    public async Task Solicitar_SedeInexistente_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.SolicitarAsync(AlumnoId, Guid.NewGuid(), DayOfWeek.Tuesday, new TimeOnly(18, 0), 60));
    }

    [Fact]
    public async Task Solicitar_SinCanchasLibresEnLaSede_Lanza()
    {
        // La única cancha de la sede está ocupada 18:00-19:00 ese día
        _horarios.Setup(h => h.ListarPorCanchaYDiaAsync(CanchaId, DayOfWeek.Tuesday, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new Horario { CanchaId = CanchaId, Dia = DayOfWeek.Tuesday, HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60 }]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.SolicitarAsync(AlumnoId, SedeId, DayOfWeek.Tuesday, new TimeOnly(18, 30), 60));

        _solicitudes.Verify(s => s.AgregarAsync(It.IsAny<SolicitudHorario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Solicitar_ConDeudaVencida_Lanza()
    {
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([new Cargo
               {
                   AlumnoId = AlumnoId, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m,
                   Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2),
               }]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.SolicitarAsync(AlumnoId, SedeId, DayOfWeek.Tuesday, new TimeOnly(18, 0), 60));
    }

    [Fact]
    public async Task Solicitar_AlumnoNoActivo_Lanza()
    {
        var pausado = AlumnoActivo();
        pausado.Estado = EstadoAlumno.Suspendido;
        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>())).ReturnsAsync(pausado);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.SolicitarAsync(AlumnoId, SedeId, DayOfWeek.Tuesday, new TimeOnly(18, 0), 60));
    }

    [Fact]
    public async Task Solicitar_YaTienePendienteEseDiaHora_Lanza()
    {
        _solicitudes.Setup(s => s.ExistePendienteAsync(AlumnoId, DayOfWeek.Tuesday, new TimeOnly(18, 0), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.SolicitarAsync(AlumnoId, SedeId, DayOfWeek.Tuesday, new TimeOnly(18, 0), 60));
    }

    // ── Canchas libres (dentro de la sede) ──

    [Fact]
    public async Task CanchasLibres_ExcluyeLaOcupada_DentroDeLaSede()
    {
        var ocupada = Guid.NewGuid();
        _sedes.Setup(s => s.ObtenerAsync(SedeId, It.IsAny<CancellationToken>()))
              .ReturnsAsync(SedeConCancha((ocupada, true)));
        _horarios.Setup(h => h.ListarPorCanchaYDiaAsync(ocupada, DayOfWeek.Tuesday, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new Horario { CanchaId = ocupada, Dia = DayOfWeek.Tuesday, HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60 }]);

        var libres = await _service.CanchasLibresAsync(SedeId, DayOfWeek.Tuesday, new TimeOnly(18, 0), 60);

        var c = Assert.Single(libres);
        Assert.Equal(CanchaId, c.CanchaId); // solo la Cancha 1 (la otra está ocupada)
    }

    // ── Aceptar / Rechazar (profe) ──

    [Fact]
    public async Task Aceptar_CreaElHorarioIndividual_YMarcaAceptada()
    {
        var solicitud = new SolicitudHorario { AlumnoId = AlumnoId, SedeId = SedeId, Dia = DayOfWeek.Tuesday, HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60, Estado = EstadoSolicitudHorario.Pendiente };
        _solicitudes.Setup(s => s.ObtenerAsync(solicitud.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitud);
        var horarioId = Guid.NewGuid();
        _horarioService.Setup(h => h.CrearAsync(It.IsAny<CreateHorarioDto>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new HorarioResponseDto { Id = horarioId });

        await _service.AceptarAsync(solicitud.Id, CanchaId);

        _horarioService.Verify(h => h.CrearAsync(
            It.Is<CreateHorarioDto>(d => d.CanchaId == CanchaId && d.AlumnoId == AlumnoId
                && d.Dia == DayOfWeek.Tuesday && d.HoraInicio == new TimeOnly(18, 0) && d.GrupoId == null),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(EstadoSolicitudHorario.Aceptada, solicitud.Estado);
        Assert.Equal(CanchaId, solicitud.CanchaId);
        Assert.Equal(horarioId, solicitud.HorarioId);
        Assert.NotNull(solicitud.ResueltoEl);
    }

    [Fact]
    public async Task Aceptar_SiLaCanchaSeOcupo_NoMarcaAceptada()
    {
        var solicitud = new SolicitudHorario { AlumnoId = AlumnoId, SedeId = SedeId, Dia = DayOfWeek.Tuesday, HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60, Estado = EstadoSolicitudHorario.Pendiente };
        _solicitudes.Setup(s => s.ObtenerAsync(solicitud.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitud);
        _horarioService.Setup(h => h.CrearAsync(It.IsAny<CreateHorarioDto>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new ReglaDeNegocioException("Se superpone con otro horario."));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AceptarAsync(solicitud.Id, CanchaId));

        Assert.Equal(EstadoSolicitudHorario.Pendiente, solicitud.Estado); // queda para reintentar
    }

    [Fact]
    public async Task Aceptar_YaResuelta_Lanza()
    {
        var solicitud = new SolicitudHorario { AlumnoId = AlumnoId, SedeId = SedeId, Estado = EstadoSolicitudHorario.Rechazada };
        _solicitudes.Setup(s => s.ObtenerAsync(solicitud.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitud);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AceptarAsync(solicitud.Id, CanchaId));
        _horarioService.Verify(h => h.CrearAsync(It.IsAny<CreateHorarioDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Rechazar_MarcaRechazada_SinCrearHorario()
    {
        var solicitud = new SolicitudHorario { AlumnoId = AlumnoId, SedeId = SedeId, Estado = EstadoSolicitudHorario.Pendiente };
        _solicitudes.Setup(s => s.ObtenerAsync(solicitud.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitud);

        await _service.RechazarAsync(solicitud.Id);

        Assert.Equal(EstadoSolicitudHorario.Rechazada, solicitud.Estado);
        _horarioService.Verify(h => h.CrearAsync(It.IsAny<CreateHorarioDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
