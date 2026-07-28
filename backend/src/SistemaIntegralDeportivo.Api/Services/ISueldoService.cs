using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Liquidación de sueldos del profe empleado: lo administra el DUEÑO.</summary>
public interface ISueldoService
{
    /// <summary>La pantalla Sueldos de un mes: calculado vs pagado por empleado + totales.</summary>
    Task<LiquidacionSueldosDto> ObtenerMesAsync(int anio, int mes, CancellationToken ct = default);

    /// <summary>La liquidación del PROPIO empleado logueado (para su panel).</summary>
    Task<EmpleadoSueldoDto> ObtenerMioAsync(int anio, int mes, CancellationToken ct = default);

    /// <summary>Registra el pago del sueldo de un empleado por un mes (monto ajustable).</summary>
    Task PagarAsync(Guid userId, int anio, int mes, decimal monto, MedioPago medio, CancellationToken ct = default);

    /// <summary>Borra el pago registrado de un empleado en un mes (por si se registró mal).</summary>
    Task RevertirPagoAsync(Guid userId, int anio, int mes, CancellationToken ct = default);

    /// <summary>Egreso de sueldos pagado por mes, últimos N meses (panel/reporte).</summary>
    Task<IReadOnlyList<SueldoMesDto>> ObtenerReporteAsync(int meses, CancellationToken ct = default);
}
