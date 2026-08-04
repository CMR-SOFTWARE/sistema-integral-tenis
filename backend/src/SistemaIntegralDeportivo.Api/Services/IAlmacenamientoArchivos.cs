namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Dónde viven los archivos que sube el usuario (fotos de perfil). Es una costura
/// intercambiable (ADR-0002): en producción va contra Supabase Storage y en
/// desarrollo contra el disco local, sin que el Service se entere de la diferencia.
/// Se elige por configuración (<c>Storage:Proveedor</c>), no por código.
/// </summary>
public interface IAlmacenamientoArchivos
{
    /// <summary>
    /// Guarda el contenido en <paramref name="ruta"/> (pisa si ya existía) y devuelve
    /// la URL con la que el navegador va a pedir la imagen.
    /// </summary>
    Task<string> SubirAsync(Stream contenido, string contentType, string ruta, CancellationToken ct = default);

    /// <summary>
    /// Borra el archivo. No revienta si ya no está: borrar algo que no existe es
    /// el estado que buscábamos igual.
    /// </summary>
    Task EliminarAsync(string ruta, CancellationToken ct = default);
}
