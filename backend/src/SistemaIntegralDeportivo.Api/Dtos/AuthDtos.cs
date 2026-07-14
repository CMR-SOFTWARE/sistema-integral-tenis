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

/// <summary>Registro GRATIS de jugador (ADR-0007): crea la identidad global.</summary>
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

    // Opcionales, pero son la llave del reclamo de ficha (match por DNI/teléfono)
    [RegularExpression(FormatosAuth.Dni, ErrorMessage = FormatosAuth.DniMensaje)]
    public string? Dni { get; set; }

    [RegularExpression(FormatosAuth.Telefono, ErrorMessage = FormatosAuth.TelefonoMensaje)]
    public string? Telefono { get; set; }
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

public class ReclamarFichaDto
{
    public Guid AlumnoId { get; set; }
}

/// <summary>Corregir DNI/teléfono de MI cuenta (para que el reclamo matchee).</summary>
public class ActualizarMisDatosDto
{
    [RegularExpression(FormatosAuth.Dni, ErrorMessage = FormatosAuth.DniMensaje)]
    public string? Dni { get; set; }

    [RegularExpression(FormatosAuth.Telefono, ErrorMessage = FormatosAuth.TelefonoMensaje)]
    public string? Telefono { get; set; }
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

    /// <summary>Dueño de un tenant (membresía Profesor, ADR-0007).</summary>
    public bool EsProfesor { get; set; }

    /// <summary>Ficha vinculada (habilita el portal); null si no reclamó ninguna.</summary>
    public FichaDto? Alumno { get; set; }

    /// <summary>Fichas precargadas por negocios que coinciden con mi DNI/teléfono.</summary>
    public List<FichaDto> FichasPorReclamar { get; set; } = [];
}
