namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Franja NO disponible de la agenda. Fijo = recurrente semanal (día +
/// horas, "los lunes de 8 a 12 no doy clases"); Rango = una fecha puntual
/// con motivo ("martes 21/07: mal clima"). Al crearse cancela en cascada
/// los turnos ya generados que pisa (nadie paga: cancelación del profe);
/// los slots futuros NO generados se saltean en la generación perezosa,
/// así que eliminar el bloqueo los hace reaparecer solos. Es configuración,
/// no historia: se borra físicamente.
/// </summary>
public class Bloqueo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public TipoBloqueo Tipo { get; set; }

    /// <summary>Día de la semana (solo Fijo).</summary>
    public DayOfWeek? Dia { get; set; }

    /// <summary>Fecha puntual (solo Rango).</summary>
    public DateOnly? Fecha { get; set; }

    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }

    /// <summary>Cancha afectada; null = todas las canchas del tenant.</summary>
    public Guid? CanchaId { get; set; }
    public Cancha? Cancha { get; set; }

    /// <summary>Solo Rango (un bloqueo fijo es disponibilidad, no un evento).</summary>
    public MotivoBloqueo? Motivo { get; set; }

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
