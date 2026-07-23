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

    /// <summary>Opcional: el profe no siempre tiene el DNI. Único por tenant cuando está.</summary>
    [StringLength(15)]
    public string? Dni { get; set; }

    /// <summary>Obligatorio: es el USUARIO de login y la contraseña inicial del alumno.</summary>
    [Required(ErrorMessage = "El teléfono es obligatorio: es el usuario y la contraseña inicial del alumno."),
     StringLength(25)]
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Opcional: si el alumno tiene email, se guarda (no es la llave de login).</summary>
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>Opcional (el profe no siempre la tiene). La condición de menor va en EsMenor.</summary>
    public DateTime? FechaNacimiento { get; set; }

    /// <summary>Lo marca el profe con un checkbox: si es true, exige tutor + consentimiento.</summary>
    public bool EsMenor { get; set; }

    public CategoriaAlumno Categoria { get; set; } = CategoriaAlumno.SinCategoria;

    [Range(0, 99_999_999)]
    public decimal? Arancel { get; set; }

    /// <summary>Profe de cabecera (dueño o staff); opcional.</summary>
    public Guid? ProfesorUserId { get; set; }

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

    [StringLength(15)]
    public string? Dni { get; set; }

    [Required, StringLength(25)]
    public string Telefono { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    /// <summary>Menor de edad (Ley 25.326): dispara la regla de tutor.</summary>
    public bool EsMenor { get; set; }

    public CategoriaAlumno Categoria { get; set; } = CategoriaAlumno.SinCategoria;

    public ModalidadPago Modalidad { get; set; } = ModalidadPago.Mensual;

    /// <summary>Profe de cabecera (dueño o staff); opcional.</summary>
    public Guid? ProfesorUserId { get; set; }

    [StringLength(500)]
    public string? Notas { get; set; }
}

/// <summary>
/// Respuesta del ALTA: la ficha + (si se pudo) las credenciales del portal. La
/// temporal se muestra UNA sola vez (no se persiste ni vuelve a aparecer). Si el
/// celular ya tenía cuenta, la ficha se crea igual pero SIN acceso.
/// </summary>
public class AlumnoCreadoDto
{
    public required AlumnoResponseDto Alumno { get; set; }

    /// <summary>¿Se creó el acceso al portal? false si ese celular ya tenía cuenta.</summary>
    public bool AccesoCreado { get; set; }

    /// <summary>Usuario (el celular) para pasarle al alumno; null si no se creó acceso.</summary>
    public string? Usuario { get; set; }

    /// <summary>Contraseña inicial (el mismo celular); null si no se creó acceso.</summary>
    public string? PasswordTemporal { get; set; }
}

/// <summary>
/// Body de "Crear acceso" (fichas sin login): teléfono ALTERNATIVO, solo si el de
/// la ficha ya está usado por otra cuenta (ej. hermano con el mismo celu).
/// </summary>
public class CrearAccesoDto
{
    [StringLength(25)]
    public string? Telefono { get; set; }
}

/// <summary>Respuesta de "Crear acceso": credenciales para pasarle al alumno.</summary>
public class AccesoCreadoDto
{
    public required string Usuario { get; set; }
    public required string PasswordTemporal { get; set; }
}
