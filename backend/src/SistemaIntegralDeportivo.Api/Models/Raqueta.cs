namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Una raqueta del alumno (M3). Un alumno puede tener varias. La administra
/// el alumno desde su perfil (dato deportivo suyo, no del profe). Guarda la
/// marca de la raqueta y cómo la tiene encordada (tensión + marca del
/// encordado) — lo que el profe o el encordador necesitan saber.
/// </summary>
public class Raqueta
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    /// <summary>Marca/modelo de la raqueta, ej "Wilson Blade 98".</summary>
    public required string Marca { get; set; }

    /// <summary>Tensión del encordado tal cual la dice el alumno, ej "24 kg" o "50 lbs".</summary>
    public string? Tension { get; set; }

    /// <summary>Marca del encordado, ej "Luxilon ALU Power".</summary>
    public string? MarcaEncordado { get; set; }

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
