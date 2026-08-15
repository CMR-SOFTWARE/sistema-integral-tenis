using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Ranking R.U.T.A. — leaderboard, inscripción y el recálculo global
/// (provisional, ADR pendiente §4: se reordena TODA la tabla tras cada
/// finalización, no un ajuste incremental de dos filas). Cross-tenant.
/// </summary>
public interface IRankingService
{
    Task<IReadOnlyList<RankingFilaDto>> ListarLeaderboardAsync(CancellationToken ct = default);
    Task<RankingFilaDto> InscribirmeAsync(Guid usuarioId, InscribirmeRankingDto dto, CancellationToken ct = default);
    Task<MiPerfilRankingDto> MiPerfilAsync(Guid usuarioId, CancellationToken ct = default);

    /// <summary>Perfil público de cualquier jugador (para la ficha que se abre al tocar su fila).</summary>
    Task<PerfilPublicoRankingDto?> PerfilPublicoAsync(Guid jugadorId, CancellationToken ct = default);

    /// <summary>El último cierre oficial vigente para ese scope (vacío si nunca se cerró).</summary>
    Task<IReadOnlyList<RankingFilaDto>> ListarOficialAsync(ScopeRanking scope, string? scopeValor, CancellationToken ct = default);

    /// <summary>
    /// Recalcula puntos vigentes (ventana 360 días, tope 72 movimientos más
    /// recientes por jugador), reordena TODOS los activos (Puntos DESC,
    /// OrdenInscripcion ASC) y persiste posición/rango/CF de cada uno.
    /// </summary>
    Task ActualizarRankingProvisionalAsync(CancellationToken ct = default);
}

public class RankingService : IRankingService
{
    private const int VigenciaDias = 360; // 24 quincenas
    private const int LimiteMovimientosPorJugador = 72;

    private readonly IJugadorRankingRepository _jugadores;
    private readonly IPuntosMovimientoRepository _movimientos;
    private readonly IRankingSnapshotRepository _snapshots;

    public RankingService(
        IJugadorRankingRepository jugadores, IPuntosMovimientoRepository movimientos, IRankingSnapshotRepository snapshots)
    {
        _jugadores = jugadores;
        _movimientos = movimientos;
        _snapshots = snapshots;
    }

    public async Task<IReadOnlyList<RankingFilaDto>> ListarLeaderboardAsync(CancellationToken ct = default)
    {
        var filas = await _jugadores.ListarActivosOrdenadosAsync(ct);
        return filas.Select(Mapear).ToList();
    }

    public async Task<RankingFilaDto> InscribirmeAsync(
        Guid usuarioId, InscribirmeRankingDto dto, CancellationToken ct = default)
    {
        if (await _jugadores.ObtenerPorUsuarioAsync(usuarioId, ct) is not null)
            throw new ReglaDeNegocioException("Ya estás inscripto en el ranking.");

        var jugador = new JugadorRanking
        {
            UsuarioId = usuarioId,
            Sexo = dto.Sexo,
            CiudadResidencia = dto.CiudadResidencia,
            Provincia = dto.Provincia,
            Pais = dto.Pais,
            Bio = dto.Bio,
        };
        await _jugadores.AgregarAsync(jugador, ct);
        await _jugadores.GuardarCambiosAsync(ct);

        // Nace al final de la tabla (0 puntos, último en orden de inscripción):
        // el recálculo le fija posición/rango/CF igual que a cualquier otro.
        await ActualizarRankingProvisionalAsync(ct);

        var fila = await _jugadores.ObtenerConNombreAsync(jugador.Id, ct);
        return Mapear(fila!);
    }

    public async Task<MiPerfilRankingDto> MiPerfilAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var jugador = await _jugadores.ObtenerPorUsuarioAsync(usuarioId, ct);
        if (jugador is null) return new MiPerfilRankingDto { Inscripto = false };

        return new MiPerfilRankingDto
        {
            Inscripto = true,
            JugadorId = jugador.Id,
            Posicion = jugador.PosicionProvisional,
            Puntos = jugador.PuntosProvisionales,
            Rango = jugador.RangoProvisional,
            Cf = jugador.CfProvisional,
            CiudadResidencia = jugador.CiudadResidencia,
            Provincia = jugador.Provincia,
            Pais = jugador.Pais,
            Bio = jugador.Bio,
            MejorPuestoHistorico = jugador.MejorPuestoHistorico,
        };
    }

    public async Task<PerfilPublicoRankingDto?> PerfilPublicoAsync(Guid jugadorId, CancellationToken ct = default)
    {
        var fila = await _jugadores.ObtenerConNombreAsync(jugadorId, ct);
        if (fila is null) return null;

        var jugador = fila.Jugador;
        return new PerfilPublicoRankingDto
        {
            JugadorId = jugador.Id,
            Nombre = fila.Nombre,
            Apellido = fila.Apellido,
            Posicion = jugador.PosicionProvisional,
            Puntos = jugador.PuntosProvisionales,
            Rango = jugador.RangoProvisional,
            Cf = jugador.CfProvisional,
            CiudadResidencia = jugador.CiudadResidencia,
            Provincia = jugador.Provincia,
            Bio = jugador.Bio,
            MejorPuestoHistorico = jugador.MejorPuestoHistorico,
        };
    }

    public async Task ActualizarRankingProvisionalAsync(CancellationToken ct = default)
    {
        var desde = DateTime.UtcNow.AddDays(-VigenciaDias);
        // Se trae la ventana entera y se agrupa/topea en MEMORIA a propósito: un
        // "top-N por grupo" (72 más recientes por jugador) no traduce bien a SQL
        // vía EF, y el dataset ya viene acotado por la ventana de 360 días — a la
        // escala real de esta plataforma esto no es un problema de performance.
        var movimientos = await _movimientos.ListarVigentesAsync(desde, ct);
        var puntosPorJugador = movimientos
            .GroupBy(m => m.JugadorRankingId)
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

    public async Task<IReadOnlyList<RankingFilaDto>> ListarOficialAsync(
        ScopeRanking scope, string? scopeValor, CancellationToken ct = default)
    {
        var snapshot = await _snapshots.ObtenerUltimoOficialAsync(ModalidadRanking.Singles, scope, scopeValor, ct);
        var dtos = new List<RankingFilaDto>();
        foreach (var fila in snapshot)
        {
            var jugador = await _jugadores.ObtenerConNombreAsync(fila.JugadorId, ct);
            if (jugador is null) continue;
            dtos.Add(new RankingFilaDto
            {
                JugadorId = fila.JugadorId,
                UsuarioId = jugador.Jugador.UsuarioId,
                Nombre = jugador.Nombre,
                Apellido = jugador.Apellido,
                Posicion = fila.Posicion,
                Puntos = fila.Puntos,
                Rango = fila.Rango,
                Cf = fila.Cf,
            });
        }
        return dtos;
    }

    private static RankingFilaDto Mapear(FilaRanking f) => new()
    {
        JugadorId = f.Jugador.Id,
        UsuarioId = f.Jugador.UsuarioId,
        Nombre = f.Nombre,
        Apellido = f.Apellido,
        Posicion = f.Jugador.PosicionProvisional ?? 0,
        Puntos = f.Jugador.PuntosProvisionales,
        Rango = f.Jugador.RangoProvisional ?? RangosRanking.De(0).Rango,
        Cf = f.Jugador.CfProvisional ?? RangosRanking.De(0).Cf,
    };
}
