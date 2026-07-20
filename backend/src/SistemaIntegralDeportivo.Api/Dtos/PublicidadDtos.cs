using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Un banner de publicidad.</summary>
public class PublicidadDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public string? Enlace { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Alta de un banner (imagen comprimida en el navegador como data URL).</summary>
public class GuardarPublicidadDto
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string ImagenUrl { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Enlace { get; set; }
}

/// <summary>Prender/apagar un banner (baja/reactivación).</summary>
public class CambiarActivoPublicidadDto
{
    public bool Activo { get; set; }
}
