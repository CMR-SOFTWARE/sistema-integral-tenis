namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// El alumno pide una CLASE INDIVIDUAL FIJA desde el portal (M5b): un horario
/// propio recurrente (él solo con el profe) en un día/hora. Propone día + hora
/// + duración; el profe la ACEPTA eligiendo una cancha libre (se crea el
/// <see cref="Horario"/> individual) o la RECHAZA. No confundir con
/// <see cref="SolicitudGrupo"/> (sumarse a un grupo existente).
/// </summary>
public class SolicitudHorario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    // ── Lo que propone el alumno ──
    /// <summary>La SEDE (lugar) donde quiere la clase; el profe elige la cancha dentro.</summary>
    public Guid SedeId { get; set; }
    public Sede Sede { get; set; } = null!;
    public DayOfWeek Dia { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; } = 60;

    public EstadoSolicitudHorario Estado { get; set; } = EstadoSolicitudHorario.Pendiente;

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
    public DateTime? ResueltoEl { get; set; }

    // ── Se completan al aceptar: la cancha que eligió el profe y el horario creado ──
    public Guid? CanchaId { get; set; }
    public Cancha? Cancha { get; set; }
    public Guid? HorarioId { get; set; }
    public Horario? Horario { get; set; }
}
