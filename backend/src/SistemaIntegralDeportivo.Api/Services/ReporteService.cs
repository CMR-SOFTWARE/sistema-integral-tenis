using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class ReporteService : IReporteService
{
    private const int MesesDeVentana = 6;

    // Mismo orden que el dashboard: de la mejor (1ra) a la inicial (7ma) + sin categoría
    private static readonly CategoriaAlumno[] OrdenCategorias =
    [
        CategoriaAlumno.Primera, CategoriaAlumno.Segunda, CategoriaAlumno.Tercera,
        CategoriaAlumno.Cuarta, CategoriaAlumno.Quinta, CategoriaAlumno.Sexta,
        CategoriaAlumno.Septima, CategoriaAlumno.SinCategoria,
    ];

    private readonly ICargoRepository _cargos;
    private readonly IAlumnoRepository _alumnos;

    public ReporteService(ICargoRepository cargos, IAlumnoRepository alumnos)
    {
        _cargos = cargos;
        _alumnos = alumnos;
    }

    public async Task<ReportesDto> ObtenerAsync(DateOnly hoy, CancellationToken ct = default)
    {
        var meses = UltimosMeses(hoy, MesesDeVentana);
        var (primerAnio, primerMes) = meses[0];
        var desde = new DateOnly(primerAnio, primerMes, 1);
        var hasta = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(1).AddDays(-1);

        var recaudado = await _cargos.SumarPagadosPorMesAsync(desde, hasta, ct);
        var porCategoria = await _alumnos.ContarPorCategoriaAsync(ct);

        return new ReportesDto
        {
            RecaudacionMensual = meses
                .Select(m => new MesRecaudacionDto
                {
                    Anio = m.Anio,
                    Mes = m.Mes,
                    Total = recaudado.GetValueOrDefault((m.Anio, m.Mes)), // sin cobros = 0
                })
                .ToList(),
            PorCategoria = OrdenCategorias
                .Select(c => new CategoriaConteoDto
                {
                    Categoria = c.ToString(),
                    Cantidad = porCategoria.GetValueOrDefault(c),
                })
                .ToList(),
        };
    }

    /// <summary>Los últimos N meses terminando en el de <paramref name="hoy"/>, del más viejo al actual.</summary>
    public static IReadOnlyList<(int Anio, int Mes)> UltimosMeses(DateOnly hoy, int cantidad)
    {
        var primero = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-(cantidad - 1));
        return Enumerable.Range(0, cantidad)
            .Select(i => primero.AddMonths(i))
            .Select(d => (d.Year, d.Month))
            .ToList();
    }
}
