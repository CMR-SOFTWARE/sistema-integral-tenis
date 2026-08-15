namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Una fila del ranking OFICIAL, congelada al momento del cierre (día 1 o 16).
/// A diferencia de JugadorRanking.PosicionProvisional (que se pisa en cada
/// recálculo), esto es historia: una vez creado, NUNCA se edita ni se borra,
/// aunque se sigan jugando partidos después. JugadorId es siempre el id de
/// SINGLES (JugadorRanking) — también para snapshots de dobles, para tener
/// una sola identidad resoluble a nombre/geo sin duplicar el perfil.
/// </summary>
public class RankingSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ModalidadRanking Modalidad { get; set; }
    public Guid JugadorId { get; set; }

    public int Posicion { get; set; }
    public int Puntos { get; set; }
    public string Rango { get; set; } = string.Empty;
    public int Cf { get; set; }

    /// <summary>Global, o local a un grupo geográfico (Ciudad/Provincia/Pais) — la
    /// Posicion de un snapshot no-Global es la posición DENTRO de ese grupo, no la global.</summary>
    public ScopeRanking Scope { get; set; }
    public string? ScopeValor { get; set; }

    public DateTime FechaCorte { get; set; }
}
