namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// La relación persona↔tenant con un rol (ADR-0007, modelo-identidad-roles §1 y §5).
/// Hoy modela al profe EMPLEADO (Staff) dentro del tenant de un head pro: se loguea
/// con su propio <see cref="Usuario"/> y ve su agenda/alumnos, sin ser dueño del
/// negocio. El dueño se sigue resolviendo por <see cref="Tenant.OwnerUserId"/>.
/// </summary>
public class MembresiaTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>El usuario (identidad global) que trabaja en este tenant.</summary>
    public Guid UserId { get; set; }

    public RolTenant Rol { get; set; } = RolTenant.Staff;

    /// <summary>
    /// El club (sede) donde trabaja el empleado. Uno por profe (decisión de producto);
    /// opcional. Al cargar un alumno con este profe de cabecera, la ficha hereda esta
    /// sede. El dueño no tiene membresía → no tiene sede fija (trabaja donde asigne).
    /// </summary>
    public Guid? SedeId { get; set; }
    public Sede? Sede { get; set; }

    /// <summary>
    /// Valor hora BASE del empleado (lo que el head pro le paga por hora de clase).
    /// Se setea una vez y se propaga: cada horario que se le asigna lo toma como
    /// default, salvo que se ponga un <see cref="Horario.ValorHoraProfe"/> propio
    /// (ej. clases con menores, que se pagan menos). null = todavía sin definir.
    /// </summary>
    public decimal? ValorHora { get; set; }

    /// <summary>Baja lógica: se desactiva cuando el profe deja de trabajar acá.</summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// El dueño lo habilita a cobrar clases y cuotas (Bloque 6, pedido 2). Sin esto,
    /// el empleado no entra a Finanzas en absoluto. El Director siempre puede cobrar
    /// (no tiene membresía, se resuelve fijo en el service).
    /// </summary>
    public bool PuedeCobrar { get; set; }

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
