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

    /// <summary>Baja lógica: se desactiva cuando el profe deja de trabajar acá.</summary>
    public bool Activo { get; set; } = true;

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
