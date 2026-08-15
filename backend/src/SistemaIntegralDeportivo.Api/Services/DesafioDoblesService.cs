using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Desafíos de dobles: proponer → aceptar/rechazar → finalizar. Espejo de
/// DesafioService pero con 4 jugadores. El CF/rango de cada PAREJA para la
/// fórmula es el peor (índice más alto) entre el rango de dobles de sus dos
/// integrantes — mismo criterio "peor rango" que ya usa PuntosCfConsolacionV1
/// para dos jugadores individuales, extendido naturalmente a un equipo de dos.
/// </summary>
public interface IDesafioDoblesService
{
    Task<DesafioDoblesDto> ProponerAsync(
        Guid usuarioProponenteId, Guid companeroJugadorId, Guid rival1JugadorId, Guid rival2JugadorId,
        CancellationToken ct = default);
    Task AceptarAsync(Guid usuarioId, Guid juegoId, CancellationToken ct = default);
    Task RechazarAsync(Guid usuarioId, Guid juegoId, CancellationToken ct = default);
    Task CancelarAsync(Guid usuarioId, Guid juegoId, CancellationToken ct = default);
    Task<DesafioDoblesDto> FinalizarAsync(Guid usuarioId, Guid juegoId, Guid ganadorJugadorId, CancellationToken ct = default);
    Task<IReadOnlyList<DesafioDoblesDto>> MisPendientesAsync(Guid usuarioId, CancellationToken ct = default);
    Task<IReadOnlyList<DesafioDoblesDto>> MisFinalizadosAsync(Guid usuarioId, CancellationToken ct = default);

    /// <summary>Historial de partidos finalizados de CUALQUIER jugador (perfil público de dobles).</summary>
    Task<IReadOnlyList<DesafioDoblesDto>> FinalizadosDeJugadorAsync(Guid jugadorId, CancellationToken ct = default);
}

public class DesafioDoblesService : IDesafioDoblesService
{
    private readonly IJuegoDoblesPendienteRepository _desafios;
    private readonly IJugadorRankingRepository _jugadoresSingles;
    private readonly IJugadorRankingDoblesRepository _jugadoresDobles;
    private readonly IPuntosMovimientoDoblesRepository _movimientos;
    private readonly IPoliticaDePuntosRanking _politica;
    private readonly INotificacionService _notificaciones;
    private readonly IRankingDoblesService _ranking;

    public DesafioDoblesService(
        IJuegoDoblesPendienteRepository desafios, IJugadorRankingRepository jugadoresSingles,
        IJugadorRankingDoblesRepository jugadoresDobles, IPuntosMovimientoDoblesRepository movimientos,
        IPoliticaDePuntosRanking politica, INotificacionService notificaciones, IRankingDoblesService ranking)
    {
        _desafios = desafios;
        _jugadoresSingles = jugadoresSingles;
        _jugadoresDobles = jugadoresDobles;
        _movimientos = movimientos;
        _politica = politica;
        _notificaciones = notificaciones;
        _ranking = ranking;
    }

    public async Task<DesafioDoblesDto> ProponerAsync(
        Guid usuarioProponenteId, Guid companeroJugadorId, Guid rival1JugadorId, Guid rival2JugadorId,
        CancellationToken ct = default)
    {
        var proponente = await _jugadoresSingles.ObtenerPorUsuarioAsync(usuarioProponenteId, ct)
            ?? throw new ReglaDeNegocioException("Tenés que inscribirte al ranking antes de desafiar.");

        var ids = new[] { proponente.Id, companeroJugadorId, rival1JugadorId, rival2JugadorId };
        if (ids.Distinct().Count() != 4)
            throw new ReglaDeNegocioException("Los 4 jugadores del partido tienen que ser distintos.");

        foreach (var id in ids)
            if (await _jugadoresDobles.ObtenerPorJugadorRankingIdAsync(id, ct) is null)
                throw new ReglaDeNegocioException("Los 4 jugadores tienen que estar inscriptos en el ranking de dobles.");

        // Un jugador solo puede tener un partido de dobles activo a la vez.
        foreach (var id in ids)
            if (await _desafios.TieneActivoAsync(id, ct))
                throw new ReglaDeNegocioException("Uno de los jugadores ya tiene un partido de dobles activo.");

        if (await _desafios.ExisteActivoEntreParejasAsync(proponente.Id, companeroJugadorId, rival1JugadorId, rival2JugadorId, ct))
            throw new ReglaDeNegocioException("Ya hay un desafío pendiente entre estas dos parejas.");

        var juego = new JuegoDoblesPendiente
        {
            Jugador1Id = proponente.Id,
            Jugador2Id = companeroJugadorId,
            Rival1Id = rival1JugadorId,
            Rival2Id = rival2JugadorId,
            CreadoPorUserId = usuarioProponenteId,
            Estado = EstadoJuegoPendiente.Propuesto,
        };
        await _desafios.AgregarAsync(juego, ct);
        await _desafios.GuardarCambiosAsync(ct);

        var rival1 = await _jugadoresSingles.ObtenerAsync(rival1JugadorId, ct);
        var rival2 = await _jugadoresSingles.ObtenerAsync(rival2JugadorId, ct);
        if (rival1 is not null)
            await _notificaciones.NotificarAsync(
                rival1.UsuarioId, "DesafioDoblesRecibido", "Te desafiaron a un partido de dobles de ranking.", juego.Id, ct);
        if (rival2 is not null)
            await _notificaciones.NotificarAsync(
                rival2.UsuarioId, "DesafioDoblesRecibido", "Te desafiaron a un partido de dobles de ranking.", juego.Id, ct);

        return await MapearAsync(juego, ct);
    }

