using System.Diagnostics;

namespace SistemaIntegralDeportivo.Api.Common;

/// <summary>
/// Loguea una línea por request de la API con los cuatro números que separan las
/// causas posibles de una pantalla lenta:
///
/// <code>GET /api/turnos/semana → 200 en 3148 ms | base: 2412 ms en 47 consultas | 186 KB</code>
///
/// <list type="bullet">
/// <item>total alto + base alta + MUCHAS consultas → hay un N+1;</item>
/// <item>total alto + base alta + POCAS consultas → hay una consulta pesada;</item>
/// <item>total alto + base baja → el costo es armar y mandar el JSON;</item>
/// <item>total bajo acá pero lento en el celular → es la red o el arranque en frío
/// del contenedor, y no hay nada que optimizar en el código.</item>
/// </list>
/// </summary>
public class TiempoDeRequestMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<TiempoDeRequestMiddleware> _log;

    public TiempoDeRequestMiddleware(RequestDelegate siguiente, ILogger<TiempoDeRequestMiddleware> log)
    {
        _siguiente = siguiente;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext contexto, DiagnosticoDb diagnostico)
    {
        // Solo la API: los archivos estáticos y el health de Railway solo harían ruido.
        if (!contexto.Request.Path.StartsWithSegments("/api"))
        {
            await _siguiente(contexto);
            return;
        }

        // Contar los bytes de la respuesta: ContentLength viene null cuando la
        // respuesta va en chunks (que es el caso de casi todos los listados), así
        // que el tamaño hay que medirlo al pasar.
        var original = contexto.Response.Body;
        var contador = new StreamQueCuenta(original);
        contexto.Response.Body = contador;

        var reloj = Stopwatch.StartNew();
        try
        {
            await _siguiente(contexto);
        }
        finally
        {
            reloj.Stop();
            contexto.Response.Body = original;

            _log.LogInformation(
                "{Metodo} {Ruta} → {Estado} en {Total} ms | base: {MsBase} ms en {Consultas} consultas | {Kb} KB",
                contexto.Request.Method,
                contexto.Request.Path.Value,
                contexto.Response.StatusCode,
                reloj.ElapsedMilliseconds,
                (long)diagnostico.Milisegundos,
                diagnostico.Consultas,
                contador.Bytes / 1024);
        }
    }

    /// <summary>Deja pasar todo al stream real y solo va sumando cuántos bytes vio.</summary>
    private sealed class StreamQueCuenta : Stream
    {
        private readonly Stream _interno;

        public StreamQueCuenta(Stream interno)
        {
            _interno = interno;
        }

        public long Bytes { get; private set; }

        public override bool CanRead => _interno.CanRead;
        public override bool CanSeek => _interno.CanSeek;
        public override bool CanWrite => _interno.CanWrite;
        public override long Length => _interno.Length;

        public override long Position
        {
            get => _interno.Position;
            set => _interno.Position = value;
        }

        public override void Flush() => _interno.Flush();
        public override Task FlushAsync(CancellationToken ct) => _interno.FlushAsync(ct);
        public override int Read(byte[] buffer, int offset, int count) => _interno.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _interno.Seek(offset, origin);
        public override void SetLength(long value) => _interno.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            Bytes += count;
            _interno.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            Bytes += count;
            return _interno.WriteAsync(buffer, offset, count, ct);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        {
            Bytes += buffer.Length;
            return _interno.WriteAsync(buffer, ct);
        }
    }
}
