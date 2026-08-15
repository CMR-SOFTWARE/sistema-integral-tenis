using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Cierre oficial (R.U.T.A., Fase 4, TDD): evitar doble-cierre el mismo día
/// (regla con estado, mockeada) + el armado de snapshots por scope geográfico
/// (lógica pura, sin mocks — es la parte con reglas de negocio reales).
/// </summary>
public class RankingCierreOficialServiceTests
{
    private readonly Mock<IRankingSnapshotRepository> _snapshots;
    private readonly Mock<IJugadorRankingRepository> _singles;
    private readonly Mock<IJugadorRankingDoblesRepository> _dobles;
    private readonly RankingCierreOficialService _service;

    public RankingCierreOficialServiceTests()
    {
        _snapshots = new Mock<IRankingSnapshotRepository>();
        _singles = new Mock<IJugadorRankingRepository>();
        _dobles = new Mock<IJugadorRankingDoblesRepository>();
        _service = new RankingCierreOficialService(_snapshots.Object, _singles.Object, _dobles.Object);

        _singles.Setup(s => s.ListarTodosActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _dobles.Setup(d => d.ListarTodosActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    [Fact]
    public async Task CerrarOficial_SiYaCerroHoy_Lanza()
    {
        _snapshots.Setup(s => s.YaCerroHoyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CerrarOficialAsync());

        _snapshots.Verify(s => s.AgregarRangoAsync(It.IsAny<IEnumerable<RankingSnapshot>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CerrarOficial_SiNoCerroHoy_PersisteYDevuelveLaCantidad()
    {
        _singles.Setup(s => s.ListarTodosActivosAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([new JugadorRanking { Id = Guid.NewGuid(), PosicionProvisional = 1, RangoProvisional = "A", CfProvisional = 200 }]);

        var cantidad = await _service.CerrarOficialAsync();

        Assert.Equal(1, cantidad); // 1 fila global, sin geo cargada => sin filas de scope local
        _snapshots.Verify(s => s.AgregarRangoAsync(It.Is<IEnumerable<RankingSnapshot>>(l => l.Count() == 1), It.IsAny<CancellationToken>()), Times.Once);
        _snapshots.Verify(s => s.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── ArmarSnapshots (pura) ──

    private static FilaParaSnapshot Fila(int posicionGlobal, int puntos, int ordenInscripcion, string? ciudad = null) =>
        new(Guid.NewGuid(), posicionGlobal, puntos, "A", 200, ordenInscripcion, ciudad, null, null);

    [Fact]
    public void ArmarSnapshots_UnaFilaGlobalPorJugador_ConLaPosicionYaCalculada()
    {
        var filas = new[] { Fila(posicionGlobal: 5, puntos: 300, ordenInscripcion: 1) };

        var snapshots = RankingCierreOficialService.ArmarSnapshots(filas, ModalidadRanking.Singles, DateTime.UtcNow);

        var global = Assert.Single(snapshots);
        Assert.Equal(ScopeRanking.Global, global.Scope);
        Assert.Null(global.ScopeValor);
        Assert.Equal(5, global.Posicion); // NO se recalcula, usa la que ya tenía
    }

    [Fact]
    public void ArmarSnapshots_ReordenaLocalmenteDentroDeCadaCiudad_IgnorandoLaPosicionGlobal()
    {
        // Globalmente el orden es Juan(#2) > Ana(#8) > Pipo(#40), pero los 3 son de "Rosario":
        // localmente Rosario tiene que quedar Juan #1, Ana #2, Pipo #3.
        var juan = Fila(posicionGlobal: 2, puntos: 500, ordenInscripcion: 1, ciudad: "Rosario");
        var ana = Fila(posicionGlobal: 8, puntos: 300, ordenInscripcion: 2, ciudad: "Rosario");
        var pipo = Fila(posicionGlobal: 40, puntos: 100, ordenInscripcion: 3, ciudad: "Rosario");
        var otro = Fila(posicionGlobal: 1, puntos: 900, ordenInscripcion: 4, ciudad: "Córdoba"); // #1 global, otra ciudad

        var snapshots = RankingCierreOficialService.ArmarSnapshots(
            [juan, ana, pipo, otro], ModalidadRanking.Singles, DateTime.UtcNow);

        var rosario = snapshots.Where(s => s.Scope == ScopeRanking.Ciudad && s.ScopeValor == "Rosario")
            .OrderBy(s => s.Posicion).ToList();
        Assert.Equal(3, rosario.Count);
        Assert.Equal(juan.JugadorId, rosario[0].JugadorId);
        Assert.Equal(1, rosario[0].Posicion);
        Assert.Equal(ana.JugadorId, rosario[1].JugadorId);
        Assert.Equal(2, rosario[1].Posicion);
        Assert.Equal(pipo.JugadorId, rosario[2].JugadorId);
        Assert.Equal(3, rosario[2].Posicion);
    }

    [Fact]
    public void ArmarSnapshots_DesempataPorOrdenInscripcion_NuncaPorPuntosIgualesSinCriterio()
    {
        var inscriptoDespues = Fila(posicionGlobal: 1, puntos: 100, ordenInscripcion: 5, ciudad: "Rosario");
        var inscriptoAntes = Fila(posicionGlobal: 2, puntos: 100, ordenInscripcion: 1, ciudad: "Rosario");

        var snapshots = RankingCierreOficialService.ArmarSnapshots(
            [inscriptoDespues, inscriptoAntes], ModalidadRanking.Singles, DateTime.UtcNow);

        var rosario = snapshots.Where(s => s.Scope == ScopeRanking.Ciudad).OrderBy(s => s.Posicion).ToList();
        Assert.Equal(inscriptoAntes.JugadorId, rosario[0].JugadorId); // mismo puntaje: gana quien se inscribió antes
    }

    [Fact]
    public void ArmarSnapshots_JugadorSinEsaGeoCargada_NoEntraEnEseScope()
    {
        var sinCiudad = Fila(posicionGlobal: 1, puntos: 999, ordenInscripcion: 1, ciudad: null);

        var snapshots = RankingCierreOficialService.ArmarSnapshots([sinCiudad], ModalidadRanking.Singles, DateTime.UtcNow);

        Assert.DoesNotContain(snapshots, s => s.Scope == ScopeRanking.Ciudad);
        Assert.Single(snapshots); // solo el Global
    }
}
