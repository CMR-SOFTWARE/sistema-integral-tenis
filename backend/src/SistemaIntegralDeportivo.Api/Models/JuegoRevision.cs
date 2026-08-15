namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Pedido de revisión sobre un partido ya Finalizado — es un TICKET, no un
/// mecanismo de corrección: Resolver solo marca Resuelta + guarda la
/// respuesta del admin. NUNCA toca PuntosMovimiento ni GanadorId — si el
/// admin decide que el resultado estaba mal, la corrección es un proceso
/// manual aparte (fuera de este build), no algo que este ticket dispare solo.
/// Sirve tanto para singles (JuegoPendienteId) como dobles (JuegoDoblesPendienteId) —
/// exactamente uno de los dos está seteado.
/// </summary>
public class JuegoRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? JuegoPendienteId { get; set; }
    public Guid? JuegoDoblesPendienteId { get; set; }

    public Guid CreadoPorUserId { get; set; }
    public string Comentario { get; set; } = string.Empty;

    public EstadoJuegoRevision Estado { get; set; } = EstadoJuegoRevision.Pendiente;
    public string? RespuestaAdmin { get; set; }
    public Guid? ResueltoPorUserId { get; set; }
    public DateTime? ResueltoEl { get; set; }

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
