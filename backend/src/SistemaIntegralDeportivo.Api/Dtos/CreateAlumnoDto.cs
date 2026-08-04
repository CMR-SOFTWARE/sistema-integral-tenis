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

    /// <summary>
    /// El club (sede) donde entrena; opcional. Se elige explícitamente y NO se mueve
    /// al cambiarle el profe: el profe que se le asigne tiene que trabajar acá.
    /// </summary>
    public Guid? SedeId { get; set; }

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
/// Edición de la ficha por el PROFE. No incluye credenciales (el email de login
/// vive en Identity). El tutor es OPCIONAL: se puede marcar menor sin cargarlo y
/// completarlo después (si viene y el alumno no tenía tutor, se crea y vincula).
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

    /// <summary>Cuota mensual del alumno (la fuente de verdad del cobro mensual).</summary>
    [Range(0, 99_999_999)]
    public decimal? Arancel { get; set; }

    /// <summary>
    /// El club (sede) donde entrena; opcional. Se elige explícitamente y NO se mueve
    /// al cambiarle el profe: el profe que se le asigne tiene que trabajar acá.
    /// </summary>
    public Guid? SedeId { get; set; }

    /// <summary>Profe de cabecera (dueño o staff); opcional.</summary>
    public Guid? ProfesorUserId { get; set; }

    [StringLength(500)]
    public string? Notas { get; set; }

    /// <summary>
    /// Tutor a cargar (opcional). Solo se usa si el alumno TODAVÍA no tiene tutor:
    /// permite completarlo después de haberlo marcado menor. No borra ni pisa uno existente.
    /// </summary>
    public TutorDto? Tutor { get; set; }
}

/// <summary>
/// Respuesta del ALTA: la ficha + (si se pudo) las credenciales del portal. La
/// temporal se muestra UNA sola vez (no se persiste ni vuelve a aparecer). Si el
/// celular ya tenía cuenta, la ficha se crea igual pero SIN acceso.
/// </summary>
public class AlumnoCreadoDto
{
    public required AlumnoResponseDto Alumno { get; set; }

    /// <summary>¿La ficha quedó con acceso al portal? (true también si se sumó a una familia).</summary>
    public bool AccesoCreado { get; set; }

    /// <summary>Usuario (el celular) para pasarle al alumno; null si se sumó a una familia existente.</summary>
    public string? Usuario { get; set; }

    /// <summary>Contraseña inicial (el mismo celular); null si se sumó a una familia existente.</summary>
    public string? PasswordTemporal { get; set; }

    /// <summary>Se sumó a una familia existente (mismo celular): entra con el login del titular.</summary>
    public bool SumadoAFamilia { get; set; }

    /// <summary>Nombre del titular a cuya familia se sumó (para el aviso al profe).</summary>
    public string? FamiliaTitular { get; set; }
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

/// <summary>
/// Respuesta de "Crear acceso": credenciales para pasarle al alumno. Si el celular
/// ya era de una cuenta (la misma persona que es staff, un hermano, el tutor…), la
/// ficha se VINCULA a ese login en vez de crear uno nuevo: entonces no hay clave
/// temporal y se avisa a quién se vinculó.
/// </summary>
public class AccesoCreadoDto
{
    /// <summary>Usuario (el celular) para pasarle al alumno; null si se vinculó a una cuenta existente.</summary>
    public string? Usuario { get; set; }

    /// <summary>Contraseña inicial (el mismo celular); null si se vinculó a una cuenta existente.</summary>
    public string? PasswordTemporal { get; set; }

    /// <summary>Se vinculó a una cuenta existente (mismo celular): entra con ESE login.</summary>
    public bool Vinculado { get; set; }

    /// <summary>Nombre del titular de la cuenta a la que se vinculó (para el aviso al profe).</summary>
    public string? Titular { get; set; }
}
