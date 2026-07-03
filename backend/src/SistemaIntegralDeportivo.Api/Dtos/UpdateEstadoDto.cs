using System.ComponentModel.DataAnnotations;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Cambio de estado del alumno (pausar/reactivar). La baja tiene su endpoint.</summary>
public class UpdateEstadoDto
{
    [Required]
    public EstadoAlumno Estado { get; set; }
}
