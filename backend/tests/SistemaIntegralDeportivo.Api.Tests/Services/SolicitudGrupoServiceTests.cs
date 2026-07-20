using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reservar horario fijo grupal (M5a, TDD): el alumno solo ve grupos con cupo
/// y de su categoría (con precio estimado ÷ miembros+él); pide, el profe
/// acepta (lo suma) o rechaza.
/// </summary>
public class SolicitudGrupoServiceTests
{
    private const decimal ValorHora = 16_000m;
    private static readonly Guid AlumnoId = Guid.NewGuid();

    private readonly Mock<ISolicitudGrupoRepository> _solicitudes;
    private readonly Mock<IGrupoRepository> _grupos;
    private readonly Mock<IAlumnoRepository> _alumnos;
    private readonly Mock<IHorarioRepository> _horarios;
    private readonly Mock<ITenantRepository> _tenant;
    private readonly Mock<IGrupoService> _grupoService;
    private readonly SolicitudGrupoService _service;

    public SolicitudGrupoServiceTests()
    {
        _solicitudes = new Mock<ISolicitudGrupoRepository>();
        _grupos = new Mock<IGrupoRepository>();
        _alumnos = new Mock<IAlumnoRepository>();
        _horarios = new Mock<IHorarioRepository>();
        _tenant = new Mock<ITenantRepository>();
        _grupoService = new Mock<IGrupoService>();
        _service = new SolicitudGrupoService(
            _solicitudes.Object, _grupos.Object, _alumnos.Object,
            _horarios.Object, _tenant.Object, _grupoService.Object);

        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AlumnoDe(CategoriaAlumno.Cuarta));
        _tenant.Setup(t => t.ObtenerActualAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Tenant { Subdominio = "d", Nombre = "Demo", ValorHoraGrupal = ValorHora });
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _solicitudes.Setup(s => s.ListarPorAlumnoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync([]);
    }

    private static Alumno AlumnoDe(CategoriaAlumno cat) => new()
    {
        Id = AlumnoId, Nombre = "Lucas", Apellido = "C", Dni = "1", Telefono = "1",
        FechaNacimiento = DateTime.UtcNow.AddYears(-25), Categoria = cat,
    };

    private static Grupo Grupo(CategoriaAlumno? cat, int? cupo, params (Guid id, bool activo)[] miembros)
    {
        var g = new Grupo { Nombre = "Intermedios", Categoria = cat, CupoMaximo = cupo, Activo = true };
        foreach (var (id, activo) in miembros)
            g.Alumnos.Add(new AlumnoGrupo { AlumnoId = id, GrupoId = g.Id, FechaBaja = activo ? null : DateTime.UtcNow });
        return g;
    }

    private void ConGrupos(params Grupo[] grupos) =>
        _grupos.Setup(g => g.ListarAsync(It.IsAny<CancellationToken>())).ReturnsAsync(grupos);

    // ── Grupos disponibles: filtros ──

    [Fact]
    public async Task Disponibles_ExcluyeDondeYaEsMiembro_SinCupo_YOtraCategoria()
    {
        var yaMiembro = Grupo(CategoriaAlumno.Cuarta, 4, (AlumnoId, true));
        var lleno = Grupo(CategoriaAlumno.Cuarta, 2, (Guid.NewGuid(), true), (Guid.NewGuid(), true));
        var otraCat = Grupo(CategoriaAlumno.Primera, 4, (Guid.NewGuid(), true));
        var ok = Grupo(CategoriaAlumno.Cuarta, 4, (Guid.NewGuid(), true));
        ConGrupos(yaMiembro, lleno, otraCat, ok);

        var res = await _service.DisponiblesParaAlumnoAsync(AlumnoId);

        var g = Assert.Single(res);
        Assert.Equal(ok.Id, g.GrupoId);
    }

    [Fact]
    public async Task Disponibles_GrupoSinCategoria_EsAbiertoATodos()
    {
        ConGrupos(Grupo(null, 4, (Guid.NewGuid(), true)));

        var res = await _service.DisponiblesParaAlumnoAsync(AlumnoId);

        Assert.Single(res);
    }

