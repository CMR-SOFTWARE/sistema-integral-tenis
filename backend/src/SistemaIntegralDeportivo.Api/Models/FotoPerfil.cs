namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Una foto de la galería del perfil, con su pie: la gracia no es la foto sola sino
/// lo que el profe cuenta de ella ("Final del provincial 2024 con Martina").
/// </summary>
public class FotoPerfil
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PerfilProfesorId { get; set; }
    public PerfilProfesor Perfil { get; set; } = null!;

    /// <summary>La URL que consume el navegador.</summary>
    public required string Url { get; set; }

    /// <summary>La ruta dentro del storage: lo que hace falta para borrar el archivo.</summary>
    public required string Ruta { get; set; }

    public string? PieDeFoto { get; set; }

    /// <summary>Posición en la galería (el profe las ordena con las flechas).</summary>
    public int Orden { get; set; }

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
