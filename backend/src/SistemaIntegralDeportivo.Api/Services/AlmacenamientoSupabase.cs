using System.Net;
using System.Net.Http.Headers;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Supabase Storage vía su API REST (no hace falta ningún SDK: son dos verbos HTTP).
/// El bucket es PÚBLICO de lectura — las fotos del perfil las mira gente que recién
/// está eligiendo profe, y con URLs firmadas cada &lt;img&gt; vencería a los minutos y
/// no cachearía. Escribir y borrar, en cambio, exigen la service_role key, que vive
/// solo en el servidor (user-secrets en dev, variables de Railway en prod).
/// </summary>
public class AlmacenamientoSupabase : IAlmacenamientoArchivos
{
    private readonly HttpClient _http;
    private readonly ILogger<AlmacenamientoSupabase> _log;
    private readonly string _bucket;

    public AlmacenamientoSupabase(HttpClient http, IConfiguration config, ILogger<AlmacenamientoSupabase> log)
    {
        _http = http;
        _log = log;
        _bucket = config["Storage:Supabase:Bucket"] ?? "perfiles";
    }

    public async Task<string> SubirAsync(Stream contenido, string contentType, string ruta, CancellationToken ct = default)
    {
        using var cuerpo = new StreamContent(contenido);
        cuerpo.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        // BaseAddress ya termina en /storage/v1/ (se arma en Program.cs)
        using var pedido = new HttpRequestMessage(HttpMethod.Post, $"object/{_bucket}/{ruta}") { Content = cuerpo };
        pedido.Headers.Add("x-upsert", "true"); // reintentar una subida no falla por "ya existe"

        using var respuesta = await _http.SendAsync(pedido, ct);
        if (!respuesta.IsSuccessStatusCode)
        {
            var detalle = await respuesta.Content.ReadAsStringAsync(ct);
            _log.LogError("Supabase Storage rechazó la subida de {Ruta}: {Codigo} {Detalle}",
                ruta, respuesta.StatusCode, detalle);
            throw new InvalidOperationException($"No se pudo guardar la imagen ({(int)respuesta.StatusCode}).");
        }

        return $"{_http.BaseAddress}object/public/{_bucket}/{ruta}";
    }

    public async Task EliminarAsync(string ruta, CancellationToken ct = default)
    {
        using var respuesta = await _http.DeleteAsync($"object/{_bucket}/{ruta}", ct);

        // Un 404 es el estado que buscábamos igual: el archivo ya no está.
        if (!respuesta.IsSuccessStatusCode && respuesta.StatusCode != HttpStatusCode.NotFound)
        {
            var detalle = await respuesta.Content.ReadAsStringAsync(ct);
            _log.LogError("Supabase Storage rechazó el borrado de {Ruta}: {Codigo} {Detalle}",
                ruta, respuesta.StatusCode, detalle);
            throw new InvalidOperationException($"No se pudo borrar la imagen ({(int)respuesta.StatusCode}).");
        }
    }
}
