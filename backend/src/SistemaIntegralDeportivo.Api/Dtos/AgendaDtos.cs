using System.ComponentModel.DataAnnotations;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Alta de una clase fija: dónde, cuándo, quién la da y quiénes vienen.</summary>
public class CreateHorarioDto
{
    [Required]
    public Guid CanchaId { get; set; }

    /// <summary>Cómo se llama la clase ("Intermedios"); vacío = se arma solo con el roster.</summary>
    [StringLength(80)]
    public string? Nombre { get; set; }

    /// <summary>Cuántos alumnos entran; null = sin límite.</summary>
    [Range(1, 50)]
    public int? CupoMaximo { get; set; }

    /// <summary>Categoría sugerida (para que el portal ofrezca clases parejas).</summary>
    public CategoriaAlumno? Categoria { get; set; }

    /// <summary>Los alumnos que arrancan en la clase; puede venir vacío y sumarlos después.</summary>
    public List<Guid> AlumnoIds { get; set; } = [];

    /// <summary>Profe que da la clase (dueño o staff); opcional.</summary>
    public Guid? ProfesorUserId { get; set; }

    /// <summary>Valor hora del profe para ESTA clase (override; null = usa el base del profe).</summary>
    [Range(0, 99_999_999)]
    public decimal? ValorHoraProfe { get; set; }

    [Required]
    public DayOfWeek Dia { get; set; }

    [Required]
    public TimeOnly HoraInicio { get; set; }

    [Range(15, 240)]
    public int DuracionMinutos { get; set; } = 60;
}

/// <summary>
/// Edición de una clase: cancha, día, hora, duración, profe, nombre y cupo. El
/// roster se toca con sus propios endpoints (agregar/quitar alumno), que además
/// reconcilian el calendario.
/// </summary>
public class UpdateHorarioDto
{
    [Required]
    public Guid CanchaId { get; set; }

    [StringLength(80)]
    public string? Nombre { get; set; }

    [Range(1, 50)]
    public int? CupoMaximo { get; set; }

    public CategoriaAlumno? Categoria { get; set; }

    /// <summary>Profe que da la clase (dueño o staff); null = sin asignar.</summary>
    public Guid? ProfesorUserId { get; set; }

    /// <summary>Valor hora del profe para esta clase (override; null = usa el base del profe).</summary>
    [Range(0, 99_999_999)]
    public decimal? ValorHoraProfe { get; set; }

    [Required]
    public DayOfWeek Dia { get; set; }

    [Required]
    public TimeOnly HoraInicio { get; set; }

    [Range(15, 240)]
    public int DuracionMinutos { get; set; } = 60;
}

/// <summary>Un alumno del roster de una clase fija.</summary>
public class MiembroHorarioDto
{
    public Guid AlumnoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
}

/// <summary>Una clase fija: cuándo, dónde, quién la da y quiénes vienen.</summary>
public class HorarioResponseDto
{
    public Guid Id { get; set; }
    /// <summary>Lo que se muestra: el nombre cargado, o uno armado con el roster.</summary>
    public string Titulo { get; set; } = string.Empty;
    /// <summary>El nombre TAL CUAL lo cargó el profe (null = se arma solo). Para el formulario de edición.</summary>
    public string? Nombre { get; set; }
    public string? Categoria { get; set; }
    public int? CupoMaximo { get; set; }
    public Guid CanchaId { get; set; }
    public string Cancha { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
    public DayOfWeek Dia { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    public bool Activo { get; set; }
    /// <summary>Profe asignado (dueño o staff); null = sin asignar. El front mapea el nombre.</summary>
    public Guid? ProfesorUserId { get; set; }
    /// <summary>Valor hora del profe para esta clase (override; null = usa el base del profe).</summary>
    public decimal? ValorHoraProfe { get; set; }
    /// <summary>Los que vienen hoy (sin los que se dieron de baja).</summary>
    public List<MiembroHorarioDto> Miembros { get; set; } = [];
    public int MiembrosActivos { get; set; }
}

/// <summary>Body para sumar un alumno al roster de una clase.</summary>
public class AgregarAlumnoHorarioDto
{
    [Required]
    public Guid AlumnoId { get; set; }
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
    /// <summary>Profe a cargo (del horario); null = suelto o sin asignar. Para el filtro por profe.</summary>
    public Guid? ProfesorUserId { get; set; }
    /// <summary>El horario (plantilla) del que salió; null = clase suelta. Habilita las acciones de horario desde el turno.</summary>
    public Guid? HorarioId { get; set; }
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
