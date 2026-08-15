using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Una fila de entrada para armar el snapshot — ya con posición/puntos/rango/CF
/// provisionales vigentes al momento del corte.</summary>
public record FilaParaSnapshot(
    Guid JugadorId, int Posicion, int Puntos, string Rango, int Cf, int OrdenInscripcion,
    string? Ciudad, string? Provincia, string? Pais);

/// <summary>
/// Cierre oficial del ranking (día 1 y 16, o forzado a mano por un admin):
/// congela un snapshot GLOBAL + uno por cada grupo geográfico (ciudad,
/// provincia, país), sobre singles y dobles. Una vez creado, el snapshot es
/// historia — nunca se edita ni se borra, aunque se sigan jugando partidos.
/// </summary>
public interface IRankingCierreOficialService
{
    /// <summary>Devuelve la cantidad de filas de snapshot creadas.</summary>
    Task<int> CerrarOficialAsync(CancellationToken ct = default);
}

public class RankingCierreOficialService : IRankingCierreOficialService
{
    private readonly IRankingSnapshotRepository _snapshots;
    private readonly IJugadorRankingRepository _singles;
    private readonly IJugadorRankingDoblesRepository _dobles;

    public RankingCierreOficialService(
        IRankingSnapshotRepository snapshots, IJugadorRankingRepository singles, IJugadorRankingDoblesRepository dobles)
    {
        _snapshots = snapshots;
        _singles = singles;
        _dobles = dobles;
    }

    public async Task<int> CerrarOficialAsync(CancellationToken ct = default)
    {
        if (await _snapshots.YaCerroHoyAsync(ct))
            throw new ReglaDeNegocioException("El ranking ya se cerró oficialmente hoy.");

        var fechaCorte = DateTime.UtcNow;

        var singlesActivos = await _singles.ListarTodosActivosAsync(ct);
        var filasSingles = singlesActivos.Select(j => new FilaParaSnapshot(
            j.Id, j.PosicionProvisional ?? 0, j.PuntosProvisionales, j.RangoProvisional ?? RangosRanking.De(0).Rango,
            j.CfProvisional ?? RangosRanking.De(0).Cf, j.OrdenInscripcion, j.CiudadResidencia, j.Provincia, j.Pais)).ToList();

        var doblesActivos = await _dobles.ListarTodosActivosAsync(ct);
        var filasDobles = new List<FilaParaSnapshot>();
        foreach (var d in doblesActivos)
        {
            var singles = await _singles.ObtenerAsync(d.JugadorRankingId, ct);
            filasDobles.Add(new FilaParaSnapshot(
                d.JugadorRankingId, d.PosicionProvisional ?? 0, d.PuntosProvisionales,
                d.RangoProvisional ?? RangosRanking.De(0).Rango, d.CfProvisional ?? RangosRanking.De(0).Cf,
                d.OrdenInscripcion, singles?.CiudadResidencia, singles?.Provincia, singles?.Pais));
        }

        var snapshots = new List<RankingSnapshot>();
        snapshots.AddRange(ArmarSnapshots(filasSingles, ModalidadRanking.Singles, fechaCorte));
        snapshots.AddRange(ArmarSnapshots(filasDobles, ModalidadRanking.Dobles, fechaCorte));

        await _snapshots.AgregarRangoAsync(snapshots, ct);
        await _snapshots.GuardarCambiosAsync(ct);
        return snapshots.Count;
    }

    /// <summary>
    /// Pura, sin I/O — fácil de testear sin mocks. Arma el snapshot Global
    /// (usa la posición YA calculada por el recálculo provisional, no la
    /// recalcula) + uno por cada grupo geográfico, reordenando SOLO dentro
    /// del grupo con el mismo criterio que el recálculo global (Puntos DESC,
    /// OrdenInscripcion ASC — nunca alfabético ni random). Un jugador sin esa
    /// geo cargada simplemente no entra en ese scope.
    /// </summary>
    public static List<RankingSnapshot> ArmarSnapshots(
        IReadOnlyList<FilaParaSnapshot> filas, ModalidadRanking modalidad, DateTime fechaCorte)
    {
        var resultado = new List<RankingSnapshot>();

        foreach (var f in filas)
            resultado.Add(new RankingSnapshot
            {
                Modalidad = modalidad,
                JugadorId = f.JugadorId,
                Posicion = f.Posicion,
                Puntos = f.Puntos,
                Rango = f.Rango,
                Cf = f.Cf,
                Scope = ScopeRanking.Global,
                ScopeValor = null,
                FechaCorte = fechaCorte,
            });

        AgregarPorGeo(resultado, filas, modalidad, ScopeRanking.Ciudad, f => f.Ciudad, fechaCorte);
        AgregarPorGeo(resultado, filas, modalidad, ScopeRanking.Provincia, f => f.Provincia, fechaCorte);
        AgregarPorGeo(resultado, filas, modalidad, ScopeRanking.Pais, f => f.Pais, fechaCorte);

        return resultado;
    }

    private static void AgregarPorGeo(
        List<RankingSnapshot> resultado, IReadOnlyList<FilaParaSnapshot> filas, ModalidadRanking modalidad,
        ScopeRanking scope, Func<FilaParaSnapshot, string?> geo, DateTime fechaCorte)
    {
        var grupos = filas.Where(f => !string.IsNullOrWhiteSpace(geo(f))).GroupBy(geo);
        foreach (var grupo in grupos)
        {
            var ordenado = grupo.OrderByDescending(f => f.Puntos).ThenBy(f => f.OrdenInscripcion).ToList();
            for (var i = 0; i < ordenado.Count; i++)
            {
                var f = ordenado[i];
                resultado.Add(new RankingSnapshot
                {
                    Modalidad = modalidad,
                    JugadorId = f.JugadorId,
                    Posicion = i + 1,
                    Puntos = f.Puntos,
                    Rango = f.Rango,
                    Cf = f.Cf,
                    Scope = scope,
                    ScopeValor = grupo.Key,
                    FechaCorte = fechaCorte,
                });
            }
        }
    }
}
