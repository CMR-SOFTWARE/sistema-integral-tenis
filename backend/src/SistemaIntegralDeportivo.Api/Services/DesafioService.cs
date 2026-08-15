using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Desafíos de ranking (singles): proponer → aceptar/rechazar → finalizar.
/// Todo cross-tenant — los jugadores pueden ser de cualquier academia.
/// </summary>
public interface IDesafioService
{
    Task<DesafioDto> ProponerAsync(Guid usuarioProponenteId, Guid rivalJugadorId, CancellationToken ct = default);
    Task AceptarAsync(Guid usuarioId, Guid juegoId, CancellationToken ct = default);
    Task RechazarAsync(Guid usuarioId, Guid juegoId, CancellationToken ct = default);
    Task CancelarAsync(Guid usuarioId, Guid juegoId, CancellationToken ct = default);
    Task<DesafioDto> FinalizarAsync(Guid usuarioId, Guid juegoId, Guid ganadorJugadorId, CancellationToken ct = default);
    Task<IReadOnlyList<DesafioDto>> MisPendientesAsync(Guid usuarioId, CancellationToken ct = default);
    Task<IReadOnlyList<DesafioDto>> MisFinalizadosAsync(Guid usuarioId, CancellationToken ct = default);

    /// <summary>Historial de partidos finalizados de CUALQUIER jugador (perfil público del ranking).</summary>
    Task<IReadOnlyList<DesafioDto>> FinalizadosDeJugadorAsync(Guid jugadorId, CancellationToken ct = default);
}

public class DesafioService : IDesafioService
{
    private readonly IJuegoPendienteRepository _desafios;
    private readonly IJugadorRankingRepository _jugadores;
    private readonly IPuntosMovimientoRepository _movimientos;
    private readonly IPoliticaDePuntosRanking _politica;
    private readonly INotificacionService _notificaciones;
    private readonly IRankingService _ranking;

    public DesafioService(
        IJuegoPendienteRepository desafios, IJugadorRankingRepository jugadores,
        IPuntosMovimientoRepository movimientos, IPoliticaDePuntosRanking politica,
        INotificacionService notificaciones, IRankingService ranking)
    {
        _desafios = desafios;
        _jugadores = jugadores;
        _movimientos = movimientos;
        _politica = politica;
        _notificaciones = notificaciones;
        _ranking = ranking;
    }

    public async Task<DesafioDto> ProponerAsync(
        Guid usuarioProponenteId, Guid rivalJugadorId, CancellationToken ct = default)
    {
        var proponente = await _jugadores.ObtenerPorUsuarioAsync(usuarioProponenteId, ct)
            ?? throw new ReglaDeNegocioException("Tenés que inscribirte al ranking antes de desafiar.");
        var rival = await _jugadores.ObtenerAsync(rivalJugadorId, ct)
            ?? throw new ReglaDeNegocioException("Ese jugador no existe.");
        if (proponente.Id == rival.Id)
            throw new ReglaDeNegocioException("No podés desafiarte a vos mismo.");
        // Un jugador solo puede tener un partido activo (Propuesto o Aceptado) a la vez.
        if (await _desafios.TieneActivoAsync(proponente.Id, ct))
            throw new ReglaDeNegocioException("Ya tenés un partido activo. Terminalo antes de desafiar a otro.");
        if (await _desafios.TieneActivoAsync(rival.Id, ct))
            throw new ReglaDeNegocioException("Ese jugador ya tiene un partido activo, no podés desafiarlo ahora.");

        var (menor, mayor) = ParNormalizado(proponente.Id, rival.Id);
        if (await _desafios.ExisteEntreAsync(menor, mayor, ct))
            throw new ReglaDeNegocioException("Ya jugaste (o tenés pendiente) un desafío con ese jugador.");

        var juego = new JuegoPendiente
        {
            Jugador1Id = proponente.Id,
            Jugador2Id = rival.Id,
            JugadorMenorId = menor,
            JugadorMayorId = mayor,
            CreadoPorUserId = usuarioProponenteId,
            Estado = EstadoJuegoPendiente.Propuesto,
        };
        await _desafios.AgregarAsync(juego, ct);
        await _desafios.GuardarCambiosAsync(ct);

        await _notificaciones.NotificarAsync(
            rival.UsuarioId, "DesafioRecibido", "Te desafiaron a un partido de ranking.", juego.Id, ct);

        return await MapearAsync(juego, ct);
    }

