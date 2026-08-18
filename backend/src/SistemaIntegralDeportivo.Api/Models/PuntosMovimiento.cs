namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Ledger de puntos de ranking: un movimiento por resultado (ganador y
/// perdedor, uno cada uno). Sin rival, sin resultado — el ranking SOLO suma
/// esto. Nunca se edita ni se borra (los partidos finalizados son historia).
/// </summary>
public class PuntosMovimiento
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid JugadorRankingId { get; set; }

    public int Puntos { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
