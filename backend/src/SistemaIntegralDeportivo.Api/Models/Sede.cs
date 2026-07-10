namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Club/lugar donde trabaja el profe (parte de SU negocio; no confundir con
/// los clubes-tenant de la Fase 2). Hoy: 2 sedes, una con 2 canchas.
/// </summary>
public class Sede
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public required string Nombre { get; set; } // "Club Atlético Norte"
    public bool Activo { get; set; } = true;
    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;

    public ICollection<Cancha> Canchas { get; set; } = new List<Cancha>();
}