    public async Task AceptarAsync(Guid usuarioId, Guid juegoId, CancellationToken ct = default)
    {
        var juego = await ObtenerAsync(juegoId, ct);
        var jugador = await _jugadoresSingles.ObtenerPorUsuarioAsync(usuarioId, ct);
        if (jugador is null || (juego.Rival1Id != jugador.Id && juego.Rival2Id != jugador.Id))
            throw new ReglaDeNegocioException("Solo un jugador de la pareja desafiada puede aceptar.");
        if (juego.Estado != EstadoJuegoPendiente.Propuesto)
            throw new ReglaDeNegocioException("Ese desafío ya no está pendiente.");

        juego.Estado = EstadoJuegoPendiente.Aceptado;
        juego.AceptadoEl = DateTime.UtcNow;
        await _desafios.GuardarCambiosAsync(ct);

        await NotificarAParejaAsync(juego.Jugador1Id, juego.Jugador2Id, "DesafioDoblesAceptado", "Tu desafío de dobles fue aceptado.", juego.Id, ct);
    }

    public async Task RechazarAsync(Guid usuarioId, Guid juegoId, CancellationToken ct = default)
    {
        var juego = await ObtenerAsync(juegoId, ct);
        var jugador = await _jugadoresSingles.ObtenerPorUsuarioAsync(usuarioId, ct);
        if (jugador is null || (juego.Rival1Id != jugador.Id && juego.Rival2Id != jugador.Id))
            throw new ReglaDeNegocioException("Solo un jugador de la pareja desafiada puede rechazar.");
        if (juego.Estado != EstadoJuegoPendiente.Propuesto)
            throw new ReglaDeNegocioException("Ese desafío ya no está pendiente.");

        _desafios.Eliminar(juego); // todavía no generó ningún efecto irreversible: se borra, no queda historia
        await _desafios.GuardarCambiosAsync(ct);

        await NotificarAParejaAsync(juego.Jugador1Id, juego.Jugador2Id, "DesafioDoblesRechazado", "Tu desafío de dobles fue rechazado.", null, ct);
    }

    public async Task CancelarAsync(Guid usuarioId, Guid juegoId, CancellationToken ct = default)
    {
        var juego = await ObtenerAsync(juegoId, ct);
        var jugador = await _jugadoresSingles.ObtenerPorUsuarioAsync(usuarioId, ct);
        var esParticipante = jugador is not null && EsParticipante(juego, jugador.Id);
        if (!esParticipante)
            throw new ReglaDeNegocioException("No participás de ese desafío.");
        if (juego.Estado == EstadoJuegoPendiente.Finalizado)
            throw new ReglaDeNegocioException("Ese desafío ya se jugó.");

        var esPropiaPareja = jugador!.Id == juego.Jugador1Id || jugador.Id == juego.Jugador2Id;
        // Propuesto: solo la pareja que propuso (la rival ya tiene "Rechazar" para eso).
        // Aceptado: cualquiera de los 4 puede bajarse.
        if (juego.Estado == EstadoJuegoPendiente.Propuesto && !esPropiaPareja)
            throw new ReglaDeNegocioException("Solo quien propuso el desafío puede cancelarlo antes de que se acepte.");

        _desafios.Eliminar(juego);
        await _desafios.GuardarCambiosAsync(ct);

        await NotificarATodosMenosAsync(juego, usuarioId, "DesafioDoblesCancelado", "El desafío de dobles se canceló.", null, ct);
    }

