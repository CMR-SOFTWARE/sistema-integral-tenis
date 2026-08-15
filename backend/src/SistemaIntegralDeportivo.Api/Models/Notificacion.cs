namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Aviso in-app para UN usuario (identidad global, no por tenant — un jugador
/// puede recibir notificaciones de ranking sin tener ficha en ningún club).
/// Primera pieza de infraestructura de notificaciones del sistema: genérica y
/// reusable, no propiedad del ranking (que es su primer consumidor).
/// </summary>
public class Notificacion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DestinatarioUserId { get; set; }

    /// <summary>Libre (ej. "DesafioRecibido", "DesafioAceptado") — no enum, para no migrar cada vez que se agrega un tipo.</summary>
    public required string Tipo { get; set; }

    public required string Mensaje { get; set; }

    /// <summary>El desafío/partido/etc. relacionado, para poder linkear desde el front. Sin navegación: cruza tablas de distintos módulos.</summary>
    public Guid? EntidadId { get; set; }

    public bool Leida { get; set; }

    public DateTime CreadaEl { get; set; } = DateTime.UtcNow;
}