    public async Task AceptarAsync(Guid usuarioId, Guid juegoId, CancellationToken ct = default)
    {
        var juego = await ObtenerAsync(juegoId, ct);
        var jugador = await _jugadores.ObtenerPorUsuarioAsync(usuarioId, ct);
        if (jugador is null || juego.Jugador2Id != jugador.Id)
            throw new ReglaDeNegocioException("Solo el desafiado puede aceptar.");
        if (juego.Estado != EstadoJuegoPendiente.Propuesto)
            throw new ReglaDeNegocioException("Ese desafío ya no está pendiente.");

        juego.Estado = EstadoJuegoPendiente.Aceptado;
        juego.AceptadoEl = DateTime.UtcNow;
        await _desafios.GuardarCambiosAsync(ct);

        if (await _jugadores.ObtenerAsync(juego.Jugador1Id, ct) is { } proponente)
            await _notificaciones.NotificarAsync(
                proponente.UsuarioId, "DesafioAceptado", "Tu desafío fue aceptado.", juego.Id, ct);
    }

    public async Task RechazarAsync(Guid usuarioId, Guid juegoId, CancellationToken ct = default)
    {
        var juego = await ObtenerAsync(juegoId, ct);
        var jugador = await _jugadores.ObtenerPorUsuarioAsync(usuarioId, ct);
        if (jugador is null || juego.Jugador2Id != jugador.Id)
            throw new ReglaDeNegocioException("Solo el desafiado puede rechazar.");
        if (juego.Estado != EstadoJuegoPendiente.Propuesto)
            throw new ReglaDeNegocioException("Ese desafío ya no está pendiente.");

        var proponente = await _jugadores.ObtenerAsync(juego.Jugador1Id, ct);
        _desafios.Eliminar(juego); // todavía no generó ningún efecto irreversible: se borra, no queda historia
        await _desafios.GuardarCambiosAsync(ct);

        if (proponente is not null)
            await _notificaciones.NotificarAsync(
                proponente.UsuarioId, "DesafioRechazado", "Tu desafío fue rechazado.", null, ct);
    }

    public async Task CancelarAsync(Guid usuarioId, Guid juegoId, CancellationToken ct = default)
    {
        var juego = await ObtenerAsync(juegoId, ct);
        var jugador = await _jugadores.ObtenerPorUsuarioAsync(usuarioId, ct);
        var esParticipante = jugador is not null && (juego.Jugador1Id == jugador.Id || juego.Jugador2Id == jugador.Id);
        if (!esParticipante)
            throw new ReglaDeNegocioException("No participás de ese desafío.");
        if (juego.Estado == EstadoJuegoPendiente.Finalizado)
            throw new ReglaDeNegocioException("Ese desafío ya se jugó.");
        // Propuesto: solo quien lo propuso (el rival ya tiene "Rechazar" para eso).
        // Aceptado: cualquiera de los dos puede bajarse.
        if (juego.Estado == EstadoJuegoPendiente.Propuesto && juego.Jugador1Id != jugador!.Id)
            throw new ReglaDeNegocioException("Solo quien propuso el desafío puede cancelarlo antes de que se acepte.");

        var otroId = juego.Jugador1Id == jugador!.Id ? juego.Jugador2Id : juego.Jugador1Id;
        var otro = await _jugadores.ObtenerAsync(otroId, ct);
        _desafios.Eliminar(juego);
        await _desafios.GuardarCambiosAsync(ct);

        if (otro is not null)
            await _notificaciones.NotificarAsync(otro.UsuarioId, "DesafioCancelado", "El desafío se canceló.", null, ct);
    }

