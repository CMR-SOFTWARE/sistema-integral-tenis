namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Un desafío de dobles entre dos parejas AD-HOC (no hay entidad "Pareja" —
/// se arman partido a partido). Jugador1/Jugador2 = la pareja que propone;
/// Rival1/Rival2 = la pareja desafiada. Las 4 FK apuntan a JugadorRanking
/// (el id de SINGLES): al finalizar hay que resolver el 1:1 con
/// JugadorRankingDobles para saber a qué fila de puntos sumarle.
///
/// A diferencia de singles, acá SÍ se permite revancha: el bloqueo de
/// "ya tenés un desafío pendiente con esta pareja" solo mira Propuesto/Aceptado
/// (nunca Finalizado) — normalizar 4 jugadores en un índice único de base es
/// enrevesado, así que se valida en DesafioDoblesService, no con un constraint.
/// </summary>
public class JuegoDoblesPendiente
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid Jugador1Id { get; set; }
    public Guid Jugador2Id { get; set; }
    public Guid Rival1Id { get; set; }
    public Guid Rival2Id { get; set; }

    public Guid CreadoPorUserId { get; set; }

    public EstadoJuegoPendiente Estado { get; set; } = EstadoJuegoPendiente.Propuesto;

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
    public DateTime? AceptadoEl { get; set; }

    /// <summary>true = ganó la pareja Jugador1/Jugador2 ("pareja A"); false = ganó Rival1/Rival2.</summary>
    public bool? GanoParejaA { get; set; }
    public int? PuntosGanadores { get; set; }
    public int? PuntosPerdedores { get; set; }
    public DateTime? FinalizadoEn { get; set; }
}
