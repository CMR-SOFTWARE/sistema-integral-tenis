namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Instancia CONCRETA de un horario en una fecha: "mar 14/07 18:00 con Juan,
/// Sofía, Mateo y Vale". Su roster (Participantes) queda CONGELADO al
/// generarse: fija el divisor del precio (modelo-precios.md). Se cancela con
/// motivo, nunca se borra.
/// </summary>
public class Turno
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid HorarioId { get; set; }
    public Horario Horario { get; set; } = null!;

    // Denormalizados del horario al momento de generar (el horario puede
    // cambiar después; el turno ya jugado es historia intocable)
    public Guid CanchaId { get; set; }
    public Cancha Cancha { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; }

    public EstadoTurno Estado { get; set; } = EstadoTurno.Programado;
    public string? CanceladoMotivo { get; set; }
    public DateTime? CanceladoEl { get; set; }
    /// <summary>Quién lo canceló (null en cancelaciones previas a esta columna).</summary>
    public CanceladoPor? CanceladoPor { get; set; }

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;

    public ICollection<TurnoParticipante> Participantes { get; set; } = new List<TurnoParticipante>();
}
