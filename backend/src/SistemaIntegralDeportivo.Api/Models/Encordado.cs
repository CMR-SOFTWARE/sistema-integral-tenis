namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Un encordado concreto de una raqueta: con qué cuerda, a qué tensión y CUÁNDO.
/// Se guarda el historial —y no un solo juego de datos que se pisa— porque lo que el
/// profe mira es <b>hace cuánto</b> que está encordada: una cuerda vencida cambia
/// cómo juega el alumno y es el momento de ofrecerle encordarla.
/// </summary>
public class Encordado
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid RaquetaId { get; set; }
    public Raqueta Raqueta { get; set; } = null!;

    /// <summary>Cuerda de las verticales, ej "Luxilon ALU Power".</summary>
    public required string CuerdaVertical { get; set; }

    /// <summary>Tensión tal cual la dice el alumno, ej "24 kg" o "50 lbs".</summary>
    public string? TensionVertical { get; set; }

    /// <summary>
    /// Cuerda de las horizontales cuando el encordado es HÍBRIDO (dos cuerdas
    /// distintas). Null = simple: la misma cuerda en las dos direcciones.
    /// </summary>
    public string? CuerdaHorizontal { get; set; }

    public string? TensionHorizontal { get; set; }

    /// <summary>Cuándo se encordó (la fecha real, no la de carga).</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Desempata cuál es "el último" si hay dos encordados de la misma fecha.</summary>
    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;

    /// <summary>Un encordado con dos cuerdas distintas (verticales vs. horizontales).</summary>
    public bool EsHibrido => !string.IsNullOrWhiteSpace(CuerdaHorizontal);
}
