using SistemaIntegralDeportivo.Api.Common;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Primer BackgroundService del proyecto: dispara el cierre oficial los
/// días 1 y 16. Corre en un contenedor persistente (Railway), así que un
/// PeriodicTimer con chequeo periódico alcanza — no hace falta el patrón
/// "endpoint + cron externo" de la infraestructura serverless original.
/// Sin lógica de negocio propia (por eso sin test-first, ADR-0005): la
/// regla real vive en RankingCierreOficialService, que sí está testeado.
/// </summary>
public class RankingCierreOficialJob : BackgroundService
{
    private static readonly int[] DiasDeCierre = [1, 16];
    private static readonly TimeSpan IntervaloDeChequeo = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RankingCierreOficialJob> _logger;

    public RankingCierreOficialJob(IServiceScopeFactory scopeFactory, ILogger<RankingCierreOficialJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(IntervaloDeChequeo);
        do
        {
            await IntentarCerrarAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task IntentarCerrarAsync(CancellationToken ct)
    {
        if (!DiasDeCierre.Contains(DateTime.UtcNow.Day)) return;

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRankingCierreOficialService>();
        try
        {
            var cantidad = await service.CerrarOficialAsync(ct);
            _logger.LogInformation("Cierre oficial de ranking: {Cantidad} filas.", cantidad);
        }
        catch (ReglaDeNegocioException)
        {
            // Ya se había cerrado hoy (p.ej. el contenedor reinició el mismo día) — no es un error real.
        }
    }
}
