namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Una noticia del club: el director publica algo (una nota, un aviso, una novedad) y lo
/// ven TODOS los alumnos de ese tenant. Puede tener una fecha de vencimiento tras la cual
/// se oculta sola (null = sin vencimiento). Distinta de <see cref="NotaAlumno"/>, que es
/// privada y por alumno.
///
/// Se llamaba <c>Aviso</c> hasta el 13/08/2026. Se renombró porque el profe las llama
/// noticias, y porque "aviso" ya significa otra cosa en este dominio: el alumno que
/// cancela una clase está dando un aviso (<see cref="TurnoParticipante.CanceloEl"/>).
/// </summary>
public class Noticia
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public required string Titulo { get; set; }

    public required string Mensaje { get; set; }

    /// <summary>
    /// Destacada: sube al Inicio del portal, bien visible. La que no lo es solo aparece
    /// en la sección Noticias — que es la diferencia entre "no hay clases mañana" y
    /// "salió el fixture del torneo".
    /// </summary>
    public bool Importante { get; set; }

    /// <summary>Fecha (inclusive) hasta la que se muestra; null = sin vencimiento.</summary>
    public DateOnly? VenceEl { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
