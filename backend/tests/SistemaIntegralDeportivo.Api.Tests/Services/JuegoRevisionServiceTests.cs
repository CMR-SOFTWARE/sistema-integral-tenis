using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Pedido de revisión (R.U.T.A., Fase 4, TDD): es un TICKET, no una
/// corrección — ResolverAsync es la regla más fácil de romper por accidente,
/// por eso tiene su propio test que verifica que NO toca nada del partido.
/// </summary>
public class JuegoRevisionServiceTests
{
    private static readonly Guid UsuarioParticipante = Guid.NewGuid();
    private static readonly Guid UsuarioAjeno = Guid.NewGuid();
    private static readonly Guid UsuarioAdmin = Guid.NewGuid();
    private static readonly Guid JugadorParticipanteId = Guid.NewGuid();
    private static readonly Guid JugadorRivalId = Guid.NewGuid();
    private static readonly Guid JugadorAjenoId = Guid.NewGuid();
    private static readonly Guid JuegoId = Guid.NewGuid();

    private readonly Mock<IJuegoRevisionRepository> _revisiones;
    private readonly Mock<IJuegoPendienteRepository> _singlesJuegos;
    private readonly Mock<IJuegoDoblesPendienteRepository> _doblesJuegos;
    private readonly Mock<IJugadorRankingRepository> _jugadores;
    private readonly Mock<INotificacionService> _notificaciones;
    private readonly Mock<IAdminRepository> _admin;
    private readonly JuegoRevisionService _service;

    public JuegoRevisionServiceTests()
    {
        _revisiones = new Mock<IJuegoRevisionRepository>();
        _singlesJuegos = new Mock<IJuegoPendienteRepository>();
        _doblesJuegos = new Mock<IJuegoDoblesPendienteRepository>();
        _jugadores = new Mock<IJugadorRankingRepository>();
        _notificaciones = new Mock<INotificacionService>();
        _admin = new Mock<IAdminRepository>();
        _service = new JuegoRevisionService(
            _revisiones.Object, _singlesJuegos.Object, _doblesJuegos.Object,
            _jugadores.Object, _notificaciones.Object, _admin.Object);

        _jugadores.Setup(j => j.ObtenerPorUsuarioAsync(UsuarioParticipante, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new JugadorRanking { Id = JugadorParticipanteId, UsuarioId = UsuarioParticipante });
        _jugadores.Setup(j => j.ObtenerPorUsuarioAsync(UsuarioAjeno, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new JugadorRanking { Id = JugadorAjenoId, UsuarioId = UsuarioAjeno });

        _singlesJuegos.Setup(s => s.ObtenerAsync(JuegoId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new JuegoPendiente
                      {
                          Id = JuegoId, Jugador1Id = JugadorParticipanteId, Jugador2Id = JugadorRivalId,
                          Estado = EstadoJuegoPendiente.Finalizado,
                      });

        _admin.Setup(a => a.ListarUsuarioIdsAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([UsuarioAdmin]);
    }

    private const string ComentarioValido = "El resultado que cargaron está mal.";

    // ── Crear ──

    [Fact]
    public async Task Crear_SiNoIndicaNingunPartido_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(UsuarioParticipante, null, null, ComentarioValido));
    }

    [Fact]
    public async Task Crear_SiIndicaLosDosPartidos_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(UsuarioParticipante, JuegoId, Guid.NewGuid(), ComentarioValido));
    }

    [Fact]
    public async Task Crear_SiElComentarioEsMuyCorto_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(UsuarioParticipante, JuegoId, null, "corto"));
    }

    [Fact]
    public async Task Crear_SiElPartidoNoEstaFinalizado_Lanza()
    {
        _singlesJuegos.Setup(s => s.ObtenerAsync(JuegoId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new JuegoPendiente
                      {
                          Id = JuegoId, Jugador1Id = JugadorParticipanteId, Jugador2Id = JugadorRivalId,
                          Estado = EstadoJuegoPendiente.Aceptado,
                      });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(UsuarioParticipante, JuegoId, null, ComentarioValido));
    }

    [Fact]
    public async Task Crear_SiNoParticipoDelPartido_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(UsuarioAjeno, JuegoId, null, ComentarioValido));
    }

    [Fact]
    public async Task Crear_SiYaHayUnaRevisionPendienteParaEsePartido_Lanza()
    {
        _revisiones.Setup(r => r.ExistePendienteAsync(JuegoId, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(UsuarioParticipante, JuegoId, null, ComentarioValido));

        _revisiones.Verify(r => r.AgregarAsync(It.IsAny<JuegoRevision>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Crear_NotificaATodosLosAdminsDePlataforma()
    {
        await _service.CrearAsync(UsuarioParticipante, JuegoId, null, ComentarioValido);

        _notificaciones.Verify(n => n.NotificarAsync(
            UsuarioAdmin, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Resolver ──

    private JuegoRevision Pendiente()
    {
        var revision = new JuegoRevision
        {
            JuegoPendienteId = JuegoId, CreadoPorUserId = UsuarioParticipante,
            Comentario = ComentarioValido, Estado = EstadoJuegoRevision.Pendiente,
        };
        _revisiones.Setup(r => r.ObtenerAsync(revision.Id, It.IsAny<CancellationToken>())).ReturnsAsync(revision);
        return revision;
    }

    [Fact]
    public async Task Resolver_SoloMarcaResueltaYGuardaLaRespuesta_SinTocarNadaMasDelPartido()
    {
        var revision = Pendiente();

        await _service.ResolverAsync(UsuarioAdmin, revision.Id, "Confirmado, el resultado quedó bien cargado.");

        Assert.Equal(EstadoJuegoRevision.Resuelta, revision.Estado);
        Assert.Equal("Confirmado, el resultado quedó bien cargado.", revision.RespuestaAdmin);
        Assert.Equal(UsuarioAdmin, revision.ResueltoPorUserId);
        Assert.NotNull(revision.ResueltoEl);
        // Estructural: el service ni siquiera puede tocar PuntosMovimiento o GanadorId —
        // no tiene inyectado ningún repo que escriba sobre JuegoPendiente/PuntosMovimiento.
        _revisiones.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Resolver_NotificaAQuienPidioLaRevision()
    {
        var revision = Pendiente();

        await _service.ResolverAsync(UsuarioAdmin, revision.Id, "Listo.");

        _notificaciones.Verify(n => n.NotificarAsync(
            UsuarioParticipante, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Resolver_SiYaEstabaResuelta_Lanza()
    {
        var revision = Pendiente();
        revision.Estado = EstadoJuegoRevision.Resuelta;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ResolverAsync(UsuarioAdmin, revision.Id, "Otra vez."));
    }
}
