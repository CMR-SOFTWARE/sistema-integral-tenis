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

    /// <summary>Obligatorio: el alta crea TAMBIÉN el usuario del portal (plan v2).</summary>
    [Required(ErrorMessage = "El email es obligatorio: con él se crea el acceso del alumno."), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public DateTime FechaNacimiento { get; set; }

    public CategoriaAlumno Categoria { get; set; } = CategoriaAlumno.SinCategoria;

    [Range(0, 99_999_999)]
    public decimal? Arancel { get; set; }

    [StringLength(500)]
    public string? Notas { get; set; }

    public bool ConsentimientoWhatsapp { get; set; }

    /// <summary>Si el alumno es menor, lo otorga el tutor y es obligatorio.</summary>
    public bool ConsentimientoDatos { get; set; }

    /// <summary>Obligatorio cuando el alumno es menor de 18.</summary>
    public TutorDto? Tutor { get; set; }
}

/// <summary>
/// Edición de la ficha por el PROFE. No incluye credenciales (el email de
/// login vive en Identity) ni tutor (se administra aparte).
/// </summary>
public class UpdateAlumnoDto
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

    public ModalidadPago Modalidad { get; set; } = ModalidadPago.Mensual;

    [StringLength(500)]
    public string? Notas { get; set; }
}

/// <summary>
/// Respuesta del ALTA: la ficha + las credenciales del portal. La temporal
/// se muestra UNA sola vez (no se persiste ni vuelve a aparecer).
/// </summary>
public class AlumnoCreadoDto
{
    public required AlumnoResponseDto Alumno { get; set; }
    public required string Email { get; set; }
    public required string PasswordTemporal { get; set; }
}

/// <summary>Body de "Crear acceso" (fichas viejas): email solo si la ficha no tiene.</summary>
public class CrearAccesoDto
{
    [EmailAddress]
    public string? Email { get; set; }
}

/// <summary>Respuesta de "Crear acceso": credenciales para pasarle al alumno.</summary>
public class AccesoCreadoDto
{
    public required string Email { get; set; }
    public required string PasswordTemporal { get; set; }
}
