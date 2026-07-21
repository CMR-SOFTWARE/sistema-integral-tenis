using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Un aviso general del club.</summary>
public class AvisoDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateOnly? VenceEl { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEl { get; set; }
}

/// <summary>Alta/edición de un aviso.</summary>
public class GuardarAvisoDto
{
    [Required, StringLength(100)]
    public string Titulo { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Opcional: hasta cuándo se muestra (null = sin vencimiento).</summary>
    public DateOnly? VenceEl { get; set; }
}

/// <summary>Prender/apagar un aviso (baja/reactivación).</summary>
public class CambiarActivoAvisoDto
{
    public bool Activo { get; set; }
}
