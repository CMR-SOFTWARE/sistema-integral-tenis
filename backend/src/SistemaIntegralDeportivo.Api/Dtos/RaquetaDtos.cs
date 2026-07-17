using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Una raqueta del alumno.</summary>
public class RaquetaDto
{
    public Guid Id { get; set; }
    public string Marca { get; set; } = string.Empty;
    public string? Tension { get; set; }
    public string? MarcaEncordado { get; set; }
}

/// <summary>Alta/edición de una raqueta.</summary>
public class GuardarRaquetaDto
{
    [Required, StringLength(80)]
    public string Marca { get; set; } = string.Empty;

    [StringLength(40)]
    public string? Tension { get; set; }

    [StringLength(80)]
    public string? MarcaEncordado { get; set; }
}

/// <summary>La foto de perfil como data URL (base64) — o vacío para quitarla.</summary>
public class ActualizarFotoDto
{
    /// <summary>"data:image/jpeg;base64,..." o null/"" para sacar la foto.</summary>
    public string? FotoUrl { get; set; }
}
