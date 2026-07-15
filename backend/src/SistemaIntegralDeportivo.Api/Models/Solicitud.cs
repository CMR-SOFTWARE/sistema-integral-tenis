namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Solicitud de un JUGADOR para tomar clases con un profe (reemplaza al
/// reclamo de ficha): el alumno la manda desde su portal, el profe la ve
/// con los datos del solicitante y la aprueba (nace/se vincula su ficha)
/// o la rechaza. Una sola pendiente por (usuario, club).
/// </summary>
public class Solicitud
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>El jugador que pide (identidad global, sin navegación — como Alumno.UserId).</summary>
    public Guid UserId { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

    /// <summary>Mensaje opcional del alumno al profe ("juego los martes...").</summary>
    public string? Mensaje { get; set; }

    /// <summary>La ficha creada/vinculada al aprobar.</summary>
    public Guid? AlumnoId { get; set; }

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
    public DateTime? ResueltoEl { get; set; }
}
