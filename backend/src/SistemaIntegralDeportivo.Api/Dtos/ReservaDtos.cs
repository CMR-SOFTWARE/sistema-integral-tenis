using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Un horario (día/hora) de un grupo disponible, con su precio estimado por clase.</summary>
public class HorarioDisponibleDto
{
    public string Dia { get; set; } = string.Empty; // "Tuesday" → el front lo traduce
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    public string Sede { get; set; } = string.Empty;
    public string Cancha { get; set; } = string.Empty;
    /// <summary>valorHoraGrupal × (duración/60) ÷ (miembros + el alumno). Null si el profe no configuró precios.</summary>
    public decimal? PrecioEstimado { get; set; }
}

/// <summary>Un grupo al que el alumno PODRÍA sumarse (tiene cupo y coincide su categoría).</summary>
public class GrupoDisponibleDto
{
    public Guid GrupoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public int MiembrosActivos { get; set; }
    public int? CupoMaximo { get; set; }
    public List<HorarioDisponibleDto> Horarios { get; set; } = [];
    /// <summary>Ya mandó una solicitud pendiente para este grupo (el front deshabilita el botón).</summary>
    public bool SolicitudPendiente { get; set; }
}

/// <summary>El alumno pide sumarse a un grupo (solo el id).</summary>
public class SolicitarGrupoDto
{
    [Required]
    public Guid GrupoId { get; set; }
}

/// <summary>Una solicitud de grupo (vista por el profe o por el alumno).</summary>
public class SolicitudGrupoDto
{
    public Guid Id { get; set; }
    public Guid AlumnoId { get; set; }
    public string AlumnoNombre { get; set; } = string.Empty;
    public Guid GrupoId { get; set; }
    public string GrupoNombre { get; set; } = string.Empty;
    /// <summary>Pendiente | Aceptada | Rechazada.</summary>
    public string Estado { get; set; } = string.Empty;
    public DateTime CreadoEl { get; set; }
    public DateTime? ResueltoEl { get; set; }
}
