using System.ComponentModel.DataAnnotations;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Datos del tutor cuando el alumno es menor (viaja dentro del alta).</summary>
public class TutorDto
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Apellido { get; set; } = string.Empty;

    [Required, StringLength(15)]
    public string Dni { get; set; } = string.Empty;

    [Required, StringLength(25)]
    public string Telefono { get; set; } = string.Empty;

    public RelacionTutor Relacion { get; set; } = RelacionTutor.Otro;
}

/// <summary>
/// Borde de entrada del alta de alumno. Las DataAnnotations cubren lo
/// sintáctico (campos presentes, formatos); las reglas de negocio
/// (menor → tutor, DNI único) viven en AlumnoService.
/// </summary>
public class CreateAlumnoDto
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Apellido { get; set; } = string.Empty;

    [Required, StringLength(15)]
    public string Dni { get; set; } = string.Empty;

    [Required, StringLength(25)]
    public string Telefono { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public DateTime FechaNacimiento { get; set; }

    public CategoriaAlumno Categoria { get; set; } = CategoriaAlumno.SinCategoria;

    [Range(0, 99_999_999)]
    public decimal? Arancel { get; set; }

    public string? Notas { get; set; }

    public bool ConsentimientoWhatsapp { get; set; }

    /// <summary>Si el alumno es menor, lo otorga el tutor y es obligatorio.</summary>
    public bool ConsentimientoDatos { get; set; }

    /// <summary>Obligatorio cuando el alumno es menor de 18.</summary>
    public TutorDto? Tutor { get; set; }
}
