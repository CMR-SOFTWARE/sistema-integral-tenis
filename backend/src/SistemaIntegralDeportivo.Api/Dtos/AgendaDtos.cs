using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Alta de horario recurrente. Grupal XOR individual (valida el service).</summary>
public class CreateHorarioDto
{
    [Required]
    public Guid CanchaId { get; set; }

    public Guid? GrupoId { get; set; }
    public Guid? AlumnoId { get; set; }

    [Required]
    public DayOfWeek Dia { get; set; }

    [Required]
    public TimeOnly HoraInicio { get; set; }

    [Range(15, 240)]
    public int DuracionMinutos { get; set; } = 60;
}

/// <summary>Horario para la grilla semanal del mockup.</summary>
public class HorarioResponseDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty; // "Intermedios" o "Juan Pérez (individual)"
    public string? Categoria { get; set; }
    public bool EsIndividual { get; set; }
    public Guid CanchaId { get; set; }
    public string Cancha { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
    public DayOfWeek Dia { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Participante del roster de un turno + asistencia.</summary>
public class ParticipanteTurnoDto
{
    public Guid AlumnoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public bool Presente { get; set; }
    public bool DeudaVencida { get; set; }
}

/// <summary>Turno concreto para el calendario.</summary>
public class TurnoResponseDto
{
    public Guid Id { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? CanceladoMotivo { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Cancha { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
    public List<ParticipanteTurnoDto> Participantes { get; set; } = [];
}

/// <summary>Body de PATCH turnos/{id}/asistencia.</summary>
public class AsistenciaDto
{
    [Required]
    public Guid AlumnoId { get; set; }
    public bool Presente { get; set; }
}

/// <summary>Body de POST turnos/{id}/cancelar.</summary>
public class CancelarTurnoDto
{
    [Required, StringLength(200)]
    public string Motivo { get; set; } = string.Empty;
}
