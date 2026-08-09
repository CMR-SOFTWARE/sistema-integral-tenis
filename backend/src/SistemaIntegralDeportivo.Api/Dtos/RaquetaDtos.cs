using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Una raqueta del alumno, con su historial de encordados.</summary>
public class RaquetaDto
{
    public Guid Id { get; set; }
    public string Marca { get; set; } = string.Empty;
    public string? Modelo { get; set; }

    /// <summary>El historial, del MÁS NUEVO al más viejo.</summary>
    public List<EncordadoDto> Encordados { get; set; } = [];

    /// <summary>El último encordado ya resuelto (null si nunca se cargó uno).</summary>
    public EncordadoDto? UltimoEncordado { get; set; }
}

/// <summary>Un encordado del historial.</summary>
public class EncordadoDto
{
    public Guid Id { get; set; }
    public string CuerdaVertical { get; set; } = string.Empty;
    public string? TensionVertical { get; set; }
    public string? CuerdaHorizontal { get; set; }
    public string? TensionHorizontal { get; set; }
    public DateOnly Fecha { get; set; }
    /// <summary>Dos cuerdas distintas (verticales vs. horizontales).</summary>
    public bool EsHibrido { get; set; }
}

/// <summary>Alta/edición de una raqueta (el encordado va por su cuenta).</summary>
public class GuardarRaquetaDto
{
    [Required, StringLength(80)]
    public string Marca { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Modelo { get; set; }
}

/// <summary>Alta/edición de un encordado. Las horizontales solo si es híbrido.</summary>
public class GuardarEncordadoDto
{
    [Required, StringLength(80)]
    public string CuerdaVertical { get; set; } = string.Empty;

    [StringLength(40)]
    public string? TensionVertical { get; set; }

    [StringLength(80)]
    public string? CuerdaHorizontal { get; set; }

    [StringLength(40)]
    public string? TensionHorizontal { get; set; }

    [Required]
    public DateOnly Fecha { get; set; }
}

/// <summary>La foto de perfil como data URL (base64) — o vacío para quitarla.</summary>
public class ActualizarFotoDto
{
    /// <summary>"data:image/jpeg;base64,..." o null/"" para sacar la foto.</summary>
    public string? FotoUrl { get; set; }
}
