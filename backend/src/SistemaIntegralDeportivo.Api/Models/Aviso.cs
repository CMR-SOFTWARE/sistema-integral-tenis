namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Un aviso general del profe para TODOS sus alumnos (tablón del club). Apunta a
/// un TENANT: los alumnos de ese club lo ven en el Inicio del portal. Puede tener
/// una fecha de vencimiento tras la cual se oculta solo (Null = sin vencimiento).
/// Distinto de <see cref="NotaAlumno"/>, que es privado y por alumno.
/// </summary>
public class Aviso
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public required string Titulo { get; set; }

    public required string Mensaje { get; set; }

    /// <summary>Fecha (inclusive) hasta la que se muestra; null = sin vencimiento.</summary>
    public DateOnly? VenceEl { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
