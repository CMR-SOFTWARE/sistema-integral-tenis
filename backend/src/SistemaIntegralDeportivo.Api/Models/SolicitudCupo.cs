namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// El alumno pide un lugar en una clase fija desde el portal. Solo puede pedir
/// horarios con cupo libre y de su categoría; el profe la ACEPTA (lo suma al roster,
/// que reconcilia el calendario) o la RECHAZA.
///
/// Reemplaza a <see cref="SolicitudGrupo"/>, que pedía entrar a un grupo. Se llama
/// "cupo" y no "horario" para no confundirla con <see cref="SolicitudHorario"/>, que
/// es otra cosa: pedir que le armen una clase individual nueva.
/// </summary>
public class SolicitudCupo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    public Guid HorarioId { get; set; }
    public Horario Horario { get; set; } = null!;

    public EstadoSolicitudGrupo Estado { get; set; } = EstadoSolicitudGrupo.Pendiente;

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
    /// <summary>Cuándo el profe la aceptó o rechazó (null mientras está Pendiente).</summary>
    public DateTime? ResueltoEl { get; set; }
}
