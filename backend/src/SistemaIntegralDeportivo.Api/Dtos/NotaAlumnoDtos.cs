using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Una nota del profe sobre un alumno.</summary>
public class NotaAlumnoDto
{
    public Guid Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public bool Compartida { get; set; }
    public DateTime CreadoEl { get; set; }
}

/// <summary>Alta de una nota (el profe elige si la comparte con el alumno).</summary>
public class CrearNotaAlumnoDto
{
    [Required, StringLength(1000)]
    public string Texto { get; set; } = string.Empty;

    public bool Compartida { get; set; }
}
