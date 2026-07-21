namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Una nota del profe SOBRE un alumno (seguimiento: mejoras, fallas de la clase).
/// Es privada por defecto; si <see cref="Compartida"/> es true, el alumno la ve en
/// su portal como devolución. Distinta de <see cref="Aviso"/>, que es general para
/// todos. Vive en el tenant del profe y se escribe desde la ficha del alumno.
/// </summary>
public class NotaAlumno
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    public required string Texto { get; set; }

    /// <summary>Si es true, el alumno la ve en su portal; si no, es privada del profe.</summary>
    public bool Compartida { get; set; }

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
