using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Arma el resumen del dashboard del profesor con datos reales del tenant.</summary>
public interface IDashboardService
{
    Task<DashboardResumenDto> ObtenerResumenAsync(CancellationToken ct = default);
}
