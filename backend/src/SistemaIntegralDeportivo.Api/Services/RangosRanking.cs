namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Un tramo de la tabla de rangos (posición inclusive) → letra + Coeficiente de Fuerza.</summary>
public record RangoCf(string Rango, int Desde, int Hasta, int Cf);

/// <summary>
/// Tabla de rangos/CF de R.U.T.A. (dato semilla del contrato de ranking). El rango
/// y el CF de un jugador salen SIEMPRE de su posición, nunca de sus puntos
/// directamente. Sin posición o más allá del último tramo → O / 50.
/// </summary>
public static class RangosRanking
{
    public static readonly IReadOnlyList<RangoCf> Tabla =
    [
        new("A", 1, 10, 200),
        new("B", 11, 20, 190),
        new("C", 21, 50, 180),
        new("D", 51, 100, 170),
        new("E", 101, 200, 160),
        new("F", 201, 500, 150),
        new("G", 501, 1000, 140),
        new("H", 1001, 2000, 130),
        new("I", 2001, 5000, 120),
        new("J", 5001, 10000, 110),
        new("K", 10001, 20000, 100),
        new("L", 20001, 50000, 90),
        new("M", 50001, 100000, 80),
        new("N", 100001, 200000, 70),
        new("Ñ", 200001, 500000, 60),
        new("O", 500001, 1000000, 50),
    ];

    private static readonly RangoCf Ultimo = Tabla[^1];

    /// <summary>El tramo que corresponde a una posición (1-based). Fuera de tabla → el último (O/50).</summary>
    public static RangoCf De(int posicion) =>
        Tabla.FirstOrDefault(r => posicion >= r.Desde && posicion <= r.Hasta) ?? Ultimo;

    /// <summary>Posición del tramo en la tabla (0 = A, el mejor). -1 si no existe.</summary>
    public static int IndiceDe(RangoCf rango) => Tabla.ToList().FindIndex(r => r.Rango == rango.Rango);

    /// <summary>El tramo inmediatamente PEOR (índice siguiente) que el dado. Si ya es el último, es el mismo.</summary>
    public static RangoCf Inferior(RangoCf rango)
    {
        var i = IndiceDe(rango);
        return i < 0 || i == Tabla.Count - 1 ? Ultimo : Tabla[i + 1];
    }
}
