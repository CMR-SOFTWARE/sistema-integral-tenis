namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Inscripción de un JugadorRanking (singles) al ranking de DOBLES — 1:1 con
/// JugadorRanking, no con Usuario directo: hay que estar en singles primero.
/// Puntos y posición son completamente independientes de los de singles.
/// </summary>
public class JugadorRankingDobles
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid JugadorRankingId { get; set; }

    public int PuntosProvisionales { get; set; }
    public int? PosicionProvisional { get; set; }
    public string? RangoProvisional { get; set; }
    public int? CfProvisional { get; set; }

    /// <summary>Desempate propio de dobles: nunca alfabético ni random. Secuencia propia.</summary>
    public int OrdenInscripcion { get; set; }

    public int? MejorPuestoHistorico { get; set; }
    public DateTime? FechaMejorPuesto { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime InscriptoEl { get; set; } = DateTime.UtcNow;
}
