namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Un sueldo PAGADO a un profe empleado por un mes (el egreso registrado).
/// Espeja a <see cref="Cargo"/> pero del lado del gasto: el monto del mes se
/// calcula en vivo desde la agenda (horas dadas × valor hora, ver
/// IPoliticaDeSueldo); esta fila nace recién cuando el head pro PAGA y congela
/// ese monto. Uno por (empleado, mes). Lo pagado es historia intocable.
/// </summary>
public class PagoEmpleado
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>El usuario (identidad global) del profe empleado que cobra.</summary>
    public Guid UserId { get; set; }

    // Período liquidado (no una fecha suelta: el sueldo es mensual).
    public int Anio { get; set; }
    public int Mes { get; set; }

    /// <summary>Snapshot de lo pagado (viene del cálculo, ajustable al pagar).</summary>
    public decimal Monto { get; set; }

    public MedioPago MedioPago { get; set; }

    /// <summary>Cuándo se registró el pago (la fecha la pone el server, nunca el cliente).</summary>
    public DateTime PagadoEl { get; set; } = DateTime.UtcNow;

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
