namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Un aviso in-app.</summary>
public class NotificacionDto
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public Guid? EntidadId { get; set; }
    public bool Leida { get; set; }
    public DateTime CreadaEl { get; set; }
}
