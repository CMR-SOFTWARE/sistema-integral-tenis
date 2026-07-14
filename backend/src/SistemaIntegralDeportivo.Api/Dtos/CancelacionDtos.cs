namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>
/// Una cancelación reciente vista por el PROFE: turno entero cancelado
/// (por él o por un bloqueo) o aviso individual de un alumno.
/// </summary>
public class CancelacionDto
{
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Motivo { get; set; }

    /// <summary>"Profesor" o "Alumno".</summary>
    public string Por { get; set; } = string.Empty;

    /// <summary>Quién avisó (avisos de alumno; en clase individual, el alumno).</summary>
    public string? AlumnoNombre { get; set; }

    /// <summary>Para el botón WhatsApp (si hay a quién escribirle).</summary>
    public string? Telefono { get; set; }

    public DateTime CanceladoEl { get; set; }
}
