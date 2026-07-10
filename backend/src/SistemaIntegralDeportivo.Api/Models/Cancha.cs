namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Cancha dentro de una sede. La regla de solapamiento de horarios es POR
/// CANCHA (el profe tiene staff: puede haber turnos simultáneos en canchas
/// distintas, nunca en la misma).
/// </summary>
public class Cancha
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SedeId { get; set; }
    public Sede Sede { get; set; } = null!;

    public required string Nombre { get; set; } // "Cancha 1"
    public bool Activo { get; set; } = true;

    public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
}
