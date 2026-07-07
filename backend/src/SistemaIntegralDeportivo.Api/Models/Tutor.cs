namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Tabla separada (no campos en Alumno) porque: 1) un tutor puede tener varios
/// hijos alumnos (hermanos); 2) mantiene Alumno limpio para los mayores
/// (sin columnas NULL de tutor).
/// </summary>
public class Tutor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public required string Nombre { get; set; }
    public required string Apellido { get; set; }
    public required string Dni { get; set; }
    public required string Telefono { get; set; } // a este número van los avisos del menor
    public string? Email { get; set; }
    public RelacionTutor Relacion { get; set; }

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;

    // 1 tutor → N alumnos (hermanos)
    public ICollection<Alumno> Alumnos { get; set; } = new List<Alumno>();
}
