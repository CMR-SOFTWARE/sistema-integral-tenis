namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Una raqueta del alumno (M3). Un alumno puede tener varias. Es el objeto FÍSICO:
/// qué raqueta es y de quién. Cómo está encordada vive en <see cref="Encordado"/>,
/// porque se reencorda cada tanto y al profe le importa cuándo fue la última vez.
///
/// La cargan tanto el alumno (desde su perfil) como el profe (desde la ficha): el
/// profe suele ser el que encorda.
/// </summary>
public class Raqueta
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    /// <summary>
    /// Cómo la llama su dueño: "Raqueta 1", "la de entrenar". Opcional — sirve cuando
    /// alguien tiene dos iguales y la marca no alcanza para distinguirlas. Sin nombre,
    /// la pantalla la muestra por marca y modelo, como hasta ahora.
    /// </summary>
    public string? Nombre { get; set; }

    /// <summary>Marca, ej "Wilson".</summary>
    public required string Marca { get; set; }

    /// <summary>Modelo, ej "Blade 98 v8". Opcional: muchos solo saben la marca.</summary>
    public string? Modelo { get; set; }

    /// <summary>Los encordados que tuvo, del más viejo al más nuevo.</summary>
    public ICollection<Encordado> Encordados { get; set; } = [];

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
