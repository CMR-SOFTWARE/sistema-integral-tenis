namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Grupo FIJO ("los pibes de los martes 18hs"), que se repite. Los grupos
/// armados clase por clase NO se modelan acá (eso es propiedad del turno,
/// en la fase de turnos). No sobre-modelar.
/// </summary>
public class Grupo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public required string Nombre { get; set; }   // "Intermedios martes"
    public CategoriaAlumno? Categoria { get; set; } // sugerida, para armar grupos parejos
    public int? CupoMaximo { get; set; }            // null = sin límite
    public bool Activo { get; set; } = true;

    /// <summary>El profe a cargo del grupo (dueño o staff); null = sin asignar. Sin nav, como UserId.</summary>
    public Guid? ProfesorUserId { get; set; }

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;

    public ICollection<AlumnoGrupo> Alumnos { get; set; } = new List<AlumnoGrupo>();
}
