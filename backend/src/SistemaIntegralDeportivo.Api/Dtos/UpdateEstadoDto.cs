using System.ComponentModel.DataAnnotations;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Cambio de estado del alumno (pausar/reactivar). La baja tiene su endpoint.</summary>
public class UpdateEstadoDto
{
    [Required]
    public EstadoAlumno Estado { get; set; }
}

/// <summary>Cambio del profe titular desde la ficha (null = desasignar).</summary>
public class CambiarProfesorDto
{
    public Guid? ProfesorUserId { get; set; }
}