    [Fact]
    public async Task Disponibles_PrecioEstimado_DivideEntreMiembrosMasElAlumno()
    {
        // Grupo de 3 activos + él = ÷4: 16.000 ÷ 4 = 4.000
        var g = Grupo(CategoriaAlumno.Cuarta, 6, (Guid.NewGuid(), true), (Guid.NewGuid(), true), (Guid.NewGuid(), true));
        ConGrupos(g);
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new Horario { GrupoId = g.Id, CanchaId = Guid.NewGuid(), Dia = DayOfWeek.Tuesday, HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60 }]);

        var res = await _service.DisponiblesParaAlumnoAsync(AlumnoId);

        var horario = Assert.Single(res[0].Horarios);
        Assert.Equal(4_000m, horario.PrecioEstimado);
    }

    [Fact]
    public async Task Disponibles_IncluyeCategoriaAdyacente_ExcluyeLasLejanas()
    {
        // Alumno Cuarta: ve Tercera (una arriba), Cuarta (la suya) y Quinta (una
        // abajo); NO ve Segunda (2 arriba) ni Sexta (2 abajo).
        var tercera = Grupo(CategoriaAlumno.Tercera, 4, (Guid.NewGuid(), true));
        var cuarta = Grupo(CategoriaAlumno.Cuarta, 4, (Guid.NewGuid(), true));
        var quinta = Grupo(CategoriaAlumno.Quinta, 4, (Guid.NewGuid(), true));
        var segunda = Grupo(CategoriaAlumno.Segunda, 4, (Guid.NewGuid(), true));
        var sexta = Grupo(CategoriaAlumno.Sexta, 4, (Guid.NewGuid(), true));
        ConGrupos(tercera, cuarta, quinta, segunda, sexta);

        var res = await _service.DisponiblesParaAlumnoAsync(AlumnoId);

        var ids = res.Select(g => g.GrupoId).ToHashSet();
        Assert.Equal(3, ids.Count);
        Assert.Contains(tercera.Id, ids);
        Assert.Contains(cuarta.Id, ids);
        Assert.Contains(quinta.Id, ids);
    }

    [Fact]
    public async Task Disponibles_AlumnoSinCategoria_SoloVeGruposAbiertos()
    {
        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AlumnoDe(CategoriaAlumno.SinCategoria));
        var abierto = Grupo(null, 4, (Guid.NewGuid(), true));
        var septima = Grupo(CategoriaAlumno.Septima, 4, (Guid.NewGuid(), true));
        ConGrupos(abierto, septima);

        var res = await _service.DisponiblesParaAlumnoAsync(AlumnoId);

        var g = Assert.Single(res);
        Assert.Equal(abierto.Id, g.GrupoId);
    }

    [Fact]
    public async Task Disponibles_MarcaLosQueYaSolicito()
    {
        var g = Grupo(CategoriaAlumno.Cuarta, 4, (Guid.NewGuid(), true));
        ConGrupos(g);
        _solicitudes.Setup(s => s.ListarPorAlumnoAsync(AlumnoId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync([new SolicitudGrupo { AlumnoId = AlumnoId, GrupoId = g.Id, Estado = EstadoSolicitudGrupo.Pendiente }]);

        var res = await _service.DisponiblesParaAlumnoAsync(AlumnoId);

        Assert.True(res[0].SolicitudPendiente);
    }

    // ── Solicitar ──

    [Fact]
    public async Task Solicitar_CasoFeliz_CreaPendiente()
    {
        var g = Grupo(CategoriaAlumno.Cuarta, 4, (Guid.NewGuid(), true));
        _grupos.Setup(x => x.ObtenerAsync(g.Id, It.IsAny<CancellationToken>())).ReturnsAsync(g);
        SolicitudGrupo? creada = null;
        _solicitudes.Setup(s => s.AgregarAsync(It.IsAny<SolicitudGrupo>(), It.IsAny<CancellationToken>()))
                    .Callback((SolicitudGrupo s, CancellationToken _) => creada = s)
                    .Returns(Task.CompletedTask);

        var dto = await _service.SolicitarAsync(AlumnoId, g.Id);

        Assert.Equal("Pendiente", dto.Estado);
        Assert.NotNull(creada);
        Assert.Equal(EstadoSolicitudGrupo.Pendiente, creada!.Estado);
    }

    [Fact]
    public async Task Solicitar_YaMiembro_Lanza()
    {
        var g = Grupo(CategoriaAlumno.Cuarta, 4, (AlumnoId, true));
        _grupos.Setup(x => x.ObtenerAsync(g.Id, It.IsAny<CancellationToken>())).ReturnsAsync(g);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.SolicitarAsync(AlumnoId, g.Id));
    }

    [Fact]
    public async Task Solicitar_CategoriaAdyacente_CreaPendiente()
    {
        // Alumno Cuarta pide un grupo de Tercera (una categoría más): permitido.
        var g = Grupo(CategoriaAlumno.Tercera, 4, (Guid.NewGuid(), true));
        _grupos.Setup(x => x.ObtenerAsync(g.Id, It.IsAny<CancellationToken>())).ReturnsAsync(g);

        var dto = await _service.SolicitarAsync(AlumnoId, g.Id);

        Assert.Equal("Pendiente", dto.Estado);
    }

    [Fact]
    public async Task Solicitar_CategoriaLejana_Lanza()
    {
        // Primera está a 3 categorías de Cuarta: fuera del alcance (±1).
        var g = Grupo(CategoriaAlumno.Primera, 4, (Guid.NewGuid(), true));
        _grupos.Setup(x => x.ObtenerAsync(g.Id, It.IsAny<CancellationToken>())).ReturnsAsync(g);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.SolicitarAsync(AlumnoId, g.Id));
    }

    [Fact]
    public async Task Solicitar_SinCupo_Lanza()
    {
        var g = Grupo(CategoriaAlumno.Cuarta, 2, (Guid.NewGuid(), true), (Guid.NewGuid(), true));
        _grupos.Setup(x => x.ObtenerAsync(g.Id, It.IsAny<CancellationToken>())).ReturnsAsync(g);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.SolicitarAsync(AlumnoId, g.Id));
    }

    [Fact]
    public async Task Solicitar_YaTienePendiente_Lanza()
    {
        var g = Grupo(CategoriaAlumno.Cuarta, 4, (Guid.NewGuid(), true));
        _grupos.Setup(x => x.ObtenerAsync(g.Id, It.IsAny<CancellationToken>())).ReturnsAsync(g);
        _solicitudes.Setup(s => s.ExistePendienteAsync(AlumnoId, g.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.SolicitarAsync(AlumnoId, g.Id));
    }

    // ── Aceptar / Rechazar (profe) ──

    [Fact]
    public async Task Aceptar_SumaAlAlumnoAlGrupo_YMarcaAceptada()
    {
        var solicitud = new SolicitudGrupo { AlumnoId = AlumnoId, GrupoId = Guid.NewGuid(), Estado = EstadoSolicitudGrupo.Pendiente };
        _solicitudes.Setup(s => s.ObtenerAsync(solicitud.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitud);

        await _service.AceptarAsync(solicitud.Id);

        _grupoService.Verify(g => g.AsignarAlumnoAsync(solicitud.GrupoId, AlumnoId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(EstadoSolicitudGrupo.Aceptada, solicitud.Estado);
        Assert.NotNull(solicitud.ResueltoEl);
    }

    [Fact]
    public async Task Aceptar_SiAsignarFalla_NoMarcaAceptada()
    {
        var solicitud = new SolicitudGrupo { AlumnoId = AlumnoId, GrupoId = Guid.NewGuid(), Estado = EstadoSolicitudGrupo.Pendiente };
        _solicitudes.Setup(s => s.ObtenerAsync(solicitud.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitud);
        _grupoService.Setup(g => g.AsignarAlumnoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new ReglaDeNegocioException("El grupo está completo."));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AceptarAsync(solicitud.Id));

        Assert.Equal(EstadoSolicitudGrupo.Pendiente, solicitud.Estado); // queda para reintentar
    }

    [Fact]
    public async Task Aceptar_YaResuelta_Lanza()
    {
        var solicitud = new SolicitudGrupo { AlumnoId = AlumnoId, GrupoId = Guid.NewGuid(), Estado = EstadoSolicitudGrupo.Aceptada };
        _solicitudes.Setup(s => s.ObtenerAsync(solicitud.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitud);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AceptarAsync(solicitud.Id));
        _grupoService.Verify(g => g.AsignarAlumnoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Rechazar_MarcaRechazada_SinSumar()
    {
        var solicitud = new SolicitudGrupo { AlumnoId = AlumnoId, GrupoId = Guid.NewGuid(), Estado = EstadoSolicitudGrupo.Pendiente };
        _solicitudes.Setup(s => s.ObtenerAsync(solicitud.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitud);

        await _service.RechazarAsync(solicitud.Id);

        Assert.Equal(EstadoSolicitudGrupo.Rechazada, solicitud.Estado);
        _grupoService.Verify(g => g.AsignarAlumnoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
