namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Un banner de publicidad (M6). Apunta a un TENANT (academia/club): los alumnos
/// de ESE tenant lo ven en el portal. Hoy lo carga el profe desde Configuración;
/// a futuro, un ANUNCIANTE va a poder subir el suyo y pagar por mes vía la
/// plataforma (se agregará AnuncianteId + facturación sin rehacer esto).
/// </summary>
public class Publicidad
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Rótulo para que el profe lo identifique ("Deportes García").</summary>
    public required string Nombre { get; set; }

    /// <summary>La imagen del banner como data URL (base64) — sin hosting externo.</summary>
    public required string ImagenUrl { get; set; }

    /// <summary>Link opcional que se abre al tocar el banner.</summary>
    public string? Enlace { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
