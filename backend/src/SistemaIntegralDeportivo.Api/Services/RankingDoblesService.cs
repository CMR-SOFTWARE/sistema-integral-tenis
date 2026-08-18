using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Ranking de DOBLES — espejo de RankingService, pool de puntos y posiciones
/// completamente independiente de singles. Requiere estar inscripto en
/// singles primero (JugadorRankingDobles es 1:1 con JugadorRanking).
/// </summary>
public interface IRankingDoblesService
{
    Task<IReadOnlyList<RankingFilaDoblesDto>> ListarLeaderboardAsync(CancellationToken ct = default);
    Task<RankingFilaDoblesDto> InscribirmeAsync(Guid usuarioId, CancellationToken ct = default);
    Task<MiPerfilDoblesDto> MiPerfilAsync(Guid usuarioId, CancellationToken ct = default);

    /// <summary>Perfil público de dobles de cualquier jugador (JugadorId de singles, no JugadorRankingDoblesId).</summary>
    Task<PerfilPublicoRankingDoblesDto?> PerfilPublicoAsync(Guid jugadorId, CancellationToken ct = default);
    Task ActualizarRankingProvisionalAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RankingFilaDoblesDto>> ListarOficialAsync(ScopeRanking scope, string? scopeValor, CancellationToken ct = default);
}

public class RankingDoblesService : IRankingDoblesService
{
    private const int VigenciaDias = 360;
    private const int LimiteMovimientosPorJugador = 72;

    private readonly IJugadorRankingRepository _jugadoresSingles;
    private readonly IJugadorRankingDoblesRepository _jugadores;
    private readonly IPuntosMovimientoDoblesRepository _movimientos;
    private readonly IRankingSnapshotRepository _snapshots;

    public RankingDoblesService(
        IJugadorRankingRepository jugadoresSingles,
        IJugadorRankingDoblesRepository jugadores,
        IPuntosMovimientoDoblesRepository movimientos,
        IRankingSnapshotRepository snapshots)
    {
        _jugadoresSingles = jugadoresSingles;
        _jugadores = jugadores;
        _movimientos = movimientos;
        _snapshots = snapshots;
    }

    public async Task<IReadOnlyList<RankingFilaDoblesDto>> ListarLeaderboardAsync(CancellationToken ct = default)
    {
        var filas = await _jugadores.ListarActivosOrdenadosAsync(ct);
        var dtos = new List<RankingFilaDoblesDto>();
        foreach (var fila in filas)
        {
            var singles = await _jugadoresSingles.ObtenerAsync(fila.JugadorRankingId, ct);
            dtos.Add(Mapear(fila, singles?.UsuarioId ?? Guid.Empty));
        }
        return dtos;
    }

    public async Task<RankingFilaDoblesDto> InscribirmeAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var singles = await _jugadoresSingles.ObtenerPorUsuarioAsync(usuarioId, ct)
            ?? throw new ReglaDeNegocioException("Tenés que inscribirte al ranking de singles antes de anotarte a dobles.");
        if (await _jugadores.ObtenerPorJugadorRankingIdAsync(singles.Id, ct) is not null)
            throw new ReglaDeNegocioException("Ya estás inscripto en el ranking de dobles.");

        var jugador = new JugadorRankingDobles { JugadorRankingId = singles.Id };
        await _jugadores.AgregarAsync(jugador, ct);
        await _jugadores.GuardarCambiosAsync(ct);

        await ActualizarRankingProvisionalAsync(ct);

