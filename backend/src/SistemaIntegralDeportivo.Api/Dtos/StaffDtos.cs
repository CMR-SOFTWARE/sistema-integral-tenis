using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Un profe empleado (Staff) del club, con los datos de su usuario.</summary>
public class StaffDto
{
    public Guid Id { get; set; } // id de la membresía
    public Guid UserId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

/// <summary>
/// Alta de un profe empleado: el dueño le crea la cuenta (como a un alumno). La
/// contraseña inicial es su teléfono; se muestra una vez en <see cref="StaffCreadoDto"/>.
/// </summary>
public class AgregarStaffDto
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Apellido { get; set; } = string.Empty;

    [Required, EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio (es la contraseña inicial del profe)."),
     RegularExpression(FormatosAuth.Telefono, ErrorMessage = FormatosAuth.TelefonoMensaje)]
    public string Telefono { get; set; } = string.Empty;
}

/// <summary>Respuesta del alta: el profe + su clave temporal (null si se reactivó una cuenta ya existente).</summary>
public class StaffCreadoDto
{
    public StaffDto Staff { get; set; } = new();
    /// <summary>La contraseña inicial (el teléfono) para pasarle al profe; null si ya tenía cuenta.</summary>
    public string? PasswordTemporal { get; set; }
}

/// <summary>Baja/reactivación de un profe empleado.</summary>
public class CambiarActivoStaffDto
{
    public bool Activo { get; set; }
}
