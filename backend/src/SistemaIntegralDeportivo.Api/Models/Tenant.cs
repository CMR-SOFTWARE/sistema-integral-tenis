namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// El "dueño" de todos los datos. Cada profesor/club es un tenant, y todas
/// las demás entidades cuelgan de él. Equivale a tener una base por cliente,
/// pero implementado como columna TenantId + filtrado en cada query.
/// </summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Subdominio { get; set; } // único: juanperez.midominio.com
    public required string Nombre { get; set; }
    public TipoTenant Tipo { get; set; } = TipoTenant.Profesor;
    public bool Activo { get; set; } = true;
    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;

    // ── Navegación (las FK apuntan hacia acá) ──
    public ICollection<Alumno> Alumnos { get; set; } = new List<Alumno>();
    public ICollection<Tutor> Tutores { get; set; } = new List<Tutor>();
    public ICollection<Grupo> Grupos { get; set; } = new List<Grupo>();
}
