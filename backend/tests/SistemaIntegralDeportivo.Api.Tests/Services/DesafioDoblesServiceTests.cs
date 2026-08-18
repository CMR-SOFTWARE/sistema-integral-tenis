using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Desafíos de dobles (R.U.T.A., Fase 3, TDD): espejo de DesafioServiceTests
/// pero con 4 jugadores (2 parejas ad-hoc). Revancha permitida tras
/// Finalizado (a diferencia de singles); bloqueo solo mientras Propuesto/Aceptado.
/// </summary>
public class DesafioDoblesServiceTests
{
    private static readonly Guid UsuarioProponente = Guid.NewGuid();
    private static readonly Guid UsuarioCompanero = Guid.NewGuid();
    private static readonly Guid UsuarioRival1 = Guid.NewGuid();
    private static readonly Guid UsuarioRival2 = Guid.NewGuid();
    private static readonly Guid JugadorProponenteId = Guid.NewGuid();
    private static readonly Guid JugadorCompaneroId = Guid.NewGuid();
    private static readonly Guid JugadorRival1Id = Guid.NewGuid();
    private static readonly Guid JugadorRival2Id = Guid.NewGuid();
    private static readonly Guid DoblesProponenteId = Guid.NewGuid();
    private static readonly Guid DoblesCompaneroId = Guid.NewGuid();
    private static readonly Guid DoblesRival1Id = Guid.NewGuid();
    private static readonly Guid DoblesRival2Id = Guid.NewGuid();

    private readonly Mock<IJuegoDoblesPendienteRepository> _desafios;
    private readonly Mock<IJugadorRankingRepository> _jugadoresSingles;
    private readonly Mock<IJugadorRankingDoblesRepository> _jugadoresDobles;
    private readonly Mock<IPuntosMovimientoDoblesRepository> _movimientos;
    private readonly Mock<IPoliticaDePuntosRanking> _politica;
    private readonly Mock<INotificacionService> _notificaciones;
    private readonly Mock<IRankingDoblesService> _ranking;
    private readonly DesafioDoblesService _service;

