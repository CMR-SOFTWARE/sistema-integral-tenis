using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Una noticia del club, como la ven el profe y el alumno.</summary>
public class NoticiaDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    /// <summary>Destacada: sube al Inicio del portal.</summary>
    public bool Importante { get; set; }
    public DateOnly? VenceEl { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEl { get; set; }
}

/// <summary>Alta y edición de una noticia (el mismo cuerpo para las dos).</summary>
public class GuardarNoticiaDto
{
    [Required, StringLength(100)]
    public string Titulo { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Destacada: aparece primero en el Inicio del portal.</summary>
    public bool Importante { get; set; }

    /// <summary>Opcional: hasta cuándo se muestra (null = sin vencimiento).</summary>
    public DateOnly? VenceEl { get; set; }
}

/// <summary>Prender/apagar una noticia (baja/reactivación).</summary>
public class CambiarActivoNoticiaDto
{
    public bool Activo { get; set; }
}
