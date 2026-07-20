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

// ── Clase individual fija (M5b) ──

/// <summary>El alumno propone una clase individual fija (elige SEDE; el profe elige la cancha al aceptar).</summary>
public class SolicitarHorarioDto
{
    [Required]
    public Guid SedeId { get; set; }

    [Required]
    public DayOfWeek Dia { get; set; }

    [Required]
    public TimeOnly HoraInicio { get; set; }

    [Range(30, 180)]
    public int DuracionMinutos { get; set; } = 60;
}

/// <summary>Una sede del club (para que el alumno elija dónde quiere la clase).</summary>
public class SedeReservaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>El profe acepta una solicitud individual eligiendo una cancha libre.</summary>
public class AceptarHorarioDto
{
    [Required]
    public Guid CanchaId { get; set; }
}

/// <summary>Una cancha libre a un día/hora (para el dropdown del profe al aceptar).</summary>
public class CanchaLibreDto
{
    public Guid CanchaId { get; set; }
    public string Cancha { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
}

/// <summary>Una solicitud de clase individual (vista por el profe o el alumno).</summary>
public class SolicitudHorarioDto
{
    public Guid Id { get; set; }
    public Guid AlumnoId { get; set; }
    public string AlumnoNombre { get; set; } = string.Empty;
    public string Dia { get; set; } = string.Empty; // "Tuesday" → el front traduce
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    /// <summary>La sede que eligió el alumno.</summary>
    public string Sede { get; set; } = string.Empty;
    /// <summary>Pendiente | Aceptada | Rechazada.</summary>
    public string Estado { get; set; } = string.Empty;
    /// <summary>Cancha asignada (solo si Aceptada).</summary>
    public string? Cancha { get; set; }
    public DateTime CreadoEl { get; set; }
    public DateTime? ResueltoEl { get; set; }
}

/// <summary>
/// Si hay lugar para una clase individual a un día/hora (chequea TODAS las
/// canchas del club). El alumno lo ve en vivo al armar el pedido.
/// </summary>
public class DisponibilidadDto
{
    public bool HayLugar { get; set; }
    public int CanchasLibres { get; set; }
}
