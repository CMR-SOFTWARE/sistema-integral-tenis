using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Desafíos de ranking (R.U.T.A., Fase 2, TDD): proponer → aceptar/rechazar →
/// finalizar. Rechazar/Cancelar mientras Propuesto borran el registro (sin
/// historia); Finalizado nunca se borra. Un par de jugadores solo se
/// enfrenta una vez (bloqueo incluye Finalizado).
/// </summary>
public class DesafioServiceTests
{
    private static readonly Guid UsuarioProponente = Guid.NewGuid();
    private static readonly Guid UsuarioRival = Guid.NewGuid();
    private static readonly Guid JugadorProponenteId = Guid.NewGuid();
    private static readonly Guid JugadorRivalId = Guid.NewGuid();

    private readonly Mock<IJuegoPendienteRepository> _desafios;
    private readonly Mock<IJugadorRankingRepository> _jugadores;
    private readonly Mock<IPuntosMovimientoRepository> _movimientos;
    private readonly Mock<IPoliticaDePuntosRanking> _politica;
    private readonly Mock<INotificacionService> _notificaciones;
    private readonly Mock<IRankingService> _ranking;
    private readonly DesafioService _service;

    public DesafioServiceTests()
    {
        _desafios = new Mock<IJuegoPendienteRepository>();
        _jugadores = new Mock<IJugadorRankingRepository>();
        _movimientos = new Mock<IPuntosMovimientoRepository>();
        _politica = new Mock<IPoliticaDePuntosRanking>();
        _notificaciones = new Mock<INotificacionService>();
        _ranking = new Mock<IRankingService>();
        _service = new DesafioService(
            _desafios.Object, _jugadores.Object, _movimientos.Object,
            _politica.Object, _notificaciones.Object, _ranking.Object);

        _jugadores.Setup(j => j.ObtenerPorUsuarioAsync(UsuarioProponente, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Proponente());
        _jugadores.Setup(j => j.ObtenerPorUsuarioAsync(UsuarioRival, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Rival());
        _jugadores.Setup(j => j.ObtenerAsync(JugadorProponenteId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Proponente());
        _jugadores.Setup(j => j.ObtenerAsync(JugadorRivalId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Rival());
    }

    private static JugadorRanking Proponente() => new()
    {
        Id = JugadorProponenteId, UsuarioId = UsuarioProponente,
        PosicionProvisional = 1, RangoProvisional = "A", CfProvisional = 200,
    };

    private static JugadorRanking Rival() => new()
    {
        Id = JugadorRivalId, UsuarioId = UsuarioRival,
        PosicionProvisional = 2, RangoProvisional = "A", CfProvisional = 200,
    };

    // ── Proponer ──

    [Fact]
    public async Task Proponer_CreaDesafioPendiente_ConParNormalizado()
    {
        JuegoPendiente? creado = null;
        _desafios.Setup(d => d.AgregarAsync(It.IsAny<JuegoPendiente>(), It.IsAny<CancellationToken>()))
                 .Callback((JuegoPendiente j, CancellationToken _) => creado = j).Returns(Task.CompletedTask);

        await _service.ProponerAsync(UsuarioProponente, JugadorRivalId);

        Assert.NotNull(creado);
        Assert.Equal(JugadorProponenteId, creado!.Jugador1Id);
        Assert.Equal(JugadorRivalId, creado.Jugador2Id);
        Assert.Equal(EstadoJuegoPendiente.Propuesto, creado.Estado);
        var (menorEsperado, mayorEsperado) = JugadorProponenteId.CompareTo(JugadorRivalId) < 0
            ? (JugadorProponenteId, JugadorRivalId) : (JugadorRivalId, JugadorProponenteId);
        Assert.Equal(menorEsperado, creado.JugadorMenorId);
        Assert.Equal(mayorEsperado, creado.JugadorMayorId);
    }

    [Fact]
    public async Task Proponer_NotificaAlRival()
    {
        await _service.ProponerAsync(UsuarioProponente, JugadorRivalId);

        _notificaciones.Verify(n => n.NotificarAsync(
            UsuarioRival, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Proponer_SiYaHayCualquierDesafioEntreLosMismosDos_Lanza()
    {
        _desafios.Setup(d => d.ExisteEntreAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true); // incluye Finalizado: el repo no distingue estado a propósito

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ProponerAsync(UsuarioProponente, JugadorRivalId));

        _desafios.Verify(d => d.AgregarAsync(It.IsAny<JuegoPendiente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Proponer_NoPuedeDesafiarseASiMismo_Lanza()
    {
        _jugadores.Setup(j => j.ObtenerAsync(JugadorProponenteId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Proponente());

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ProponerAsync(UsuarioProponente, JugadorProponenteId));
    }

    [Fact]
    public async Task Proponer_SiElProponenteYaTieneUnPartidoActivo_Lanza()
    {
        _desafios.Setup(d => d.TieneActivoAsync(JugadorProponenteId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ProponerAsync(UsuarioProponente, JugadorRivalId));

        _desafios.Verify(d => d.AgregarAsync(It.IsAny<JuegoPendiente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Proponer_SiElRivalYaTieneUnPartidoActivo_Lanza()
    {
        _desafios.Setup(d => d.TieneActivoAsync(JugadorRivalId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ProponerAsync(UsuarioProponente, JugadorRivalId));

        _desafios.Verify(d => d.AgregarAsync(It.IsAny<JuegoPendiente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Proponer_SiNoEstaInscriptoEnElRanking_Lanza()
    {
        _jugadores.Setup(j => j.ObtenerPorUsuarioAsync(UsuarioProponente, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((JugadorRanking?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ProponerAsync(UsuarioProponente, JugadorRivalId));
    }

    // ── Aceptar / Rechazar / Cancelar ──

    private JuegoPendiente PropuestoPorMi() => Propuesto(JugadorProponenteId, JugadorRivalId);

    private JuegoPendiente Propuesto(Guid jugador1, Guid jugador2)
    {
        var juego = new JuegoPendiente
        {
            Jugador1Id = jugador1, Jugador2Id = jugador2,
            CreadoPorUserId = UsuarioProponente, Estado = EstadoJuegoPendiente.Propuesto,
        };
        _desafios.Setup(d => d.ObtenerAsync(juego.Id, It.IsAny<CancellationToken>())).ReturnsAsync(juego);
        return juego;
    }

    [Fact]
    public async Task Aceptar_SoloElRival_CambiaEstado()
    {
        var juego = PropuestoPorMi();

        await _service.AceptarAsync(UsuarioRival, juego.Id);

        Assert.Equal(EstadoJuegoPendiente.Aceptado, juego.Estado);
        Assert.NotNull(juego.AceptadoEl);
    }

    [Fact]
    public async Task Aceptar_NotificaAlProponente()
    {
        var juego = PropuestoPorMi();

        await _service.AceptarAsync(UsuarioRival, juego.Id);

        _notificaciones.Verify(n => n.NotificarAsync(
            UsuarioProponente, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Aceptar_SiLoIntentaElProponente_Lanza()
    {
        var juego = PropuestoPorMi();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AceptarAsync(UsuarioProponente, juego.Id));
    }

    [Fact]
    public async Task Rechazar_BorraElRegistro_SinDejarHistoria()
    {
        var juego = PropuestoPorMi();

        await _service.RechazarAsync(UsuarioRival, juego.Id);

        _desafios.Verify(d => d.Eliminar(juego), Times.Once);
    }

    [Fact]
    public async Task Rechazar_SoloElRival_Lanza()
    {
        var juego = PropuestoPorMi();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.RechazarAsync(UsuarioProponente, juego.Id));

        _desafios.Verify(d => d.Eliminar(It.IsAny<JuegoPendiente>()), Times.Never);
    }

    [Fact]
    public async Task Cancelar_MientrasPropuesto_SoloElProponente()
    {
        var juego = PropuestoPorMi();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarAsync(UsuarioRival, juego.Id));

        _desafios.Verify(d => d.Eliminar(It.IsAny<JuegoPendiente>()), Times.Never);
    }

    [Fact]
    public async Task Cancelar_UnaVezAceptado_CualquieraDeLosDos_BorraElRegistro()
    {
        var juego = PropuestoPorMi();
        juego.Estado = EstadoJuegoPendiente.Aceptado;

        await _service.CancelarAsync(UsuarioRival, juego.Id); // el rival, no quien propuso

        _desafios.Verify(d => d.Eliminar(juego), Times.Once);
    }

    [Fact]
    public async Task Cancelar_UnDesafioYaFinalizado_Lanza()
    {
        var juego = PropuestoPorMi();
        juego.Estado = EstadoJuegoPendiente.Finalizado;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarAsync(UsuarioProponente, juego.Id));
    }

    // ── Finalizar ──

    [Fact]
    public async Task Finalizar_UsaLaPoliticaConLosRangosProvisionalesActuales_YCreaLosMovimientos()
    {
        var juego = PropuestoPorMi();
        juego.Estado = EstadoJuegoPendiente.Aceptado;
        _politica.Setup(p => p.Calcular(It.IsAny<RangoCf>(), It.IsAny<RangoCf>())).Returns((200, 180));
        var movimientos = new List<PuntosMovimiento>();
        _movimientos.Setup(m => m.AgregarAsync(It.IsAny<PuntosMovimiento>(), It.IsAny<CancellationToken>()))
                    .Callback((PuntosMovimiento pm, CancellationToken _) => movimientos.Add(pm))
                    .Returns(Task.CompletedTask);

        await _service.FinalizarAsync(UsuarioProponente, juego.Id, JugadorProponenteId);

        Assert.Equal(2, movimientos.Count);
        Assert.Contains(movimientos, m => m.JugadorRankingId == JugadorProponenteId && m.Puntos == 200);
        Assert.Contains(movimientos, m => m.JugadorRankingId == JugadorRivalId && m.Puntos == 180);
    }

    [Fact]
    public async Task Finalizar_MarcaFinalizadoConGanadorYPuntos_YDisparaElRecalculoGlobal()
    {
        var juego = PropuestoPorMi();
        juego.Estado = EstadoJuegoPendiente.Aceptado;
        _politica.Setup(p => p.Calcular(It.IsAny<RangoCf>(), It.IsAny<RangoCf>())).Returns((200, 180));

        await _service.FinalizarAsync(UsuarioProponente, juego.Id, JugadorProponenteId);

        Assert.Equal(EstadoJuegoPendiente.Finalizado, juego.Estado);
        Assert.Equal(JugadorProponenteId, juego.GanadorId);
        Assert.Equal(200, juego.PuntosGanador);
        Assert.Equal(180, juego.PuntosPerdedor);
        Assert.NotNull(juego.FinalizadoEn);
        _ranking.Verify(r => r.ActualizarRankingProvisionalAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Finalizar_SiElDesafioNoEstaAceptado_Lanza()
    {
        var juego = PropuestoPorMi(); // sigue en Propuesto

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.FinalizarAsync(UsuarioProponente, juego.Id, JugadorProponenteId));

        _ranking.Verify(r => r.ActualizarRankingProvisionalAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Finalizar_ElGanadorTieneQueSerUnoDeLosDosJugadores_Lanza()
    {
        var juego = PropuestoPorMi();
        juego.Estado = EstadoJuegoPendiente.Aceptado;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.FinalizarAsync(UsuarioProponente, juego.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Finalizar_ElPerdedorNuncaSumaCero()
    {
        var juego = PropuestoPorMi();
        juego.Estado = EstadoJuegoPendiente.Aceptado;
        // Política real (no mock) para verificar la regla de negocio de punta a punta
        var servicioReal = new DesafioService(
            _desafios.Object, _jugadores.Object, _movimientos.Object,
            new PuntosCfConsolacionV1(), _notificaciones.Object, _ranking.Object);

        await servicioReal.FinalizarAsync(UsuarioProponente, juego.Id, JugadorProponenteId);

        Assert.True(juego.PuntosPerdedor > 0);
    }
}
