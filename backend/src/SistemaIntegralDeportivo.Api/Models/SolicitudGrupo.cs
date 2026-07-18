namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// El alumno pide sumarse a un GRUPO desde el portal (M5a). Solo puede pedir
/// grupos con cupo y de su categoría; el profe la ACEPTA (lo suma al grupo vía
/// AsignarAlumnoAsync, que reconcilia el calendario) o la RECHAZA. No confundir
/// con <see cref="Solicitud"/>, que es el pedido de entrar a un CLUB.
/// </summary>
public class SolicitudGrupo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    public Guid GrupoId { get; set; }
    public Grupo Grupo { get; set; } = null!;

    public EstadoSolicitudGrupo Estado { get; set; } = EstadoSolicitudGrupo.Pendiente;

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
    /// <summary>Cuándo el profe la aceptó o rechazó (null mientras está Pendiente).</summary>
    public DateTime? ResueltoEl { get; set; }
}
