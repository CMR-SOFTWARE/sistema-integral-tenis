using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// La plata (ADR-0009): genera los cargos de clase desde los turnos del mes
/// (perezoso e idempotente), suma productos y ajustes, y registra pagos.
/// </summary>
public interface ICuotaService
{
    /// <summary>
    /// Liquidación del mes: genera los cargos de clase que falten (grupal =
    /// valor hora ÷ ASIGNADOS del turno; individual = valor entero; cancelado
    /// = sin cargo) y devuelve la cuenta por alumno con estado calculado.
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si el tenant no configuró sus precios.</exception>
    Task<LiquidacionMesDto> ObtenerMesAsync(int anio, int mes, CancellationToken ct = default);

    /// <summary>Cargo manual: Producto (encordado, pelotas) o Ajuste (+/- con motivo).</summary>
    Task<CargoResponseDto> AgregarCargoManualAsync(CreateCargoManualDto dto, CancellationToken ct = default);

    /// <summary>Modalidad Mensual: salda TODOS los cargos impagos del mes de un alumno.</summary>
    Task PagarMesAsync(Guid alumnoId, int anio, int mes, MedioPago medio, CancellationToken ct = default);

    /// <summary>Modalidad PorClase (o pago suelto): salda UN cargo.</summary>
    Task PagarCargoAsync(Guid cargoId, MedioPago medio, CancellationToken ct = default);
}
