using Moq;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reportes (TDD acotado, ADR-0005): la única lógica es la ventana de
/// "últimos 6 meses" (con cruce de año) y el relleno con ceros de los
/// meses sin recaudación. El resto es agregación en el repo.
/// </summary>
public class ReporteServiceTests
{
    private readonly Mock<ICargoRepository> _cargos = new();
    private readonly Mock<IAlumnoRepository> _alumnos = new();
    private readonly ReporteService _service;

    public ReporteServiceTests()
    {
        _service = new ReporteService(_cargos.Object, _alumnos.Object);

        _cargos.Setup(c => c.SumarPagadosPorMesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<(int, int), decimal>());
        _alumnos.Setup(a => a.ContarPorCategoriaAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
    }

    [Fact]
    public void UltimosMeses_DentroDelAnio()
    {
        var meses = ReporteService.UltimosMeses(new DateOnly(2026, 7, 13), 6);

        Assert.Equal([(2026, 2), (2026, 3), (2026, 4), (2026, 5), (2026, 6), (2026, 7)], meses);
    }

    [Fact]
    public void UltimosMeses_CruzaElAnio()
    {
        var meses = ReporteService.UltimosMeses(new DateOnly(2026, 2, 1), 6);

        Assert.Equal([(2025, 9), (2025, 10), (2025, 11), (2025, 12), (2026, 1), (2026, 2)], meses);
    }

    [Fact]
    public async Task Obtener_Devuelve6MesesCompletos_ConCeroDondeNoHuboRecaudacion()
    {
        // Solo mayo y julio tuvieron cobros
        _cargos.Setup(c => c.SumarPagadosPorMesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<(int, int), decimal>
               {
                   [(2026, 5)] = 24_000m,
                   [(2026, 7)] = 16_000m,
               });

        var reporte = await _service.ObtenerAsync(new DateOnly(2026, 7, 13));

        Assert.Equal(6, reporte.RecaudacionMensual.Count);
        Assert.Equal(2, reporte.RecaudacionMensual[0].Mes);      // arranca en febrero
        Assert.Equal(0m, reporte.RecaudacionMensual[0].Total);   // sin cobros = 0
        Assert.Equal(24_000m, reporte.RecaudacionMensual.Single(m => m.Mes == 5).Total);
        Assert.Equal(16_000m, reporte.RecaudacionMensual.Single(m => m.Mes == 7).Total);
        // La ventana pedida al repo: del 1° del mes -5 al fin del mes actual
        _cargos.Verify(c => c.SumarPagadosPorMesAsync(
            new DateOnly(2026, 2, 1), new DateOnly(2026, 7, 31), It.IsAny<CancellationToken>()), Times.Once);
    }
}
