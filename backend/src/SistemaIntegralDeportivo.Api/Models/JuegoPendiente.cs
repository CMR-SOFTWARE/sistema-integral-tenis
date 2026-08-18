namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Un desafío entre dos JugadorRanking (singles). Propuesto → Aceptado →
/// Finalizado. Sin resultado en texto (no "6-4 6-4") — solo quién ganó.
/// Rechazar/Cancelar mientras sigue Propuesto NO deja historia (se borra):
/// todavía no generó ningún efecto irreversible, mismo criterio que
/// PedidoService.CancelarAsync. Una vez Finalizado, nunca se borra.
/// </summary>
public class JuegoPendiente
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid Jugador1Id { get; set; }
    public Guid Jugador2Id { get; set; }

    /// <summary>Par normalizado (menor/mayor Guid) para el índice único: un par de
    /// jugadores solo puede enfrentarse UNA VEZ en este flujo (incluye Finalizado).</summary>
    public Guid JugadorMenorId { get; set; }
    public Guid JugadorMayorId { get; set; }

    public Guid CreadoPorUserId { get; set; }

    public EstadoJuegoPendiente Estado { get; set; } = EstadoJuegoPendiente.Propuesto;

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
    public DateTime? AceptadoEl { get; set; }

    public Guid? GanadorId { get; set; }
    public int? PuntosGanador { get; set; }
    public int? PuntosPerdedor { get; set; }
    public DateTime? FinalizadoEn { get; set; }
}
