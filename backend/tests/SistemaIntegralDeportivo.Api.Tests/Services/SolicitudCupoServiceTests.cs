using Moq;
using SistemaIntegralDeportivo.Api.Common;
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

    private readonly Mock<ISolicitudCupoRepository> _solicitudes;
    private readonly Mock<IAlumnoRepository> _alumnos;
    private readonly Mock<IHorarioRepository> _horarios;
    private readonly Mock<ITenantRepository> _tenant;
    private readonly Mock<ICargoRepository> _cargos;
    private readonly Mock<IHorarioService> _horarioService;
    private readonly SolicitudCupoService _service;

    public SolicitudCupoServiceTests()
    {
        _solicitudes = new Mock<ISolicitudCupoRepository>();
        _alumnos = new Mock<IAlumnoRepository>();
        _horarios = new Mock<IHorarioRepository>();
        _tenant = new Mock<ITenantRepository>();
        _cargos = new Mock<ICargoRepository>();
        _horarioService = new Mock<IHorarioService>();
        _service = new SolicitudCupoService(
            _solicitudes.Object, _alumnos.Object, _horarios.Object,
            _tenant.Object, _cargos.Object, _horarioService.Object);

        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AlumnoActivo());
        // Por defecto: no debe nada
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _horarios.Setup(h => h.ObtenerConRosterAsync(HorarioId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(ClaseConLugar());
        _solicitudes.Setup(s => s.ExistePendienteAsync(AlumnoId, HorarioId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
    }

    private static Alumno AlumnoActivo() => new()
    {
        Id = AlumnoId, Nombre = "Lucas", Apellido = "C", Telefono = "1",
        Estado = EstadoAlumno.Activo, Categoria = CategoriaAlumno.SinCategoria,
    };

    /// <summary>Clase activa, sin categoría (abierta a todos) y con lugar.</summary>
    private static Horario ClaseConLugar() => new()
    {
        Id = HorarioId, CanchaId = Guid.NewGuid(), Activo = true, CupoMaximo = 4,
        Dia = DayOfWeek.Tuesday, HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60,
    };

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
}
