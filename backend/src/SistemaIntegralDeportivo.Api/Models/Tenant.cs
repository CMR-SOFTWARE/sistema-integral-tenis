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

    /// <summary>Default PendientePago: nada se activa sin pagar (o sin seed).</summary>
    public EstadoTenant Estado { get; set; } = EstadoTenant.PendientePago;

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;

    // ── Auth: el usuario dueño del negocio (su rol Profesor en ESTE tenant).
    //    Membresía mínima del ADR-0007; Staff y demás roles, en fases futuras. ──
    public Guid? OwnerUserId { get; set; }

    // ── Precios (config del profe; null = todavía no configurado) ──
    // La fórmula (modelo-precios.md): grupal se divide entre los ASIGNADOS
    // del turno; individual la paga entera el alumno.
    public decimal? ValorHoraGrupal { get; set; }
    public decimal? ValorClaseIndividual { get; set; }

    // ── Navegación (las FK apuntan hacia acá) ──
    public ICollection<Alumno> Alumnos { get; set; } = new List<Alumno>();
    public ICollection<Tutor> Tutores { get; set; } = new List<Tutor>();
    public ICollection<Grupo> Grupos { get; set; } = new List<Grupo>();
}
