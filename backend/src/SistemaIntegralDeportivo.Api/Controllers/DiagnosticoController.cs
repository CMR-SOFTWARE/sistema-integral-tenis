using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;

namespace SistemaIntegralDeportivo.Api.Controllers;

/// <summary>
/// Mediciones de infraestructura para el dueño de la app. No toca datos de ningún
/// club: solo devuelve números sobre el entorno donde corre la API.
/// </summary>
[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/diagnostico")]
public class DiagnosticoController : ControllerBase
{
    private const int Mediciones = 10;

    private readonly AppDbContext _db;

    public DiagnosticoController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET api/diagnostico/base — cuánto tarda una ida y vuelta a la base.
    ///
    /// Contesta directo la pregunta de si el problema es la distancia entre Railway y
    /// Supabase: este número es el <b>piso</b> de cada consulta, así que multiplicado
    /// por la cantidad de consultas de una pantalla da el mínimo que esa pantalla puede
    /// tardar. Menos de 10 ms significa que la base está al lado y el problema es
    /// nuestro; 50-200 ms, que están lejos y lo que hay que bajar es la CANTIDAD de
    /// consultas.
    ///
    /// Devuelve solo tiempos: nada de connection string ni host.
    /// </summary>
    [HttpGet("base")]
    public async Task<ActionResult<LatenciaDto>> Latencia(CancellationToken ct)
    {
        var muestras = new List<double>(Mediciones);

        // La primera suele incluir abrir la conexión: se mide igual y se informa
        // aparte, porque en un contenedor recién levantado ESE es el costo real.
        for (var i = 0; i < Mediciones; i++)
        {
            var reloj = Stopwatch.StartNew();
            await _db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            reloj.Stop();
            muestras.Add(reloj.Elapsed.TotalMilliseconds);
        }

        return Ok(new LatenciaDto
        {
            Primera = Math.Round(muestras[0], 1),
            Minimo = Math.Round(muestras.Min(), 1),
            Promedio = Math.Round(muestras.Average(), 1),
            Maximo = Math.Round(muestras.Max(), 1),
            Region = Environment.GetEnvironmentVariable("RAILWAY_REGION"),
        });
    }

    /// <summary>Una ida y vuelta a la base, en milisegundos.</summary>
    public class LatenciaDto
    {
        /// <summary>La primera medición: incluye abrir la conexión si el pool estaba frío.</summary>
        public double Primera { get; set; }
        public double Minimo { get; set; }
        public double Promedio { get; set; }
        public double Maximo { get; set; }
        /// <summary>En qué región corre la API (Railway lo inyecta); null fuera de Railway.</summary>
        public string? Region { get; set; }
    }
}
