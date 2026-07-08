using System.ComponentModel.DataAnnotations;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Alta de grupo fijo ("Intermedios martes").</summary>
public class CreateGrupoDto
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Categoría sugerida para armar grupos parejos (opcional).</summary>
    public CategoriaAlumno? Categoria { get; set; }

    /// <summary>Null = sin límite de integrantes.</summary>
    [Range(1, 50)]
    public int? CupoMaximo { get; set; }
}

/// <summary>Miembro activo de un grupo (para la tarjeta del mockup).</summary>
public class MiembroGrupoDto
{
    public Guid AlumnoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
}

/// <summary>Borde de salida del grupo con sus miembros activos.</summary>
public class GrupoResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public int? CupoMaximo { get; set; }
    public bool Activo { get; set; }
    public int MiembrosActivos { get; set; }
    public List<MiembroGrupoDto> Miembros { get; set; } = [];
}

/// <summary>Body de POST grupos/{id}/alumnos.</summary>
public class AsignarAlumnoDto
{
    [Required]
    public Guid AlumnoId { get; set; }
}