    public async Task<DesafioDto> FinalizarAsync(
        Guid usuarioId, Guid juegoId, Guid ganadorJugadorId, CancellationToken ct = default)
    {
        var juego = await ObtenerAsync(juegoId, ct);
        var jugador = await _jugadores.ObtenerPorUsuarioAsync(usuarioId, ct);
        var esParticipante = jugador is not null && (juego.Jugador1Id == jugador.Id || juego.Jugador2Id == jugador.Id);
        if (!esParticipante)
            throw new ReglaDeNegocioException("No participás de ese desafío.");
        if (juego.Estado != EstadoJuegoPendiente.Aceptado)
            throw new ReglaDeNegocioException("El desafío tiene que estar aceptado para cargar el resultado.");
        if (ganadorJugadorId != juego.Jugador1Id && ganadorJugadorId != juego.Jugador2Id)
            throw new ReglaDeNegocioException("El ganador tiene que ser uno de los dos jugadores del desafío.");

        var jugador1 = await _jugadores.ObtenerAsync(juego.Jugador1Id, ct)
            ?? throw new ReglaDeNegocioException("Uno de los jugadores ya no existe.");
        var jugador2 = await _jugadores.ObtenerAsync(juego.Jugador2Id, ct)
            ?? throw new ReglaDeNegocioException("Uno de los jugadores ya no existe.");
        var ganador = ganadorJugadorId == jugador1.Id ? jugador1 : jugador2;
        var perdedor = ganador == jugador1 ? jugador2 : jugador1;

        // El CF/rango que cuenta es el PROVISIONAL actual de cada uno, no uno cacheado propio.
        var rangoGanador = RangosRanking.De(ganador.PosicionProvisional ?? int.MaxValue);
        var rangoPerdedor = RangosRanking.De(perdedor.PosicionProvisional ?? int.MaxValue);
        var (puntosGanador, puntosPerdedor) = _politica.Calcular(rangoGanador, rangoPerdedor);

        await _movimientos.AgregarAsync(
            new PuntosMovimiento { JugadorRankingId = ganador.Id, Puntos = puntosGanador }, ct);
        await _movimientos.AgregarAsync(
            new PuntosMovimiento { JugadorRankingId = perdedor.Id, Puntos = puntosPerdedor }, ct);
        await _movimientos.GuardarCambiosAsync(ct);

        juego.Estado = EstadoJuegoPendiente.Finalizado;
        juego.GanadorId = ganador.Id;
        juego.PuntosGanador = puntosGanador;
        juego.PuntosPerdedor = puntosPerdedor;
        juego.FinalizadoEn = DateTime.UtcNow;
        await _desafios.GuardarCambiosAsync(ct);

        await _ranking.ActualizarRankingProvisionalAsync(ct); // reordena TODA la tabla, no solo estos dos

        var rivalUsuarioId = jugador!.Id == ganador.Id ? perdedor.UsuarioId : ganador.UsuarioId;
        await _notificaciones.NotificarAsync(
            rivalUsuarioId, "DesafioFinalizado", "Se cargó el resultado de tu desafío.", juego.Id, ct);

        return await MapearAsync(juego, ct);
    }

    public async Task<IReadOnlyList<DesafioDto>> MisPendientesAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var jugador = await _jugadores.ObtenerPorUsuarioAsync(usuarioId, ct);
        if (jugador is null) return [];

        var pendientes = await _desafios.MisPendientesAsync(jugador.Id, ct);
        var dtos = new List<DesafioDto>();
        foreach (var juego in pendientes) dtos.Add(await MapearAsync(juego, ct));
        return dtos;
    }

    public async Task<IReadOnlyList<DesafioDto>> MisFinalizadosAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var jugador = await _jugadores.ObtenerPorUsuarioAsync(usuarioId, ct);
        if (jugador is null) return [];

        var finalizados = await _desafios.MisFinalizadosAsync(jugador.Id, ct);
        var dtos = new List<DesafioDto>();
        foreach (var juego in finalizados) dtos.Add(await MapearAsync(juego, ct));
        return dtos;
    }

    public async Task<IReadOnlyList<DesafioDto>> FinalizadosDeJugadorAsync(Guid jugadorId, CancellationToken ct = default)
    {
        var finalizados = await _desafios.MisFinalizadosAsync(jugadorId, ct);
        var dtos = new List<DesafioDto>();
        foreach (var juego in finalizados) dtos.Add(await MapearAsync(juego, ct));
        return dtos;
    }

    private async Task<JuegoPendiente> ObtenerAsync(Guid juegoId, CancellationToken ct) =>
        await _desafios.ObtenerAsync(juegoId, ct) ?? throw new ReglaDeNegocioException("El desafío no existe.");

    private static (Guid Menor, Guid Mayor) ParNormalizado(Guid a, Guid b) =>
        a.CompareTo(b) < 0 ? (a, b) : (b, a);

    private async Task<DesafioDto> MapearAsync(JuegoPendiente juego, CancellationToken ct)
    {
        var j1 = await _jugadores.ObtenerConNombreAsync(juego.Jugador1Id, ct);
        var j2 = await _jugadores.ObtenerConNombreAsync(juego.Jugador2Id, ct);
        return new DesafioDto
        {
            Id = juego.Id,
            Jugador1Id = juego.Jugador1Id,
            Jugador1Nombre = j1 is null ? string.Empty : $"{j1.Nombre} {j1.Apellido}",
            Jugador2Id = juego.Jugador2Id,
            Jugador2Nombre = j2 is null ? string.Empty : $"{j2.Nombre} {j2.Apellido}",
            Estado = juego.Estado.ToString(),
            CreadoEl = juego.CreadoEl,
            AceptadoEl = juego.AceptadoEl,
            GanadorId = juego.GanadorId,
            PuntosGanador = juego.PuntosGanador,
            PuntosPerdedor = juego.PuntosPerdedor,
            FinalizadoEn = juego.FinalizadoEn,
        };
    }
}