    public async Task<DesafioDoblesDto> FinalizarAsync(
        Guid usuarioId, Guid juegoId, Guid ganadorJugadorId, CancellationToken ct = default)
    {
        var juego = await ObtenerAsync(juegoId, ct);
        var jugador = await _jugadoresSingles.ObtenerPorUsuarioAsync(usuarioId, ct);
        if (jugador is null || !EsParticipante(juego, jugador.Id))
            throw new ReglaDeNegocioException("No participás de ese desafío.");
        if (juego.Estado != EstadoJuegoPendiente.Aceptado)
            throw new ReglaDeNegocioException("El desafío tiene que estar aceptado para cargar el resultado.");
        if (!EsParticipante(juego, ganadorJugadorId))
            throw new ReglaDeNegocioException("El ganador tiene que ser uno de los 4 jugadores del desafío.");

        var ganoParejaA = ganadorJugadorId == juego.Jugador1Id || ganadorJugadorId == juego.Jugador2Id;
        var (ganadoresIds, perdedoresIds) = ganoParejaA
            ? ((juego.Jugador1Id, juego.Jugador2Id), (juego.Rival1Id, juego.Rival2Id))
            : ((juego.Rival1Id, juego.Rival2Id), (juego.Jugador1Id, juego.Jugador2Id));

        var rangoGanadores = await PeorRangoDeParejaAsync(ganadoresIds.Item1, ganadoresIds.Item2, ct);
        var rangoPerdedores = await PeorRangoDeParejaAsync(perdedoresIds.Item1, perdedoresIds.Item2, ct);
        var (puntosGanadores, puntosPerdedores) = _politica.Calcular(rangoGanadores, rangoPerdedores);

        foreach (var id in new[] { ganadoresIds.Item1, ganadoresIds.Item2 })
            await AgregarMovimientoAsync(id, puntosGanadores, ct);
        foreach (var id in new[] { perdedoresIds.Item1, perdedoresIds.Item2 })
            await AgregarMovimientoAsync(id, puntosPerdedores, ct);
        await _movimientos.GuardarCambiosAsync(ct);

        juego.Estado = EstadoJuegoPendiente.Finalizado;
        juego.GanoParejaA = ganoParejaA;
        juego.PuntosGanadores = puntosGanadores;
        juego.PuntosPerdedores = puntosPerdedores;
        juego.FinalizadoEn = DateTime.UtcNow;
        await _desafios.GuardarCambiosAsync(ct);

        await _ranking.ActualizarRankingProvisionalAsync(ct); // reordena TODA la tabla de dobles

        await NotificarATodosMenosAsync(juego, usuarioId, "DesafioDoblesFinalizado", "Se cargó el resultado de tu partido de dobles.", juego.Id, ct);

        return await MapearAsync(juego, ct);
    }

    public async Task<IReadOnlyList<DesafioDoblesDto>> MisPendientesAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var jugador = await _jugadoresSingles.ObtenerPorUsuarioAsync(usuarioId, ct);
        if (jugador is null) return [];

