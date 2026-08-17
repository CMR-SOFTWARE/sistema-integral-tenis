namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Una foto de un producto del catálogo. Sin esto el profe puede ofrecer un encordado
/// (que se explica solo) pero no una raqueta ni una remera: no hay forma de mostrarlas.
///
/// La imagen vive en el STORAGE y acá se guarda su URL, igual que <see cref="FotoPerfil"/>
/// y a diferencia de <c>Alumno.FotoUrl</c>, que la mete en base64 dentro de la fila: con
/// veinte productos eso serían megas viajando en cada carga del catálogo.
/// </summary>
public class FotoServicio
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ServicioId { get; set; }
    public Servicio Servicio { get; set; } = null!;

    /// <summary>La URL que consume el navegador.</summary>
    public required string Url { get; set; }

    /// <summary>La ruta dentro del storage: lo que hace falta para borrar el archivo.</summary>
    public required string Ruta { get; set; }

    /// <summary>Posición en la galería. La PRIMERA es la que se ve en el listado del Shop.</summary>
    public int Orden { get; set; }

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
