using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Contrato de datos de los sueldos PAGADOS a empleados (el egreso registrado).</summary>
public interface IPagoEmpleadoRepository
{
    /// <summary>Pagos de sueldo del mes (para cruzar con lo calculado).</summary>
    Task<IReadOnlyList<PagoEmpleado>> ListarDelMesAsync(int anio, int mes, CancellationToken ct = default);

    /// <summary>El pago de un empleado en un mes puntual (idempotencia: uno por mes).</summary>
    Task<PagoEmpleado?> ObtenerDelMesAsync(Guid userId, int anio, int mes, CancellationToken ct = default);

    /// <summary>Total pagado por mes en un rango (reporte de egresos).</summary>
    Task<Dictionary<(int Anio, int Mes), decimal>> SumarPorMesAsync(
        int desdeAnio, int desdeMes, int hastaAnio, int hastaMes, CancellationToken ct = default);

    Task AgregarAsync(PagoEmpleado pago, CancellationToken ct = default);
    void Eliminar(PagoEmpleado pago);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
