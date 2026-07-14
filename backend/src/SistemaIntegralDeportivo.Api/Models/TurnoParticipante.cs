namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Alumno en el roster de un turno + su asistencia. Default PRESENTE: el
/// profe solo marca al que faltó. La asistencia NO mueve la plata
/// (modelo-precios.md: el ausente paga igual; es registro + input para
/// recuperaciones a criterio del profe).
/// </summary>
public class TurnoParticipante
{
    public Guid TurnoId { get; set; }
    public Turno Turno { get; set; } = null!;

    public Guid AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    public bool Presente { get; set; } = true;

    // ── Aviso de cancelación DEL ALUMNO (portal): el turno sigue en pie para
    //    el resto y su cargo queda (= falta con aviso; modelo-precios.md).
    //    La recuperación es a discreción del profe. ──
    public DateTime? CanceloEl { get; set; }
    public string? CancelacionMotivo { get; set; }
}
