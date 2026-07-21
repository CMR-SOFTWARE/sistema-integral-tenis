using Microsoft.AspNetCore.Identity;

namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Identidad GLOBAL de plataforma (ADR-0007): una cuenta por persona real,
/// registro gratis. Lo que la persona ES en cada negocio se modela como
/// membresías (Alumno.UserId hoy; Socio/Staff en fases futuras), nunca acá.
/// Hereda de IdentityUser&lt;Guid&gt;: email, hash de contraseña y teléfono
/// (PhoneNumber) los administra ASP.NET Core Identity.
/// </summary>
public class Usuario : IdentityUser<Guid>
{
    public required string Nombre { get; set; }
    public required string Apellido { get; set; }

    /// <summary>Para el reclamo de fichas por coincidencia (modelo-identidad-roles §3).</summary>
    public string? Dni { get; set; }

    // ── Datos deportivos del JUGADOR (registro segmentado): viajan a la
    //    ficha cuando se vincula a un club (solicitudes, plan v2) ──
    public DateTime? FechaNacimiento { get; set; }
    public CategoriaAlumno? Categoria { get; set; }

    /// <summary>
    /// Nació con contraseña TEMPORAL (lo dio de alta su profe): debe
    /// cambiarla en el primer login antes de usar el portal.
    /// </summary>
    public bool DebeCambiarPassword { get; set; }

    /// <summary>
    /// Admin de PLATAFORMA (el dueño de la app): ve métricas globales y gestiona
    /// todos los clubes. Es un rol cross-tenant, aparte de su membresía de profe.
    /// </summary>
    public bool EsAdminPlataforma { get; set; }

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
