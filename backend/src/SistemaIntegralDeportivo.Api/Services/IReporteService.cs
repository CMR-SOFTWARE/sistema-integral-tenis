using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Reportes del profe: recaudación últimos 6 meses + alumnos por categoría.</summary>
public interface IReporteService
{
    /// <param name="hoy">Fecha de referencia (el mes actual es el último de la ventana).</param>
    Task<ReportesDto> ObtenerAsync(DateOnly hoy, CancellationToken ct = default);
}
