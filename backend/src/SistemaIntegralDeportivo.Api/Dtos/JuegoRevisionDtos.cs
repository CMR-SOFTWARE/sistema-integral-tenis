using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Pedido de revisión: exactamente uno de los dos ids viene seteado (singles o dobles).</summary>
public class CrearRevisionDto
{
    public Guid? JuegoPendienteId { get; set; }
    public Guid? JuegoDoblesPendienteId { get; set; }

    [Required, MinLength(10)]
    public string Comentario { get; set; } = string.Empty;
}

public class ResolverRevisionDto
{
    [Required]
    public string Respuesta { get; set; } = string.Empty;
}

public class JuegoRevisionDto
{
    public Guid Id { get; set; }
    public Guid? JuegoPendienteId { get; set; }
    public Guid? JuegoDoblesPendienteId { get; set; }
    public string Comentario { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? RespuestaAdmin { get; set; }
    public DateTime CreadoEl { get; set; }
    public DateTime? ResueltoEl { get; set; }
}

/// <summary>Para el panel de moderación de Plataforma — con el nombre de quien pidió la revisión.</summary>
public class RevisionPendienteDto
{
    public Guid Id { get; set; }
    public Guid? JuegoPendienteId { get; set; }
    public Guid? JuegoDoblesPendienteId { get; set; }
    public string CreadoPorNombre { get; set; } = string.Empty;
    public string Comentario { get; set; } = string.Empty;
    public DateTime CreadoEl { get; set; }
}
