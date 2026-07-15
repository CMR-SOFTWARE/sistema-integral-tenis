using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>El jugador pide entrar a un club (desde el portal).</summary>
public class CrearSolicitudDto
{
    [Required]
    public Guid TenantId { get; set; }

    [StringLength(200)]
    public string? Mensaje { get; set; }
}

/// <summary>Una solicitud MÍA vista desde el portal (con su estado).</summary>
public class MiSolicitudDto
{
    public Guid Id { get; set; }
    public string Club { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty; // Pendiente | Aprobada | Rechazada
    public string? Mensaje { get; set; }
    public DateTime CreadoEl { get; set; }
    public DateTime? ResueltoEl { get; set; }
}

/// <summary>Una solicitud pendiente vista por el PROFE (datos del solicitante para decidir).</summary>
public class SolicitudPendienteDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? Telefono { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public bool EsMenor { get; set; }
    public string? Categoria { get; set; }
    public string? Mensaje { get; set; }
    public DateTime CreadoEl { get; set; }
}

/// <summary>Conteo para el badge del sidebar del profe.</summary>
public class ConteoSolicitudesDto
{
    public int Pendientes { get; set; }
}
