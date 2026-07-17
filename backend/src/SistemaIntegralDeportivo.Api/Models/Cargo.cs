namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Línea de la cuenta corriente del alumno (ADR-0009): una clase tomada,
/// un producto/servicio (encordado, pelotas) o un ajuste manual (+/-).
/// El monto es SNAPSHOT: cambios de precios posteriores no lo tocan.
/// </summary>
public class Cargo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    public TipoCargo Tipo { get; set; }

    /// <summary>"Clase grupal — Intermedios (4)", "Encordado Wilson", "Descuento hermanos".</summary>
    public required string Concepto { get; set; }

    /// <summary>Positivo debe, negativo descuenta (solo Ajuste puede ser negativo).</summary>
    public decimal Monto { get; set; }

    /// <summary>Fecha del hecho (la clase, la venta). Define a qué período pertenece.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Solo cargos de Clase: el turno que lo originó (idempotencia por turno+alumno).</summary>
    public Guid? TurnoId { get; set; }
    public Turno? Turno { get; set; }

    // ── Pago (null = impago) ──
    // Estado intermedio: el alumno AVISÓ desde el portal que transfirió, pero
    // el profe todavía no lo confirmó. La plata sigue IMPAGA (PagadoEl null)
    // hasta que el profe confirme: la verdad de la plata la pone el profe, no
    // el cliente. Al confirmar → PagadoEl; al rechazar → PagoInformadoEl null.
    public DateTime? PagoInformadoEl { get; set; }
    public DateTime? PagadoEl { get; set; }
    public MedioPago? MedioPago { get; set; }

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
