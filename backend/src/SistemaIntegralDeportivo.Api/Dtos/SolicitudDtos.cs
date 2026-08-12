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

/// <summary>Por qué alguien figura en la lista de espera.</summary>
public enum MotivoEspera
{
    /// <summary>Todavía no tiene ninguna clase asignada.</summary>
    SinClase,
    /// <summary>Pidió sumarse a una clase y el profe no resolvió el pedido. Puede
    /// tener otras clases: por eso alguien aparece en Alumnos y en la espera a la vez.</summary>
    PidioCupo,
    /// <summary>El profe lo anotó a mano (le pidió clase hablando, no desde el portal).
    /// Como el pedido de cupo, puede tener otras clases.</summary>
    LoAnotoElProfe,
}

/// <summary>
/// Una fila de la lista de espera vista por el PROFE. Hereda de
/// <see cref="AlumnoResponseDto"/> a propósito: la espera muestra la MISMA tabla que
/// Alumnos (mismos datos, mismos filtros, misma ficha), y heredar es lo que evita que
/// las dos se desincronicen el día que la fila del alumno gane una columna. Lo propio
/// de la espera —por qué está y desde cuándo— se agrega acá.
/// </summary>
public class EsperaResponseDto : AlumnoResponseDto
{
    /// <summary>SinClase | PidioCupo | LoAnotoElProfe: decide qué acciones ofrece la fila.</summary>
    public string Motivo { get; set; } = string.Empty;

    /// <summary>El pedido a rechazar; solo cuando el motivo es PidioCupo.</summary>
    public Guid? SolicitudId { get; set; }

    /// <summary>La clase que pidió; solo cuando el motivo es PidioCupo.</summary>
    public string? Clase { get; set; }

    /// <summary>
    /// Desde cuándo espera, que es lo que ordena la lista. NO es <c>CreadoEl</c>: al
    /// anotado a mano se le cuenta desde que el profe lo anotó, porque su ficha puede
    /// ser de hace dos años y recién ahora haberle pedido otra clase.
    /// </summary>
    public DateTime EsperaDesde { get; set; }
}

/// <summary>Conteo para el badge del sidebar del profe.</summary>
public class ConteoSolicitudesDto
{
    public int Pendientes { get; set; }
}

/// <summary>Body para anotar/desanotar a mano en la lista de espera.</summary>
public class CambiarEsperaDto
{
    public bool EnEspera { get; set; }
}
