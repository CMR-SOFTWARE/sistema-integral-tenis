using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Compone agregados del repositorio en el resumen del dashboard.
/// Sin reglas de negocio (queries finas) → sin test-first, según ADR-0005.
/// </summary>
public class DashboardService : IDashboardService
{
    // Orden fijo del ranking: de la mejor (1ra) a la inicial (7ma) + sin categoría
    private static readonly CategoriaAlumno[] OrdenCategorias =
    [
        CategoriaAlumno.Primera, CategoriaAlumno.Segunda, CategoriaAlumno.Tercera,
        CategoriaAlumno.Cuarta, CategoriaAlumno.Quinta, CategoriaAlumno.Sexta,
        CategoriaAlumno.Septima, CategoriaAlumno.SinCategoria,
    ];

    private readonly IAlumnoRepository _repo;

    public DashboardService(IAlumnoRepository repo)
    {
        _repo = repo;
    }

    public async Task<DashboardResumenDto> ObtenerResumenAsync(CancellationToken ct = default)
    {
        var hoy = DateTime.UtcNow;
        var inicioDeMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var porCategoria = await _repo.ContarPorCategoriaAsync(ct);

        return new DashboardResumenDto
        {
            AlumnosActivos = await _repo.ContarPorEstadoAsync(EstadoAlumno.Activo, ct),
            NuevosEsteMes = await _repo.ContarNuevosDesdeAsync(inicioDeMes, ct),
            Pausados = await _repo.ContarPorEstadoAsync(EstadoAlumno.Suspendido, ct),
            IngresoMensualEstimado = await _repo.SumarArancelActivosAsync(ct),
            PorCategoria = OrdenCategorias
                .Select(c => new CategoriaConteoDto
                {
                    Categoria = c.ToString(),
                    Cantidad = porCategoria.GetValueOrDefault(c),
                })
                .ToList(),
        };
    }
}
