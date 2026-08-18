namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Fórmula de puntos de un resultado — pieza intercambiable, mismo criterio
/// que IPoliticaDeCuota/IPoliticaDeSueldo. Una sola implementación hoy
/// (cf_consolacion_v1, marcada "provisoria" en el contrato de ranking).
/// </summary>
public interface IPoliticaDePuntosRanking
{
    (int PuntosGanador, int PuntosPerdedor) Calcular(RangoCf rangoGanador, RangoCf rangoPerdedor);
}

/// <summary>
/// cf_consolacion_v1: ambos suman, el perdedor nunca suma 0.
/// puntosGanador = CF del rango del perdedor.
/// puntosPerdedor = CF del rango inmediatamente PEOR entre los dos (si ya es
/// el último de la tabla, usa el CF de ese mismo).
/// </summary>
public class PuntosCfConsolacionV1 : IPoliticaDePuntosRanking
{
    public (int PuntosGanador, int PuntosPerdedor) Calcular(RangoCf rangoGanador, RangoCf rangoPerdedor)
    {
        var peorRango = RangosRanking.IndiceDe(rangoGanador) > RangosRanking.IndiceDe(rangoPerdedor)
            ? rangoGanador
            : rangoPerdedor;
        var inferior = RangosRanking.Inferior(peorRango);
        return (rangoPerdedor.Cf, inferior.Cf);
    }
}
