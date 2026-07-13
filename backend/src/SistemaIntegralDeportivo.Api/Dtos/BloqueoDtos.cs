using System.ComponentModel.DataAnnotations;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Alta de bloqueo. Fijo: requiere Dia; Rango: requiere Fecha y Motivo.</summary>
public class CreateBloqueoDto
{
    [Required]
    public TipoBloqueo Tipo { get; set; }

    public DayOfWeek? Dia { get; set; }
    public DateOnly? Fecha { get; set; }

    [Required]
    public TimeOnly HoraInicio { get; set; }

    [Required]
    public TimeOnly HoraFin { get; set; }

    /// <summary>null = todas las canchas.</summary>
    public Guid? CanchaId { get; set; }

    public MotivoBloqueo? Motivo { get; set; }
}

public class BloqueoResponseDto
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string? Dia { get; set; }
    public DateOnly? Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public Guid? CanchaId { get; set; }
    /// <summary>Nombre de la cancha; null = todas.</summary>
    public string? Cancha { get; set; }
    public string? Motivo { get; set; }
    public DateTime CreadoEl { get; set; }
}

/// <summary>Un alumno afectado por la cascada (una fila del modal Impacto).</summary>
public class AlumnoAfectadoDto
{
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string AlumnoNombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
}

/// <summary>Qué cancela el bloqueo: conteo de turnos y alumnos a avisar.</summary>
public class ImpactoBloqueoDto
{
    public int TurnosAfectados { get; set; }
    public List<AlumnoAfectadoDto> Afectados { get; set; } = [];
}

/// <summary>Respuesta del alta: el bloqueo creado + lo que canceló.</summary>
public class BloqueoCreadoDto
{
    public BloqueoResponseDto Bloqueo { get; set; } = new();
    public ImpactoBloqueoDto Impacto { get; set; } = new();
}
