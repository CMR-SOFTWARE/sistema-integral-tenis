namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>Ledger de puntos de ranking de DOBLES — espejo de PuntosMovimiento, pool independiente.</summary>
public class PuntosMovimientoDobles
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid JugadorRankingDoblesId { get; set; }

    public int Puntos { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
