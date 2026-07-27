using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Costura VOLÁTIL de "cómo nace el sueldo del mes" (patrón Strategy, igual que
/// <see cref="IPoliticaDeCuota"/>). Hoy: valor hora × horas dadas
/// (<see cref="SueldoPorHora"/>). Mañana se puede cambiar por "% de lo recaudado"
/// o "fijo mensual" registrando otra implementación, sin tocar SueldoService ni
/// el ledger de pagos.
/// </summary>
public interface IPoliticaDeSueldo
{
    /// <summary>Lo que ganó cada empleado en el mes (el pago todavía no ocurrió).</summary>
    Task<IReadOnlyList<SueldoCalculadoDto>> CalcularDelMesAsync(
        int anio, int mes, CancellationToken ct = default);
}
