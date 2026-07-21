using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

// Formatos compartidos: DNI solo dígitos; teléfono flexible (el match del
// reclamo es igualdad EXACTA de strings — formato sugerido E.164)
public static class FormatosAuth
{
    public const string Dni = @"^\d{7,9}$";
    public const string DniMensaje = "El DNI debe tener solo números (7 a 9 dígitos), sin puntos.";
    public const string Telefono = @"^\+?[0-9 -]{8,20}$";
    public const string TelefonoMensaje = "El teléfono debe tener entre 8 y 20 dígitos (podés incluir el +54).";
}

/// <summary>
/// Registro GRATIS de jugador (ADR-0007): crea la identidad global con los
/// datos deportivos completos (viajan a la ficha al vincularse a un club).
/// </summary>
public class RegistroJugadorDto
{
    [Required, StringLength(80)]
    public required string Nombre { get; set; }

    [Required, StringLength(80)]
    public required string Apellido { get; set; }

    [Required, EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public required string Email { get; set; }

    [Required, MinLength(8, ErrorMessage = "La contraseña necesita al menos 8 caracteres.")]
    public required string Password { get; set; }

    [Required(ErrorMessage = "El DNI es obligatorio."),
     RegularExpression(FormatosAuth.Dni, ErrorMessage = FormatosAuth.DniMensaje)]
    public required string Dni { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio."),
     RegularExpression(FormatosAuth.Telefono, ErrorMessage = FormatosAuth.TelefonoMensaje)]
    public required string Telefono { get; set; }

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public required DateTime FechaNacimiento { get; set; }

    /// <summary>"No sé todavía" = SinCategoria.</summary>
    public Models.CategoriaAlumno Categoria { get; set; } = Models.CategoriaAlumno.SinCategoria;
}

/// <summary>Registro de PROFESOR: identidad + su club, que nace PENDIENTE_PAGO.</summary>
public class RegistroProfesorDto
{
    [Required, StringLength(80)]
    public required string Nombre { get; set; }

    [Required, StringLength(80)]
    public required string Apellido { get; set; }

    [Required, EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public required string Email { get; set; }

    [Required, MinLength(8, ErrorMessage = "La contraseña necesita al menos 8 caracteres.")]
    public required string Password { get; set; }

    [RegularExpression(FormatosAuth.Dni, ErrorMessage = FormatosAuth.DniMensaje)]
    public string? Dni { get; set; }

    [RegularExpression(FormatosAuth.Telefono, ErrorMessage = FormatosAuth.TelefonoMensaje)]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "Poné el nombre de tu club o academia."), StringLength(80)]
    public required string NombreClub { get; set; }
}

/// <summary>Un profesor visible públicamente (búsqueda del registro/solicitudes).</summary>
public class ProfesorPublicoDto
{
    public Guid TenantId { get; set; }
    public string Club { get; set; } = string.Empty;
    public string Profesor { get; set; } = string.Empty;
}

public class LoginDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

/// <summary>Ficha de alumno en un negocio, para vincular o ya vinculada.</summary>
public class FichaDto
{
    public Guid AlumnoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Club { get; set; } = string.Empty;
}

/// <summary>
/// Los datos deportivos de MI cuenta (jugador sin club los completa desde
/// su perfil: la solicitud los necesita para armar la ficha).
/// </summary>
public class MisDatosDto
{
    [Required(ErrorMessage = "El DNI es obligatorio."),
     RegularExpression(FormatosAuth.Dni, ErrorMessage = FormatosAuth.DniMensaje)]
    public required string Dni { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio."),
     RegularExpression(FormatosAuth.Telefono, ErrorMessage = FormatosAuth.TelefonoMensaje)]
    public required string Telefono { get; set; }

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public required DateTime FechaNacimiento { get; set; }

    public Models.CategoriaAlumno Categoria { get; set; } = Models.CategoriaAlumno.SinCategoria;
}

/// <summary>Cambio de contraseña (opcional, desde el perfil).</summary>
public class CambiarPasswordDto
{
    [Required]
    public required string PasswordActual { get; set; }

    [Required, MinLength(8, ErrorMessage = "La contraseña nueva necesita al menos 8 caracteres.")]
    public required string PasswordNueva { get; set; }
}

/// <summary>
/// La sesión que ve el front: quién soy y qué membresías tengo. Con eso el
/// front decide la vista (dashboard de gestión / portal alumno / reclamo).
/// </summary>
public class SesionDto
{
    /// <summary>Solo en login/registro; en GET /yo va null (ya lo tenés).</summary>
    public string? Token { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Trabaja en un tenant ACTIVO (dueño o staff): habilita el panel de gestión.</summary>
    public bool EsProfesor { get; set; }

    /// <summary>"owner" (head pro) o "staff" (profe empleado); null si no es profe. El front arma el menú con esto.</summary>
    public string? Rol { get; set; }

    /// <summary>Estado del club propio ("PendientePago" manda al checkout); null si no tiene.</summary>
    public string? EstadoTenant { get; set; }

    /// <summary>Nació con contraseña inicial del profe (informativo).</summary>
    public bool DebeCambiarPassword { get; set; }

    // ── Mis datos deportivos (para prefill del perfil sin club y para saber
    //    si la solicitud es posible) ──
    public string? Dni { get; set; }
    public string? Telefono { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? Categoria { get; set; }

    /// <summary>Ficha vinculada (habilita el portal); null si no está en ningún club.</summary>
    public FichaDto? Alumno { get; set; }
}