        var fila = await _jugadores.ObtenerConNombreAsync(jugador.Id, ct);
        return Mapear(fila!, usuarioId);
    }

    public async Task<MiPerfilDoblesDto> MiPerfilAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var singles = await _jugadoresSingles.ObtenerPorUsuarioAsync(usuarioId, ct);
        if (singles is null) return new MiPerfilDoblesDto { Inscripto = false };

        var jugador = await _jugadores.ObtenerPorJugadorRankingIdAsync(singles.Id, ct);
        if (jugador is null) return new MiPerfilDoblesDto { Inscripto = false };

        return new MiPerfilDoblesDto
        {
            Inscripto = true,
            JugadorRankingDoblesId = jugador.Id,
            Posicion = jugador.PosicionProvisional,
            Puntos = jugador.PuntosProvisionales,
            Rango = jugador.RangoProvisional,
            Cf = jugador.CfProvisional,
            MejorPuestoHistorico = jugador.MejorPuestoHistorico,
        };
    }

    public async Task<PerfilPublicoRankingDoblesDto?> PerfilPublicoAsync(Guid jugadorId, CancellationToken ct = default)
    {
        var dobles = await _jugadores.ObtenerPorJugadorRankingIdAsync(jugadorId, ct);
        if (dobles is null) return null;

        var singles = await _jugadoresSingles.ObtenerConNombreAsync(jugadorId, ct);
        if (singles is null) return null;

        return new PerfilPublicoRankingDoblesDto
        {
            JugadorId = jugadorId,
            Nombre = singles.Nombre,
            Apellido = singles.Apellido,
            Posicion = dobles.PosicionProvisional,
            Puntos = dobles.PuntosProvisionales,
            Rango = dobles.RangoProvisional,
            Cf = dobles.CfProvisional,
            MejorPuestoHistorico = dobles.MejorPuestoHistorico,
        };
    }

    public async Task ActualizarRankingProvisionalAsync(CancellationToken ct = default)
    {
        var desde = DateTime.UtcNow.AddDays(-VigenciaDias);
        var movimientos = await _movimientos.ListarVigentesAsync(desde, ct);
        var puntosPorJugador = movimientos
            .GroupBy(m => m.JugadorRankingDoblesId)
            .ToDictionary(g => g.Key, g => g.Take(LimiteMovimientosPorJugador).Sum(m => m.Puntos));

        var jugadores = await _jugadores.ListarTodosActivosAsync(ct);
        var ordenados = jugadores
            .OrderByDescending(j => puntosPorJugador.GetValueOrDefault(j.Id, 0))
            .ThenBy(j => j.OrdenInscripcion)
            .ToList();

        for (var i = 0; i < ordenados.Count; i++)
        {
            var jugador = ordenados[i];
            var posicion = i + 1;
            var rango = RangosRanking.De(posicion);

            jugador.PuntosProvisionales = puntosPorJugador.GetValueOrDefault(jugador.Id, 0);
            jugador.PosicionProvisional = posicion;
            jugador.RangoProvisional = rango.Rango;
            jugador.CfProvisional = rango.Cf;

            if (jugador.MejorPuestoHistorico is null || posicion < jugador.MejorPuestoHistorico)
            {
                jugador.MejorPuestoHistorico = posicion;
                jugador.FechaMejorPuesto = DateTime.UtcNow;
            }
        }

        await _jugadores.GuardarCambiosAsync(ct);
    }

    public async Task<IReadOnlyList<RankingFilaDoblesDto>> ListarOficialAsync(
        ScopeRanking scope, string? scopeValor, CancellationToken ct = default)
    {
        var snapshot = await _snapshots.ObtenerUltimoOficialAsync(ModalidadRanking.Dobles, scope, scopeValor, ct);
        var dtos = new List<RankingFilaDoblesDto>();
        foreach (var fila in snapshot)
        {
            var dobles = await _jugadores.ObtenerPorJugadorRankingIdAsync(fila.JugadorId, ct);
            var singles = await _jugadoresSingles.ObtenerConNombreAsync(fila.JugadorId, ct);
            if (dobles is null || singles is null) continue;
            dtos.Add(new RankingFilaDoblesDto
            {
                JugadorRankingDoblesId = dobles.Id,
                JugadorId = fila.JugadorId,
                UsuarioId = singles.Jugador.UsuarioId,
                Nombre = singles.Nombre,
                Apellido = singles.Apellido,
                Posicion = fila.Posicion,
                Puntos = fila.Puntos,
                Rango = fila.Rango,
                Cf = fila.Cf,
            });
        }
        return dtos;
    }

    private static RankingFilaDoblesDto Mapear(FilaRankingDobles f, Guid usuarioId) => new()
    {
        JugadorRankingDoblesId = f.Jugador.Id,
        JugadorId = f.JugadorRankingId,
        UsuarioId = usuarioId,
        Nombre = f.Nombre,
        Apellido = f.Apellido,
        Posicion = f.Jugador.PosicionProvisional ?? 0,
        Puntos = f.Jugador.PuntosProvisionales,
        Rango = f.Jugador.RangoProvisional ?? RangosRanking.De(0).Rango,
        Cf = f.Jugador.CfProvisional ?? RangosRanking.De(0).Cf,
    };
}
