using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Un profe empleado (Staff) del club, con los datos de su usuario (para la ficha).</summary>
public class StaffDto
{
    public Guid Id { get; set; } // id de la membresía
    public Guid UserId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    /// <summary>Celular (es el login del profe): se muestra, no se edita desde la ficha.</summary>
    public string Telefono { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public bool Activo { get; set; }
    /// <summary>Valor hora base (lo que se le paga por hora de clase); null = sin definir.</summary>
    public decimal? ValorHora { get; set; }
    /// <summary>Alta del profe en el club (para la ficha).</summary>
    public DateTime CreadoEl { get; set; }
}

/// <summary>Edición de la ficha del empleado (el celular/login NO se toca acá).</summary>
public class UpdateStaffDto
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Apellido { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string? Email { get; set; }

    [StringLength(15)]
    public string? Dni { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    [Range(0, 99_999_999)]
    public decimal? ValorHora { get; set; }
}

/// <summary>
/// Alta de un profe empleado: el dueño le crea la cuenta (como a un alumno). El
/// teléfono es el USUARIO y la contraseña inicial; se muestran una vez en
/// <see cref="StaffCreadoDto"/>. El email es opcional.
/// </summary>
public class AgregarStaffDto
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Apellido { get; set; } = string.Empty;

    /// <summary>Opcional: si lo tenés, se guarda (no es la llave de login).</summary>
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio (es el usuario y la contraseña inicial del profe)."),
     RegularExpression(FormatosAuth.Telefono, ErrorMessage = FormatosAuth.TelefonoMensaje)]
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Valor hora base del profe (opcional; se puede cargar/editar después).</summary>
    [Range(0, 99_999_999)]
    public decimal? ValorHora { get; set; }
}

/// <summary>Respuesta del alta: el profe + su usuario/clave temporal (null si se reactivó una cuenta existente).</summary>
public class StaffCreadoDto
{
    public StaffDto Staff { get; set; } = new();
    /// <summary>El usuario (el celular) para pasarle al profe; null si ya tenía cuenta.</summary>
    public string? Usuario { get; set; }
    /// <summary>La contraseña inicial (el mismo celular); null si ya tenía cuenta.</summary>
    public string? PasswordTemporal { get; set; }
}

/// <summary>Baja/reactivación de un profe empleado.</summary>
public class CambiarActivoStaffDto
{
    public bool Activo { get; set; }
}

/// <summary>Un profe al que el dueño le puede asignar horarios/grupos/alumnos (dueño + staff activos).</summary>
public class ProfesorAsignableDto
{
    public Guid UserId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool EsDueño { get; set; }
    /// <summary>Valor hora base del empleado (para pre-cargar el del horario); null en el dueño.</summary>
    public decimal? ValorHora { get; set; }
}

/// <summary>Body para (re)asignar el profe de un horario o grupo (null = sacar la asignación).</summary>
public class AsignarProfesorDto
{
    public Guid? ProfesorUserId { get; set; }
    /// <summary>Solo horarios: valor hora del profe para esa clase (override; null = base del profe).</summary>
    [Range(0, 99_999_999)]
    public decimal? ValorHoraProfe { get; set; }
}
