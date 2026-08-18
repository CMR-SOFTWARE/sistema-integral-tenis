using System.ComponentModel.DataAnnotations;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Métricas globales de la plataforma (todos los clubes juntos).</summary>
public class MetricasPlataformaDto
{
    public int TotalClubes { get; set; }
    public int ClubesActivos { get; set; }
    public int ClubesPendientes { get; set; }
    public int ClubesSuspendidos { get; set; }

    /// <summary>Dueños + staff activos de toda la plataforma.</summary>
    public int TotalProfes { get; set; }

    /// <summary>Todas las personas dadas de alta en los clubes, tengan clase o no.</summary>
    public int TotalUsuarios { get; set; }

    /// <summary>Suma de pagos confirmados en el mes en curso (aprox. de facturación).</summary>
    public decimal IngresosMes { get; set; }

    public int ClubesNuevos30d { get; set; }
    public int AlumnosNuevos30d { get; set; }
}

/// <summary>Un club/tenant en la lista del admin (con su profe dueño y su tamaño).</summary>
public class ClubAdminDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Subdominio { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Profesor { get; set; } = string.Empty;
    public int Alumnos { get; set; }
    public DateTime CreadoEl { get; set; }
}

/// <summary>Body para activar/suspender un club (solo Activo o Suspendido).</summary>
public class CambiarEstadoClubDto
{
    public EstadoTenant Estado { get; set; }
}

/// <summary>
/// Alta de una academia desde Plataforma (Bloque 6, pedido 10): el admin crea el club
/// y la cuenta del director (como a un empleado), pero SALTEA el checkout de Mercado
/// Pago — nace directo Activa.
/// </summary>
public class AltaClubDto
{
    [Required(ErrorMessage = "Poné el nombre del club o academia."), StringLength(80)]
    public string NombreClub { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Apellido { get; set; } = string.Empty;

    /// <summary>Es el usuario y la contraseña inicial del director (como a un empleado).</summary>
    [Required(ErrorMessage = "El teléfono es obligatorio (es el usuario y la contraseña inicial del director)."),
     RegularExpression(FormatosAuth.Telefono, ErrorMessage = FormatosAuth.TelefonoMensaje)]
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Opcional: si lo tenés, se guarda (no es la llave de login).</summary>
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string? Email { get; set; }
}

/// <summary>Respuesta del alta: el club + la clave temporal del director (se muestra una sola vez).</summary>
public class ClubCreadoDto
{
    public ClubAdminDto Club { get; set; } = new();
    /// <summary>El usuario (el celular) para pasarle al director.</summary>
    public string Usuario { get; set; } = string.Empty;
    /// <summary>La contraseña inicial (el mismo celular).</summary>
    public string PasswordTemporal { get; set; } = string.Empty;
}

/// <summary>
/// El padrón de PERSONAS de Plataforma (Bloque 6, pedido 11): una proyección de
/// AspNetUsers con sus membresías, NO la tabla Alumnos (esa es un padrón de FICHAS,
/// deja afuera al director/staff sin ficha y duplica a quien tiene fichas en
/// varios clubes). Una persona puede tener varios roles.
/// </summary>
public class PersonaAdminDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool EsAdminPlataforma { get; set; }
    public List<RolPersonaDto> Roles { get; set; } = new();
}

/// <summary>Un rol de la persona en un club puntual (puede tener varios, en clubes distintos).</summary>
public class RolPersonaDto
{
    public string Tipo { get; set; } = string.Empty; // "Dueño" | "Staff" | "Alumno"
    public string Club { get; set; } = string.Empty;
}