    public DesafioDoblesServiceTests()
    {
        _desafios = new Mock<IJuegoDoblesPendienteRepository>();
        _jugadoresSingles = new Mock<IJugadorRankingRepository>();
        _jugadoresDobles = new Mock<IJugadorRankingDoblesRepository>();
        _movimientos = new Mock<IPuntosMovimientoDoblesRepository>();
        _politica = new Mock<IPoliticaDePuntosRanking>();
        _notificaciones = new Mock<INotificacionService>();
        _ranking = new Mock<IRankingDoblesService>();
        _service = new DesafioDoblesService(
            _desafios.Object, _jugadoresSingles.Object, _jugadoresDobles.Object,
            _movimientos.Object, _politica.Object, _notificaciones.Object, _ranking.Object);

        void SetupSingles(Guid usuarioId, Guid jugadorId)
        {
            var jugador = new JugadorRanking { Id = jugadorId, UsuarioId = usuarioId, PosicionProvisional = 1 };
            _jugadoresSingles.Setup(j => j.ObtenerPorUsuarioAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(jugador);
            _jugadoresSingles.Setup(j => j.ObtenerAsync(jugadorId, It.IsAny<CancellationToken>())).ReturnsAsync(jugador);
        }
        SetupSingles(UsuarioProponente, JugadorProponenteId);
        SetupSingles(UsuarioCompanero, JugadorCompaneroId);
        SetupSingles(UsuarioRival1, JugadorRival1Id);
        SetupSingles(UsuarioRival2, JugadorRival2Id);

        void SetupDobles(Guid jugadorSinglesId, Guid doblesId)
        {
            var dobles = new JugadorRankingDobles { Id = doblesId, JugadorRankingId = jugadorSinglesId, PosicionProvisional = 1 };
            _jugadoresDobles.Setup(d => d.ObtenerPorJugadorRankingIdAsync(jugadorSinglesId, It.IsAny<CancellationToken>())).ReturnsAsync(dobles);
        }
        SetupDobles(JugadorProponenteId, DoblesProponenteId);
        SetupDobles(JugadorCompaneroId, DoblesCompaneroId);
        SetupDobles(JugadorRival1Id, DoblesRival1Id);
        SetupDobles(JugadorRival2Id, DoblesRival2Id);
    }

    // ── Proponer ──

    [Fact]
    public async Task Proponer_CreaDesafioPendiente_ConLos4Jugadores()
    {
        JuegoDoblesPendiente? creado = null;
        _desafios.Setup(d => d.AgregarAsync(It.IsAny<JuegoDoblesPendiente>(), It.IsAny<CancellationToken>()))
                 .Callback((JuegoDoblesPendiente j, CancellationToken _) => creado = j).Returns(Task.CompletedTask);

        await _service.ProponerAsync(UsuarioProponente, JugadorCompaneroId, JugadorRival1Id, JugadorRival2Id);

        Assert.NotNull(creado);
        Assert.Equal(JugadorProponenteId, creado!.Jugador1Id);
        Assert.Equal(JugadorCompaneroId, creado.Jugador2Id);
        Assert.Equal(JugadorRival1Id, creado.Rival1Id);
        Assert.Equal(JugadorRival2Id, creado.Rival2Id);
        Assert.Equal(EstadoJuegoPendiente.Propuesto, creado.Estado);
    }

    [Fact]
    public async Task Proponer_NotificaALosDosRivales()
    {
        await _service.ProponerAsync(UsuarioProponente, JugadorCompaneroId, JugadorRival1Id, JugadorRival2Id);

        _notificaciones.Verify(n => n.NotificarAsync(
            UsuarioRival1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificaciones.Verify(n => n.NotificarAsync(
            UsuarioRival2, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Proponer_SiUnJugadorSeRepite_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ProponerAsync(UsuarioProponente, JugadorProponenteId, JugadorRival1Id, JugadorRival2Id));

        _desafios.Verify(d => d.AgregarAsync(It.IsAny<JuegoDoblesPendiente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Proponer_SiAlgunoNoEstaInscriptoEnDobles_Lanza()
    {
        _jugadoresDobles.Setup(d => d.ObtenerPorJugadorRankingIdAsync(JugadorRival2Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((JugadorRankingDobles?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ProponerAsync(UsuarioProponente, JugadorCompaneroId, JugadorRival1Id, JugadorRival2Id));
    }

    [Fact]
    public async Task Proponer_SiUnJugadorYaTieneUnPartidoDeDoblesActivo_Lanza()
    {
        _desafios.Setup(d => d.TieneActivoAsync(JugadorCompaneroId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ProponerAsync(UsuarioProponente, JugadorCompaneroId, JugadorRival1Id, JugadorRival2Id));

        _desafios.Verify(d => d.AgregarAsync(It.IsAny<JuegoDoblesPendiente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Proponer_SiYaHayUnDesafioActivoEntreLasMismasDosParejas_Lanza()
    {
        _desafios.Setup(d => d.ExisteActivoEntreParejasAsync(
                     JugadorProponenteId, JugadorCompaneroId, JugadorRival1Id, JugadorRival2Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ProponerAsync(UsuarioProponente, JugadorCompaneroId, JugadorRival1Id, JugadorRival2Id));

        _desafios.Verify(d => d.AgregarAsync(It.IsAny<JuegoDoblesPendiente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Aceptar / Rechazar / Cancelar ──

    private JuegoDoblesPendiente PropuestoPorMiPareja()
    {
        var juego = new JuegoDoblesPendiente
        {
            Jugador1Id = JugadorProponenteId, Jugador2Id = JugadorCompaneroId,
            Rival1Id = JugadorRival1Id, Rival2Id = JugadorRival2Id,
            CreadoPorUserId = UsuarioProponente, Estado = EstadoJuegoPendiente.Propuesto,
        };
        _desafios.Setup(d => d.ObtenerAsync(juego.Id, It.IsAny<CancellationToken>())).ReturnsAsync(juego);
        return juego;
    }

    [Fact]
    public async Task Aceptar_CualquieraDeLosDosRivales_CambiaEstado()
    {
        var juego = PropuestoPorMiPareja();

        await _service.AceptarAsync(UsuarioRival2, juego.Id); // el rival 2, no el 1

        Assert.Equal(EstadoJuegoPendiente.Aceptado, juego.Estado);
        Assert.NotNull(juego.AceptadoEl);
    }

    [Fact]
    public async Task Aceptar_SiLoIntentaLaParejaQuePropuso_Lanza()
    {
        var juego = PropuestoPorMiPareja();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AceptarAsync(UsuarioCompanero, juego.Id));
    }

    [Fact]
    public async Task Rechazar_BorraElRegistro_SinDejarHistoria()
    {
        var juego = PropuestoPorMiPareja();

        await _service.RechazarAsync(UsuarioRival1, juego.Id);

        _desafios.Verify(d => d.Eliminar(juego), Times.Once);
    }

    [Fact]
    public async Task Cancelar_MientrasPropuesto_SoloLaParejaQuePropuso()
    {
        var juego = PropuestoPorMiPareja();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarAsync(UsuarioRival1, juego.Id));

        _desafios.Verify(d => d.Eliminar(It.IsAny<JuegoDoblesPendiente>()), Times.Never);
    }

    [Fact]
    public async Task Cancelar_UnaVezAceptado_CualquieraDeLos4_BorraElRegistro()
    {
        var juego = PropuestoPorMiPareja();
        juego.Estado = EstadoJuegoPendiente.Aceptado;

        await _service.CancelarAsync(UsuarioRival2, juego.Id);

        _desafios.Verify(d => d.Eliminar(juego), Times.Once);
    }

    // ── Finalizar ──

    [Fact]
    public async Task Finalizar_SumaPuntosALosDosGanadoresYALosDosPerdedores()
    {
        var juego = PropuestoPorMiPareja();
        juego.Estado = EstadoJuegoPendiente.Aceptado;
        _politica.Setup(p => p.Calcular(It.IsAny<RangoCf>(), It.IsAny<RangoCf>())).Returns((200, 180));
        var movimientos = new List<PuntosMovimientoDobles>();
        _movimientos.Setup(m => m.AgregarAsync(It.IsAny<PuntosMovimientoDobles>(), It.IsAny<CancellationToken>()))
                    .Callback((PuntosMovimientoDobles pm, CancellationToken _) => movimientos.Add(pm))
                    .Returns(Task.CompletedTask);

        await _service.FinalizarAsync(UsuarioProponente, juego.Id, JugadorProponenteId); // gana la pareja A

        Assert.Equal(4, movimientos.Count);
        Assert.Contains(movimientos, m => m.JugadorRankingDoblesId == DoblesProponenteId && m.Puntos == 200);
        Assert.Contains(movimientos, m => m.JugadorRankingDoblesId == DoblesCompaneroId && m.Puntos == 200);
        Assert.Contains(movimientos, m => m.JugadorRankingDoblesId == DoblesRival1Id && m.Puntos == 180);
        Assert.Contains(movimientos, m => m.JugadorRankingDoblesId == DoblesRival2Id && m.Puntos == 180);
    }

    [Fact]
    public async Task Finalizar_MarcaFinalizadoYDisparaElRecalculoGlobalDeDobles()
    {
        var juego = PropuestoPorMiPareja();
        juego.Estado = EstadoJuegoPendiente.Aceptado;
        _politica.Setup(p => p.Calcular(It.IsAny<RangoCf>(), It.IsAny<RangoCf>())).Returns((200, 180));

        await _service.FinalizarAsync(UsuarioRival1, juego.Id, JugadorRival1Id); // gana la pareja B (rivales)

        Assert.Equal(EstadoJuegoPendiente.Finalizado, juego.Estado);
        Assert.Equal(false, juego.GanoParejaA);
        Assert.Equal(200, juego.PuntosGanadores);
        Assert.Equal(180, juego.PuntosPerdedores);
        _ranking.Verify(r => r.ActualizarRankingProvisionalAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Finalizar_SiElDesafioNoEstaAceptado_Lanza()
    {
        var juego = PropuestoPorMiPareja(); // sigue en Propuesto

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.FinalizarAsync(UsuarioProponente, juego.Id, JugadorProponenteId));

        _ranking.Verify(r => r.ActualizarRankingProvisionalAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Finalizar_ElGanadorTieneQueSerUnoDeLos4_Lanza()
    {
        var juego = PropuestoPorMiPareja();
        juego.Estado = EstadoJuegoPendiente.Aceptado;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.FinalizarAsync(UsuarioProponente, juego.Id, Guid.NewGuid()));
    }
}
