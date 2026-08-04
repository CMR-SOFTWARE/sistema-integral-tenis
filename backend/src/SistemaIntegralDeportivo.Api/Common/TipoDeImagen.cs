namespace SistemaIntegralDeportivo.Api.Common;

/// <summary>
/// Reconoce el tipo de una imagen por sus "magic bytes" (la firma con la que
/// arranca el archivo), no por el Content-Type ni por la extensión que declara el
/// cliente: los dos se falsifican con un click, los bytes no. Sin esto, un .svg o
/// un .html renombrado a .jpg terminaría servido desde nuestro dominio.
/// </summary>
public static class TipoDeImagen
{
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string Webp = "image/webp";

    /// <summary>El content-type real, o null si no es una imagen de las que aceptamos.</summary>
    public static string? Detectar(ReadOnlySpan<byte> bytes)
    {
        // JPEG: FF D8 FF
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return Jpeg;

        // PNG: 89 "PNG" 0D 0A 1A 0A
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
            return Png;

        // WEBP: "RIFF" ....(4 bytes de tamaño).... "WEBP"
        if (bytes.Length >= 12 &&
            bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
            return Webp;

        return null;
    }

    /// <summary>La extensión que le corresponde al content-type ya detectado.</summary>
    public static string ExtensionDe(string contentType) => contentType switch
    {
        Png => "png",
        Webp => "webp",
        _ => "jpg",
    };
}
