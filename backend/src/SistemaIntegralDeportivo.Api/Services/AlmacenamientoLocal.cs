namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Los archivos en el disco de la máquina de desarrollo, servidos por Kestrel en
/// <c>/archivos</c>. Es el default: así se desarrolla y se prueba la feature completa
/// sin credenciales de Supabase y sin apuntar dev a producción (CLAUDE.md §7).
/// </summary>
public class AlmacenamientoLocal : IAlmacenamientoArchivos
{
    private readonly string _raiz;
    private readonly string _urlBase;

    public AlmacenamientoLocal(IConfiguration config, IHostEnvironment entorno)
    {
        _raiz = RaizDe(config, entorno);
        _urlBase = (config["Storage:Local:UrlBase"] ?? "http://localhost:5223/archivos").TrimEnd('/');
    }

    /// <summary>La carpeta física, resuelta igual acá y en Program.cs (que monta el UseStaticFiles).</summary>
    public static string RaizDe(IConfiguration config, IHostEnvironment entorno) =>
        Path.Combine(entorno.ContentRootPath, config["Storage:Local:Raiz"] ?? "archivos-locales");

    public async Task<string> SubirAsync(Stream contenido, string contentType, string ruta, CancellationToken ct = default)
    {
        var destino = RutaFisica(ruta);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);

        await using (var archivo = File.Create(destino))
            await contenido.CopyToAsync(archivo, ct);

        return $"{_urlBase}/{ruta}";
    }

    public Task EliminarAsync(string ruta, CancellationToken ct = default)
    {
        var destino = RutaFisica(ruta);
        if (File.Exists(destino)) File.Delete(destino);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Traduce la ruta lógica a una física dentro de la raíz. Verifica que no se
    /// escape con "..": hoy la ruta la arma el backend, pero el chequeo cuesta nada
    /// y evita que un cambio futuro abra un path traversal.
    /// </summary>
    private string RutaFisica(string ruta)
    {
        var destino = Path.GetFullPath(Path.Combine(_raiz, ruta));
        if (!destino.StartsWith(Path.GetFullPath(_raiz), StringComparison.Ordinal))
            throw new InvalidOperationException("Ruta de archivo inválida.");
        return destino;
    }
}