        var pendientes = await _desafios.MisPendientesAsync(jugador.Id, ct);
        var dtos = new List<DesafioDoblesDto>();
        foreach (var juego in pendientes) dtos.Add(await MapearAsync(juego, ct));
        return dtos;
    }

    public async Task<IReadOnlyList<DesafioDoblesDto>> MisFinalizadosAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var jugador = await _jugadoresSingles.ObtenerPorUsuarioAsync(usuarioId, ct);
        if (jugador is null) return [];

        var finalizados = await _desafios.MisFinalizadosAsync(jugador.Id, ct);
        var dtos = new List<DesafioDoblesDto>();
        foreach (var juego in finalizados) dtos.Add(await MapearAsync(juego, ct));
        return dtos;
    }

    public async Task<IReadOnlyList<DesafioDoblesDto>> FinalizadosDeJugadorAsync(Guid jugadorId, CancellationToken ct = default)
    {
        var finalizados = await _desafios.MisFinalizadosAsync(jugadorId, ct);
        var dtos = new List<DesafioDoblesDto>();
        foreach (var juego in finalizados) dtos.Add(await MapearAsync(juego, ct));
        return dtos;
    }

    private async Task AgregarMovimientoAsync(Guid jugadorSinglesId, int puntos, CancellationToken ct)
    {
        var dobles = await _jugadoresDobles.ObtenerPorJugadorRankingIdAsync(jugadorSinglesId, ct)
            ?? throw new ReglaDeNegocioException("Uno de los jugadores ya no está inscripto en dobles.");
        await _movimientos.AgregarAsync(new PuntosMovimientoDobles { JugadorRankingDoblesId = dobles.Id, Puntos = puntos }, ct);
    }

    private async Task<RangoCf> PeorRangoDeParejaAsync(Guid jugador1SinglesId, Guid jugador2SinglesId, CancellationToken ct)
    {
        var d1 = await _jugadoresDobles.ObtenerPorJugadorRankingIdAsync(jugador1SinglesId, ct);
        var d2 = await _jugadoresDobles.ObtenerPorJugadorRankingIdAsync(jugador2SinglesId, ct);
        var rango1 = RangosRanking.De(d1?.PosicionProvisional ?? int.MaxValue);
        var rango2 = RangosRanking.De(d2?.PosicionProvisional ?? int.MaxValue);
        return RangosRanking.IndiceDe(rango1) > RangosRanking.IndiceDe(rango2) ? rango1 : rango2;
    }

    private static bool EsParticipante(JuegoDoblesPendiente juego, Guid jugadorSinglesId) =>
        juego.Jugador1Id == jugadorSinglesId || juego.Jugador2Id == jugadorSinglesId ||
        juego.Rival1Id == jugadorSinglesId || juego.Rival2Id == jugadorSinglesId;

    private async Task<JuegoDoblesPendiente> ObtenerAsync(Guid juegoId, CancellationToken ct) =>
        await _desafios.ObtenerAsync(juegoId, ct) ?? throw new ReglaDeNegocioException("El desafío no existe.");

    private async Task NotificarAParejaAsync(Guid jugador1Id, Guid jugador2Id, string tipo, string mensaje, Guid? entidadId, CancellationToken ct)
    {
        var j1 = await _jugadoresSingles.ObtenerAsync(jugador1Id, ct);
        var j2 = await _jugadoresSingles.ObtenerAsync(jugador2Id, ct);
        if (j1 is not null) await _notificaciones.NotificarAsync(j1.UsuarioId, tipo, mensaje, entidadId, ct);
        if (j2 is not null) await _notificaciones.NotificarAsync(j2.UsuarioId, tipo, mensaje, entidadId, ct);
    }

    private async Task NotificarATodosMenosAsync(
        JuegoDoblesPendiente juego, Guid usuarioIdQueEjecuto, string tipo, string mensaje, Guid? entidadId, CancellationToken ct)
    {
        foreach (var jugadorSinglesId in new[] { juego.Jugador1Id, juego.Jugador2Id, juego.Rival1Id, juego.Rival2Id })
        {
            var jugador = await _jugadoresSingles.ObtenerAsync(jugadorSinglesId, ct);
            if (jugador is not null && jugador.UsuarioId != usuarioIdQueEjecuto)
                await _notificaciones.NotificarAsync(jugador.UsuarioId, tipo, mensaje, entidadId, ct);
        }
    }

    private async Task<DesafioDoblesDto> MapearAsync(JuegoDoblesPendiente juego, CancellationToken ct)
    {
        var j1 = await _jugadoresSingles.ObtenerConNombreAsync(juego.Jugador1Id, ct);
        var j2 = await _jugadoresSingles.ObtenerConNombreAsync(juego.Jugador2Id, ct);
        var r1 = await _jugadoresSingles.ObtenerConNombreAsync(juego.Rival1Id, ct);
        var r2 = await _jugadoresSingles.ObtenerConNombreAsync(juego.Rival2Id, ct);
        return new DesafioDoblesDto
        {
            Id = juego.Id,
            Jugador1Id = juego.Jugador1Id,
            Jugador1Nombre = j1 is null ? string.Empty : $"{j1.Nombre} {j1.Apellido}",
            Jugador2Id = juego.Jugador2Id,
            Jugador2Nombre = j2 is null ? string.Empty : $"{j2.Nombre} {j2.Apellido}",
            Rival1Id = juego.Rival1Id,
            Rival1Nombre = r1 is null ? string.Empty : $"{r1.Nombre} {r1.Apellido}",
            Rival2Id = juego.Rival2Id,
            Rival2Nombre = r2 is null ? string.Empty : $"{r2.Nombre} {r2.Apellido}",
            Estado = juego.Estado.ToString(),
            CreadoEl = juego.CreadoEl,
            AceptadoEl = juego.AceptadoEl,
            GanoParejaA = juego.GanoParejaA,
            PuntosGanadores = juego.PuntosGanadores,
            PuntosPerdedores = juego.PuntosPerdedores,
            FinalizadoEn = juego.FinalizadoEn,
        };
    }
}
